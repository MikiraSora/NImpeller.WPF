using System;
using System.Windows;
using System.Windows.Interop;

using NImpeller;
using NImpeller.Wpf.Interop;

using Silk.NET.Vulkan;

namespace NImpeller.Wpf;

internal sealed unsafe class ImpellerViewResources : IDisposable
{
    private readonly ImpellerViewSettings _settings;
    private ImpellerSharedHost? _host;
    private D3DResources? _d3dResources;
    private SharedVulkanImage? _sharedVkImage;
    private HiddenVulkanWindow? _hiddenWindow;
    private SurfaceKHR _vkSurface;
    private ImpellerVulkanSwapchain? _impellerSwapchain;
    private ulong _swapchainHandle;
    private BlitContext? _blitContext;
    private D3DImage? _d3dImage;
    private string? _hostWindowTitle;

    private ImpellerViewResources(ImpellerViewSettings settings, uint pixelWidth, uint pixelHeight, double dpiScaleX, double dpiScaleY)
    {
        _settings = settings;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        DpiScaleX = dpiScaleX;
        DpiScaleY = dpiScaleY;
    }

    internal D3DImage? D3DImage => _d3dImage;
    internal ImpellerContext? Context => _host?.Context;
    internal ImpellerTypographyContext? Typography => _host?.Typography;
    internal uint PixelWidth { get; private set; }
    internal uint PixelHeight { get; private set; }
    internal double DpiScaleX { get; private set; }
    internal double DpiScaleY { get; private set; }
    internal bool CanRender => _host != null && _impellerSwapchain != null && _d3dImage != null;

    internal static ImpellerViewResources Create(
        ImpellerView owner,
        ImpellerViewSettings settings,
        uint pixelWidth,
        uint pixelHeight,
        double dpiScaleX,
        double dpiScaleY)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(settings);

