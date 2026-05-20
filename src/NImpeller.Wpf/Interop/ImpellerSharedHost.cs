using System;
using System.Runtime.InteropServices;
using System.Threading;

using NImpeller;
using NImpeller.Wpf;

using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace NImpeller.Wpf.Interop;

/// <summary>
/// Process-wide singleton that owns the one ImpellerContext (and all its Vulkan
/// underpinnings) shared by every <see cref="ImpellerView"/>.
///
/// Why singleton: Impeller creates its own VkInstance/VkDevice internally based on the
/// adapter LUID we steer it towards. If multiple ImpellerContexts existed in the same
/// process they might pick different physical devices, and the D3D11-shared textures
/// imported into one device cannot be sampled by another. Forcing a single context
/// also amortizes Impeller's ~1-2 s init cost across all views and across repeated
/// window open/close cycles.
///
/// Lifetime: reference-counted via <see cref="AcquireAndStart"/> / <see cref="Release"/>.
/// We default to <b>keep-alive</b> (we do not dispose when refcount hits zero), so
/// reopening a window is instant. <see cref="Shutdown"/> can be called by the app at
/// exit if early teardown is desired; otherwise a <see cref="AppDomain.ProcessExit"/>
/// hook does the same work.
/// </summary>
internal sealed unsafe class ImpellerSharedHost
{
    private static readonly object Gate = new();
    private static ImpellerSharedHost? _instance;
    private static int _refCount;
    /// <summary>
    /// Guard so the <c>AppDomain.ProcessExit</c> handler is only attached once per
    /// process. Without this, a <c>Shutdown()</c> followed by a new
    /// <c>AcquireAndStart</c> would create a fresh instance and attach a second
    /// handler — at process exit <c>Shutdown</c> would then run twice
    /// (idempotent thanks to <c>DisposeAll</c>'s guard, but still wasteful and
    /// surprising).
    /// </summary>
    private static bool _processExitHooked;

    public static ImpellerSharedHost AcquireAndStart(ImpellerViewSettings settings)
    {
        lock (Gate)
        {
            if (_instance == null)
                _instance = CreateInstance(settings);
            _refCount++;
            return _instance;
        }
    }

    public static void Release()
    {
        lock (Gate)
        {
            if (_refCount > 0) _refCount--;
            // Keep alive intentionally — even on refcount==0 we hold the ImpellerContext
            // so the next view creation is fast. Shutdown() / ProcessExit will tear it down.
        }
    }

    public static void Shutdown()
    {
        lock (Gate)
        {
            _instance?.DisposeAll();
            _instance = null;
            _refCount = 0;
        }
    }

    // ---------------- Instance state ----------------
    public ImpellerContext Context { get; private set; } = null!;
    public ImpellerContextVulkanInfo VulkanInfo { get; private set; }
    public Vk Vk { get; private set; } = null!;
    public Instance VkInstance { get; private set; }
    public PhysicalDevice VkPhysicalDevice { get; private set; }
    public Device VkDevice { get; private set; }
    public Queue VkQueue { get; private set; }
    public uint QueueFamilyIndex { get; private set; }
    public ImpellerTypographyContext? Typography { get; private set; }
    public KhrSurface? KhrSurfaceExt { get; private set; }
    public KhrWin32Surface? KhrWin32SurfaceExt { get; private set; }
    /// <summary>
    /// Device-level extension used by <see cref="SharedVulkanImage"/> to query the
    /// memory type bits compatible with an imported D3D11 KMT handle
    /// (<c>vkGetMemoryWin32HandlePropertiesKHR</c>). Loaded once at host init.
    /// </summary>
    public KhrExternalMemoryWin32? KhrExternalMemoryWin32Ext { get; private set; }
    /// <summary>HINSTANCE of the host process (used for creating per-view hidden HWNDs).</summary>
    public IntPtr HiddenHInstance { get; private set; }

    /// <summary>
    /// Process-shared mutex used to serialize <c>vkQueueSubmit</c> across every
    /// <see cref="BlitContext"/> (one per ImpellerView) since all of them post
    /// to the same Impeller <see cref="Queue"/>. Multi-view present paths must
    /// take this lock; allocating a per-view lock would let concurrent submits
    /// from different views race on the shared queue.
    /// </summary>
    public object SharedQueueLock { get; } = new();

    private bool _disposed;

    private ImpellerSharedHost() { }

    private static ImpellerSharedHost CreateInstance(ImpellerViewSettings settings)
    {
        var host = new ImpellerSharedHost();
        host.InitializeOnce(settings);
        return host;
    }

