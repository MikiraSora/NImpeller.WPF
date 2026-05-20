using System;
using System.Threading;

using NImpeller;

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
    /// <summary>HINSTANCE of the host process (used for creating per-view hidden HWNDs).</summary>
    public IntPtr HiddenHInstance { get; private set; }

    /// <summary>Shared mutex used to serialize <c>vkQueueSubmit</c> across all BlitContexts.</summary>
    public object QueueSubmitLock { get; } = new();

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
        var ctx = ImpellerContext.CreateVulkanNew(
            VkProcInterceptor.GetProcAddress,
            enableValidation: settings.EnableValidation);
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

        // 7) Ensure clean shutdown on process exit (idempotent w.r.t. explicit Shutdown())
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Shutdown();
    }

    [System.Runtime.InteropServices.DllImport("kernel32")]
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
