using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

using NImpeller;
using NImpeller.Wpf.Interop;

using Silk.NET.Vulkan;

namespace NImpeller.Wpf;

/// <summary>
/// A WPF control that hosts an Impeller (Vulkan backend) render surface and displays the
/// result via D3DImage. Multiple <c>ImpellerView</c> instances may coexist in the same
/// window; they share a single <see cref="ImpellerSharedHost"/> (one ImpellerContext per
/// process) but each owns its own swapchain + shared D3D texture.
///
/// Usage:
/// <code>
/// &lt;imp:ImpellerView x:Name="View1" Render="View1_OnRender"/&gt;
/// </code>
/// In code-behind, call <c>View1.Start()</c> (or <c>Start(new ImpellerViewSettings { ... })</c>)
/// after <c>InitializeComponent()</c>. The <see cref="Render"/> event fires once per frame
/// while the view is loaded and visible.
/// </summary>
public sealed unsafe class ImpellerView : FrameworkElement
{
    /// <summary>Fires once per frame on the UI thread. Issue Impeller draw calls here.</summary>
    public event EventHandler<ImpellerRenderEventArgs>? Render;

    /// <summary>Fires once after the first successful frame is rendered + presented.</summary>
    public event EventHandler? Ready;

    private ImpellerViewSettings _settings = new();
    private bool _startRequested;
    private bool _isStarted;
    private bool _isInitialized;

    // ---- DPI / pixel size ----
    private double _dpiScaleX = 1.0;
    private double _dpiScaleY = 1.0;
    private uint _pixelWidth;
    private uint _pixelHeight;

    // ---- Per-view resources ----
    private ImpellerSharedHost? _host;
    private D3DResources? _d3dResources;
    private SharedVulkanImage? _sharedVkImage;
    private HiddenVulkanWindow? _hiddenWindow;
    private SurfaceKHR _vkSurface;
    private ImpellerVulkanSwapchain? _impellerSwapchain;
    private ulong _swapchainHandle;
    private BlitContext? _blitContext;
    private D3DImage? _d3dImage;

    // ---- Timing ----
    private readonly Stopwatch _stopwatch = new();
    private TimeSpan _lastFrameTime = TimeSpan.Zero;
    private long _frameNumber;

    // ---- FPS tracking ----
    private int _fpsFrameCount;
    private TimeSpan _fpsLastSample = TimeSpan.Zero;
    private double _fps;

    // ---- Ticker subscription ----
    private ViewTicker.TickCallback? _tickCallback;
    private bool _frameReadyFired;

    public bool IsStarted => _isStarted;
    public int PixelWidth => (int)_pixelWidth;
    public int PixelHeight => (int)_pixelHeight;
    public double DpiScaleX => _dpiScaleX;
    public double DpiScaleY => _dpiScaleY;
    public long FrameNumber => _frameNumber;

    /// <summary>Average frames per second over the last ~1 second window. Updates after each window closes.</summary>
    public double Fps => _fps;

    /// <summary>Raised on the UI thread roughly once per second after <see cref="Fps"/> has been recomputed.</summary>
    public event EventHandler? FpsUpdated;

    public ImpellerView()
    {
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
    }

    /// <summary>Start the view with default settings.</summary>
    public void Start() => Start(new ImpellerViewSettings());

    /// <summary>
    /// Start the view with the given settings. Safe to call before or after the view is
    /// loaded — if called early, initialization is deferred until <c>Loaded</c>.
    /// </summary>
    public void Start(ImpellerViewSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _startRequested = true;
        if (IsLoaded && !_isInitialized)
            Initialize();
    }

    /// <summary>Request a redraw. Only meaningful when <c>RenderContinuously = false</c>.</summary>
    public void InvalidateRender()
    {
        if (!_isInitialized) return;
        Dispatcher.BeginInvoke(new Action(RenderOneFrame));
    }

    /// <summary>Stop continuous rendering. Resources are kept; call <see cref="Start()"/> to resume.</summary>
    public void Stop()
    {
        UnregisterFromTicker();
    }

    // ============================================================
    // Layout
    // ============================================================
    protected override Size MeasureOverride(Size availableSize)
    {
        if (_settings.LogicalSizeOverride is { } sz) return sz;
        var w = double.IsPositiveInfinity(availableSize.Width) ? 0 : availableSize.Width;
        var h = double.IsPositiveInfinity(availableSize.Height) ? 0 : availableSize.Height;
        return new Size(w, h);
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (_d3dImage == null) return;
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        dc.DrawImage(_d3dImage, new Rect(0, 0, ActualWidth, ActualHeight));
    }