    private void InitializeOnce(ImpellerViewSettings settings)
    {
        // 1) Vulkan loader + interceptor (interceptor is static, no per-instance state)
        VkProcInterceptor.Initialize();

        // 2) Probe the D3D adapter LUID by spinning up a throwaway D3DResources just to read
        //    the LUID; immediately dispose. The LUID drives our vkEnumeratePhysicalDevices
        //    reorder so Impeller picks the same physical GPU as D3D.
        {
            using var probe = new D3DResources();
            // D3D9Ex requires a focus HWND. Use the desktop window — read-only, never modified.
            probe.Initialize(GetDesktopWindow());
            var luid = probe.AdapterLuid;
            VkTrampolines.TargetAdapterLuid = ((ulong)(uint)luid.High << 32) | (uint)luid.Low;
            TraceLog.Log($"[ImpellerSharedHost] Target adapter LUID = 0x{VkTrampolines.TargetAdapterLuid:X16}");
        }

        // 3) Create the single ImpellerContext (this is the heavyweight, ~1-2 s) step
        VkTrampolines.PendingInitializationError = null;
        var ctx = ImpellerContext.CreateVulkanNew(
            VkProcInterceptor.GetProcAddress,
            enableValidation: settings.EnableValidation);

        // The LUID-reorder trampoline may have stashed a fatal mismatch — re-throw on
        // the managed side now, with the original CreateVulkanNew failure (if any)
        // as the inner exception.
        if (VkTrampolines.PendingInitializationError is { } stashed)
        {
            VkTrampolines.PendingInitializationError = null;
            ctx?.Dispose();
            throw new ImpellerRenderErrorException(
                "ImpellerSharedHost initialization aborted: " + stashed.Message,
                stashed);
        }

        Context = ctx ?? throw new InvalidOperationException("ImpellerContext.CreateVulkanNew returned null.");

        var info = Context.GetVulkanInfo()
                   ?? throw new InvalidOperationException("ImpellerContext.GetVulkanInfo returned null.");
        VulkanInfo = info;

        // 4) Cache Silk.NET handles + extensions
        Vk = Vk.GetApi();
        VkInstance = new Instance(info.Vk_instance);
        VkPhysicalDevice = new PhysicalDevice(info.Vk_physical_device);
        VkDevice = new Device(info.Vk_logical_device);
        QueueFamilyIndex = info.Graphics_queue_family_index;
        Vk.GetDeviceQueue(VkDevice, QueueFamilyIndex, info.Graphics_queue_index, out var queue);
        VkQueue = queue;

        if (!Vk.TryGetInstanceExtension<KhrWin32Surface>(VkInstance, out var khrWin32))
            throw new InvalidOperationException("VK_KHR_win32_surface not available on Impeller's VkInstance.");
        KhrWin32SurfaceExt = khrWin32;
        if (!Vk.TryGetInstanceExtension<KhrSurface>(VkInstance, out var khrSurface))
            throw new InvalidOperationException("VK_KHR_surface not available on Impeller's VkInstance.");
        KhrSurfaceExt = khrSurface;
        if (!Vk.TryGetDeviceExtension<KhrExternalMemoryWin32>(VkInstance, VkDevice, out var khrExtMemWin32))
            throw new InvalidOperationException(
                "VK_KHR_external_memory_win32 not available on Impeller's VkDevice — shared-texture import cannot work.");
        KhrExternalMemoryWin32Ext = khrExtMemWin32;

        TraceLog.Log("[ImpellerSharedHost] Vulkan context ready:");
        TraceLog.Log($"  VkInstance       = 0x{(long)info.Vk_instance:X16}");
        TraceLog.Log($"  VkPhysicalDevice = 0x{(long)info.Vk_physical_device:X16}");
        TraceLog.Log($"  VkDevice         = 0x{(long)info.Vk_logical_device:X16}");
        TraceLog.Log($"  VkQueue          = 0x{VkQueue.Handle:X16}");
        TraceLog.Log($"  QueueFamilyIndex = {QueueFamilyIndex}");

        // 5) Typography context (reused by every view)
        Typography = ImpellerTypographyContext.New();
        if (Typography == null)
            TraceLog.Log("[ImpellerSharedHost] WARNING: TypographyContext.New returned null; text APIs disabled.");

        // 6) HINSTANCE of the host module — each ImpellerView creates its own hidden HWND
        //    sized to match its physical pixel render target, so that
        //    vkGetPhysicalDeviceSurfaceCapabilitiesKHR returns the right currentExtent for
        //    that view's swapchain. A single shared HWND would force all surfaces to the
        //    same client-area size, which doesn't work when views have different sizes.
        HiddenHInstance = GetModuleHandleW(null);

        // 7) Ensure clean shutdown on process exit (idempotent w.r.t. explicit Shutdown()).
        //    Only register the handler once per process — re-registering after a
        //    Shutdown+Acquire cycle would cause Shutdown to fire multiple times.
        if (!_processExitHooked)
        {
            _processExitHooked = true;
            AppDomain.CurrentDomain.ProcessExit += (_, _) => Shutdown();
        }

        // 8) Cache a snapshot of GPU + Vulkan + Impeller info for diagnostic UIs.
        CachedGpuInfo = QueryGpuInfo();
    }

    // ---------------- GPU info ----------------
    public static ImpellerGpuInfo? CachedGpuInfo { get; private set; }

    [DllImport("impeller", EntryPoint = "ImpellerGetVersion", CallingConvention = CallingConvention.StdCall)]
    private static extern uint ImpellerGetVersion();