        var resources = new ImpellerViewResources(settings, pixelWidth, pixelHeight, dpiScaleX, dpiScaleY);
        try
        {
            resources.Initialize(owner);
            return resources;
        }
        catch
        {
            TraceLog.Log("[ImpellerViewResources] create failed; rolling back partial state");
            resources.Dispose();
            throw;
        }
    }

    internal void Resize(uint pixelWidth, uint pixelHeight)
    {
        if (_host == null) return;

        TraceLog.Log($"[ImpellerView] resize {PixelWidth}x{PixelHeight} -> {pixelWidth}x{pixelHeight}");

        try { _host.Vk.DeviceWaitIdle(_host.VkDevice); }
        catch (Exception ex) { TraceLog.Log($"[ImpellerView] DeviceWaitIdle on resize threw: {ex.Message}"); }

        ReleaseSwapchainAndSharedImage();

        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;

        _d3dResources!.CreateOrResizeRenderTarget(pixelWidth, pixelHeight);
        AttachBackBuffer();

        _sharedVkImage = new SharedVulkanImage(_host.Vk, _host.KhrExternalMemoryWin32Ext!, _host.VkPhysicalDevice, _host.VkDevice);
        _sharedVkImage.Import(_d3dResources.VulkanSharedHandle, pixelWidth, pixelHeight);

        CreateSurfaceAndSwapchain();
    }

    internal void RebuildForDpi(
        double dpiScaleX,
        double dpiScaleY,
        uint pixelWidth,
        uint pixelHeight,
        DependencyPropertyChangedEventHandler frontBufferHandler)
    {
        DetachFrontBufferHandler(frontBufferHandler);
        DpiScaleX = dpiScaleX;
        DpiScaleY = dpiScaleY;
        _d3dImage = CreateD3DImage();
        AttachFrontBufferHandler(frontBufferHandler);
        Resize(pixelWidth, pixelHeight);
    }

    internal void AttachFrontBufferHandler(DependencyPropertyChangedEventHandler handler)
    {
        if (_d3dImage != null)
            _d3dImage.IsFrontBufferAvailableChanged += handler;
    }

    internal void DetachFrontBufferHandler(DependencyPropertyChangedEventHandler handler)
    {
        if (_d3dImage != null)
            _d3dImage.IsFrontBufferAvailableChanged -= handler;
    }

    internal void ReattachBackBuffer()
    {
        if (_d3dImage == null || _d3dResources == null) return;
        if (!_d3dImage.IsFrontBufferAvailable) return;

        AttachBackBuffer();
    }

    internal void DrawDisplayListAndPresent(ImpellerDisplayList displayList)
    {
        if (_impellerSwapchain == null)
            throw new InvalidOperationException("Impeller swapchain is not available.");

        using var surface = _impellerSwapchain.AcquireNextSurfaceNew()
                            ?? throw new InvalidOperationException("AcquireNextSurfaceNew returned null");
        surface.DrawDisplayList(displayList);
        surface.Present();
    }

    internal void AddDirtyRect()
    {
        _d3dImage?.AddDirtyRect(new Int32Rect(0, 0, _d3dImage.PixelWidth, _d3dImage.PixelHeight));
    }

    internal void LockImage()
    {
        _d3dImage?.Lock();
    }

    internal void UnlockImage()
    {
        _d3dImage?.Unlock();
    }

    public void Dispose()
    {
        if (_host != null)
        {
            try { _host.Vk.DeviceWaitIdle(_host.VkDevice); }
            catch (Exception ex) { TraceLog.Log($"[ImpellerView] DeviceWaitIdle on cleanup threw: {ex.Message}"); }
        }

        ReleaseSwapchainAndSharedImage();

        _d3dImage = null;
        _d3dResources?.Dispose();
        _d3dResources = null;
        _hiddenWindow?.Dispose();
        _hiddenWindow = null;

        if (_host != null)
        {
            ImpellerSharedHost.Release();
            _host = null;
        }
    }

    private void Initialize(ImpellerView owner)
    {
        _host = ImpellerSharedHost.AcquireAndStart(_settings);

        var hostWindow = Window.GetWindow(owner)
            ?? throw new InvalidOperationException(
                "ImpellerView must be hosted inside a Window before InitializeRender() is called. " +
                "If the view lives in a Popup, custom PresentationSource, or has not yet " +
                "been added to a Window's visual tree, defer InitializeRender() until OnLoaded.");
        _hostWindowTitle = hostWindow.Title;

        _d3dResources = new D3DResources();
        _d3dResources.Initialize(new WindowInteropHelper(hostWindow).Handle);
        _d3dResources.CreateOrResizeRenderTarget(PixelWidth, PixelHeight);

        _d3dImage = CreateD3DImage();
        AttachBackBuffer();

        _sharedVkImage = new SharedVulkanImage(_host.Vk, _host.KhrExternalMemoryWin32Ext!, _host.VkPhysicalDevice, _host.VkDevice);
        _sharedVkImage.Import(_d3dResources.VulkanSharedHandle, PixelWidth, PixelHeight);

        CreateSurfaceAndSwapchain();
    }

    private D3DImage CreateD3DImage() => new(96.0 * DpiScaleX, 96.0 * DpiScaleY);

    private void AttachBackBuffer()
    {
        if (_d3dImage == null || _d3dResources == null) return;

        _d3dImage.Lock();
        _d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _d3dResources.BackbufferSurfaceHandle);
        _d3dImage.Unlock();
    }

    private void CreateSurfaceAndSwapchain()
    {
        var host = _host!;

        if (_hiddenWindow == null)
        {
            _hiddenWindow = new HiddenVulkanWindow();
            _hiddenWindow.Create((int)PixelWidth, (int)PixelHeight, _hostWindowTitle);
        }
        else
        {
            _hiddenWindow.Resize((int)PixelWidth, (int)PixelHeight);
        }

        var surfaceInfo = new Silk.NET.Vulkan.Win32SurfaceCreateInfoKHR(
            sType: StructureType.Win32SurfaceCreateInfoKhr,
            hwnd: _hiddenWindow.Hwnd,
            hinstance: host.HiddenHInstance);

        SurfaceKHR surface;
        var r = host.KhrWin32SurfaceExt!.CreateWin32Surface(host.VkInstance, &surfaceInfo, null, out surface);
        if (r != Result.Success)
            throw new InvalidOperationException($"vkCreateWin32SurfaceKHR failed: {r}");
        _vkSurface = surface;

        try
        {
            _blitContext = new BlitContext(
                host.Vk, host.VkDevice, host.VkQueue, host.QueueFamilyIndex,
                _sharedVkImage!.VkImage,
                new Extent2D(PixelWidth, PixelHeight),
                host.SharedQueueLock);
        }
        catch
        {
            host.KhrSurfaceExt!.DestroySurface(host.VkInstance, _vkSurface, null);
            _vkSurface = default;
            throw;
        }

        VkTrampolines.SetPendingBlitContext(_blitContext);

        try
        {
            _impellerSwapchain = host.Context.VulkanSwapchainCreateNew(new IntPtr((long)_vkSurface.Handle));
        }
        catch
        {
            CleanupFailedSwapchainCreate(host);
            throw;
        }

        if (_impellerSwapchain == null)
        {
            CleanupFailedSwapchainCreate(host);
            throw new InvalidOperationException("ImpellerContext.VulkanSwapchainCreateNew returned null.");
        }

        _swapchainHandle = 0;
        foreach (var kv in VkTrampolines.BlitsBySwapchain)
        {
            if (ReferenceEquals(kv.Value, _blitContext))
            {
                _swapchainHandle = kv.Key;
                break;
            }
        }
        TraceLog.Log($"[ImpellerView] swapchain registered (handle=0x{_swapchainHandle:X16})");
    }

    private void CleanupFailedSwapchainCreate(ImpellerSharedHost host)
    {
        VkTrampolines.SetPendingBlitContext(null!);
        _blitContext?.Dispose();
        _blitContext = null;
        if (_vkSurface.Handle != 0)
        {
            host.KhrSurfaceExt!.DestroySurface(host.VkInstance, _vkSurface, null);
            _vkSurface = default;
        }
    }

    private void ReleaseSwapchainAndSharedImage()
    {
        if (_swapchainHandle != 0)
        {
            VkTrampolines.UnregisterSwapchainBlit(_swapchainHandle);
            _swapchainHandle = 0;
        }

        _impellerSwapchain?.Dispose();
        _impellerSwapchain = null;
        _vkSurface = default;

        _blitContext?.Dispose();
        _blitContext = null;
        _sharedVkImage?.Dispose();
        _sharedVkImage = null;
    }
}