    // ============================================================
    // Lifecycle
    // ============================================================
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_startRequested && !_isInitialized)
            Initialize();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Teardown();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!_isInitialized) return;

        var newPxW = ComputePixelWidth();
        var newPxH = ComputePixelHeight();
        if (newPxW == _pixelWidth && newPxH == _pixelHeight) return;

        // Defer so we coalesce burst events (e.g. window drag-resize)
        Dispatcher.BeginInvoke(new Action(() =>
        {
            var w = ComputePixelWidth();
            var h = ComputePixelHeight();
            if (w == _pixelWidth && h == _pixelHeight) return;
            RecreateForSize(w, h);
        }));
    }

    private uint ComputePixelWidth()
    {
        var dipW = ActualWidth > 0 ? ActualWidth : 1.0;
        return Math.Max(1u, (uint)Math.Round(dipW * (_settings.UseDeviceDpi ? _dpiScaleX : 1.0)));
    }

    private uint ComputePixelHeight()
    {
        var dipH = ActualHeight > 0 ? ActualHeight : 1.0;
        return Math.Max(1u, (uint)Math.Round(dipH * (_settings.UseDeviceDpi ? _dpiScaleY : 1.0)));
    }

    private void Initialize()
    {
        if (_isInitialized) return;

        // Detect DPI
        var src = PresentationSource.FromVisual(this);
        if (src?.CompositionTarget != null)
        {
            var m = src.CompositionTarget.TransformToDevice;
            _dpiScaleX = m.M11;
            _dpiScaleY = m.M22;
        }
        TraceLog.Log($"[ImpellerView] DPI scale = {_dpiScaleX:0.###} x {_dpiScaleY:0.###}");

        _pixelWidth = ComputePixelWidth();
        _pixelHeight = ComputePixelHeight();
        TraceLog.Log($"[ImpellerView] initial render target = {_pixelWidth}x{_pixelHeight} physical px");

        // Acquire shared Impeller host
        _host = ImpellerSharedHost.AcquireAndStart(_settings);

        // Per-view D3D resources (D3D9Ex + D3D11 device + shared texture)
        _d3dResources = new D3DResources();
        _d3dResources.Initialize(new WindowInteropHelper(Window.GetWindow(this)!).Handle);
        _d3dResources.CreateOrResizeRenderTarget(_pixelWidth, _pixelHeight);

        // D3DImage with DPI-aware backing so dc.DrawImage maps physical px -> DIP correctly
        _d3dImage = new D3DImage(96.0 * _dpiScaleX, 96.0 * _dpiScaleY);
        _d3dImage.Lock();
        _d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _d3dResources.BackbufferSurfaceHandle);
        _d3dImage.Unlock();

        // Import D3D shared texture as VkImage on Impeller's device
        _sharedVkImage = new SharedVulkanImage(_host.Vk, _host.VkPhysicalDevice, _host.VkDevice);
        _sharedVkImage.Import(_d3dResources.VulkanSharedHandle, _pixelWidth, _pixelHeight);

        // Per-view VkSurfaceKHR (on the shared hidden HWND), Impeller swapchain, blit context
        CreateSurfaceAndSwapchain();

        _stopwatch.Restart();
        _isInitialized = true;
        _isStarted = true;

        if (_settings.RenderContinuously)
            RegisterToTicker();

        InvalidateVisual();
    }

    private void CreateSurfaceAndSwapchain()
    {
        var host = _host!;

        // Per-view hidden HWND, sized to the view's current physical pixel size so that
        // vkGetPhysicalDeviceSurfaceCapabilitiesKHR returns the right currentExtent.
        if (_hiddenWindow == null)
        {
            _hiddenWindow = new HiddenVulkanWindow();
            _hiddenWindow.Create((int)_pixelWidth, (int)_pixelHeight);
        }
        else
        {
            _hiddenWindow.Resize((int)_pixelWidth, (int)_pixelHeight);
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

        // Per-view BlitContext, stashed in ThreadStatic so the vkCreateSwapchainKHR trampoline
        // can bind it to the freshly created swapchain handle.
        _blitContext = new BlitContext(
            host.Vk, host.VkDevice, host.VkQueue, host.QueueFamilyIndex,
            _sharedVkImage!.VkImage,
            new Extent2D(_pixelWidth, _pixelHeight),
            host.QueueSubmitLock);
        VkTrampolines.SetPendingBlitContext(_blitContext);

        _impellerSwapchain = host.Context.VulkanSwapchainCreateNew(new IntPtr((long)_vkSurface.Handle));
        if (_impellerSwapchain == null)
        {
            VkTrampolines.SetPendingBlitContext(null!);
            throw new InvalidOperationException("ImpellerContext.VulkanSwapchainCreateNew returned null.");
        }

        // Recover the registered swapchain handle so we can unregister later.
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

    private void RecreateForSize(uint pxW, uint pxH)
    {
        if (_host == null) return;
        TraceLog.Log($"[ImpellerView] resize {_pixelWidth}x{_pixelHeight} -> {pxW}x{pxH}");

        try { _host.Vk.DeviceWaitIdle(_host.VkDevice); }
        catch (Exception ex) { TraceLog.Log($"[ImpellerView] DeviceWaitIdle on resize threw: {ex.Message}"); }

        // Tear down only the swapchain + surface + shared image; keep the host.
        if (_swapchainHandle != 0)
        {
            VkTrampolines.UnregisterSwapchainBlit(_swapchainHandle);
            _swapchainHandle = 0;
        }
        _impellerSwapchain?.Dispose();
        _impellerSwapchain = null;
        _blitContext?.Dispose();
        _blitContext = null;
        if (_vkSurface.Handle != 0)
        {
            _host.KhrSurfaceExt!.DestroySurface(_host.VkInstance, _vkSurface, null);
            _vkSurface = default;
        }
        _sharedVkImage?.Dispose();
        _sharedVkImage = null;

        _pixelWidth = pxW;
        _pixelHeight = pxH;

        _d3dResources!.CreateOrResizeRenderTarget(pxW, pxH);
        _d3dImage!.Lock();
        _d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _d3dResources.BackbufferSurfaceHandle);
        _d3dImage.Unlock();

        _sharedVkImage = new SharedVulkanImage(_host.Vk, _host.VkPhysicalDevice, _host.VkDevice);
        _sharedVkImage.Import(_d3dResources.VulkanSharedHandle, pxW, pxH);

        CreateSurfaceAndSwapchain();
        InvalidateVisual();
    }

    private void Teardown()
    {
        UnregisterFromTicker();
        if (!_isInitialized) return;

        if (_host != null)
        {
            try { _host.Vk.DeviceWaitIdle(_host.VkDevice); }
            catch (Exception ex) { TraceLog.Log($"[ImpellerView] DeviceWaitIdle on teardown threw: {ex.Message}"); }
        }

        if (_swapchainHandle != 0)
        {
            VkTrampolines.UnregisterSwapchainBlit(_swapchainHandle);
            _swapchainHandle = 0;
        }
        _impellerSwapchain?.Dispose();
        _impellerSwapchain = null;

        if (_vkSurface.Handle != 0 && _host != null)
        {
            _host.KhrSurfaceExt!.DestroySurface(_host.VkInstance, _vkSurface, null);
            _vkSurface = default;
        }

        _blitContext?.Dispose();
        _blitContext = null;
        _sharedVkImage?.Dispose();
        _sharedVkImage = null;
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

        _isInitialized = false;
        _isStarted = false;
        _frameReadyFired = false;
    }

    // ============================================================
    // Ticker registration
    // ============================================================
    private void RegisterToTicker()
    {
        if (_tickCallback != null) return;
        _tickCallback = RenderOneFrame;
        ViewTicker.Register(_tickCallback);
    }

    private void UnregisterFromTicker()
    {
        if (_tickCallback == null) return;
        ViewTicker.Unregister(_tickCallback);
        _tickCallback = null;
    }

    // ============================================================
    // Per-frame render
    // ============================================================
    private void RenderOneFrame()
    {
        if (!_isInitialized || _impellerSwapchain == null || _d3dImage == null) return;
        if (Visibility != Visibility.Visible) return;
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        if (!_d3dImage.IsFrontBufferAvailable) return;

        var totalTime = _stopwatch.Elapsed;
        var deltaTime = totalTime - _lastFrameTime;
        _lastFrameTime = totalTime;
        _frameNumber++;

        _d3dImage.Lock();
        try
        {
            using var builder = ImpellerDisplayListBuilder.New(new ImpellerRect(0, 0, (int)_pixelWidth, (int)_pixelHeight))
                                ?? throw new InvalidOperationException("ImpellerDisplayListBuilder.New returned null");

            var args = new ImpellerRenderEventArgs(
                source: this,
                builder: builder,
                typography: _host!.Typography,
                pixelWidth: (int)_pixelWidth,
                pixelHeight: (int)_pixelHeight,
                dpiScale: (float)_dpiScaleX,
                deltaTime: deltaTime,
                totalTime: totalTime,
                frameNumber: _frameNumber);

            Render?.Invoke(this, args);

            using var displayList = builder.CreateDisplayListNew()
                                    ?? throw new InvalidOperationException("CreateDisplayListNew returned null");
            using var surface = _impellerSwapchain.AcquireNextSurfaceNew()
                                ?? throw new InvalidOperationException("AcquireNextSurfaceNew returned null");
            surface.DrawDisplayList(displayList);
            surface.Present(); // -> QueuePresentKHR trampoline blits into shared D3D texture

            _d3dImage.AddDirtyRect(new Int32Rect(0, 0, _d3dImage.PixelWidth, _d3dImage.PixelHeight));
        }
        catch (Exception ex)
        {
            TraceLog.Log($"[ImpellerView] frame render failed: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            _d3dImage.Unlock();
        }

        if (!_frameReadyFired)
        {
            _frameReadyFired = true;
            Ready?.Invoke(this, EventArgs.Empty);
        }

        UpdateFps(totalTime);
    }

    private void UpdateFps(TimeSpan now)
    {
        _fpsFrameCount++;
        var elapsed = now - _fpsLastSample;
        if (elapsed.TotalSeconds >= 1.0)
        {
            _fps = _fpsFrameCount / elapsed.TotalSeconds;
            _fpsFrameCount = 0;
            _fpsLastSample = now;
            FpsUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}