    private ImpellerGpuInfo QueryGpuInfo()
    {
        // Vulkan device properties
        Vk.GetPhysicalDeviceProperties(VkPhysicalDevice, out var props);
        Vk.GetPhysicalDeviceMemoryProperties(VkPhysicalDevice, out var memProps);

        // Device name is a fixed byte[256] UTF-8 buffer; convert with a manual scan.
        string deviceName;
        unsafe
        {
            int len = 0;
            while (len < 256 && props.DeviceName[len] != 0) len++;
            deviceName = System.Text.Encoding.UTF8.GetString(props.DeviceName, len);
        }

        ulong devLocal = 0, hostVis = 0;
        unsafe
        {
            for (int i = 0; i < memProps.MemoryHeapCount; i++)
            {
                var heap = memProps.MemoryHeaps[i];
                if ((heap.Flags & MemoryHeapFlags.DeviceLocalBit) != 0)
                    devLocal += heap.Size;
            }
            for (int i = 0; i < memProps.MemoryTypeCount; i++)
            {
                var mt = memProps.MemoryTypes[i];
                if ((mt.PropertyFlags & MemoryPropertyFlags.HostVisibleBit) != 0)
                {
                    if (mt.HeapIndex < memProps.MemoryHeapCount)
                        hostVis = Math.Max(hostVis, memProps.MemoryHeaps[(int)mt.HeapIndex].Size);
                }
            }
        }

        uint impellerVer = 0;
        try { impellerVer = ImpellerGetVersion(); }
        catch (Exception ex) { TraceLog.Log($"[ImpellerSharedHost] ImpellerGetVersion threw: {ex.Message}"); }

        return new ImpellerGpuInfo
        {
            ImpellerApiVersionRaw = impellerVer,
            ImpellerApiVersion = DecodeImpellerVersion(impellerVer),
            VulkanApiVersionRaw = props.ApiVersion,
            VulkanApiVersion = DecodeVulkanVersion(props.ApiVersion),
            DriverVersionRaw = props.DriverVersion,
            VendorId = props.VendorID,
            VendorName = VendorName(props.VendorID),
            DeviceId = props.DeviceID,
            DeviceName = deviceName,
            DeviceType = props.DeviceType.ToString().Replace("PhysicalDeviceType", ""),
            AdapterLuid = VkTrampolines.TargetAdapterLuid,
            QueueFamilyIndex = QueueFamilyIndex,
            QueueIndex = VulkanInfo.Graphics_queue_index,
            DeviceLocalMemoryBytes = devLocal,
            HostVisibleMemoryBytes = hostVis,
            MaxImageDimension2D = props.Limits.MaxImageDimension2D,
            MaxFramebufferWidth = props.Limits.MaxFramebufferWidth,
            MaxFramebufferHeight = props.Limits.MaxFramebufferHeight,
            VkInstance = VulkanInfo.Vk_instance,
            VkPhysicalDevice = VulkanInfo.Vk_physical_device,
            VkDevice = VulkanInfo.Vk_logical_device,
            VkQueue = (IntPtr)VkQueue.Handle,
        };
    }

    private static string DecodeVulkanVersion(uint v) =>
        $"{(v >> 22) & 0x7F}.{(v >> 12) & 0x3FF}.{v & 0xFFF}";

    private static string DecodeImpellerVersion(uint v) =>
        // Impeller uses ImpellerMakeVersion(variant, major, minor, patch) — same layout as Vulkan
        // (variant<<29 | major<<22 | minor<<12 | patch). Show major.minor.patch.
        $"{(v >> 22) & 0x7F}.{(v >> 12) & 0x3FF}.{v & 0xFFF}";

    private static string VendorName(uint vendorId) => vendorId switch
    {
        0x1002 => "AMD",
        0x1010 => "ImgTec",
        0x10DE => "NVIDIA",
        0x13B5 => "ARM",
        0x5143 => "Qualcomm",
        0x8086 => "Intel",
        _      => $"0x{vendorId:X4}",
    };

    [DllImport("kernel32")]
    private static extern IntPtr GetModuleHandleW(string? lpModuleName);

    private void DisposeAll()
    {
        if (_disposed) return;
        _disposed = true;

        try { Vk?.DeviceWaitIdle(VkDevice); }
        catch (Exception ex) { TraceLog.Log($"[ImpellerSharedHost] DeviceWaitIdle threw: {ex.Message}"); }

        Typography?.Dispose();
        Typography = null;

        KhrWin32SurfaceExt?.Dispose();
        KhrWin32SurfaceExt = null;
        KhrSurfaceExt?.Dispose();
        KhrSurfaceExt = null;
        KhrExternalMemoryWin32Ext?.Dispose();
        KhrExternalMemoryWin32Ext = null;

        Context?.Dispose();
        Context = null!;

        HiddenHInstance = IntPtr.Zero;

        Vk?.Dispose();
        Vk = null!;

        TraceLog.Log("[ImpellerSharedHost] disposed.");
    }

    [System.Runtime.InteropServices.DllImport("user32")]
    private static extern IntPtr GetDesktopWindow();
}
