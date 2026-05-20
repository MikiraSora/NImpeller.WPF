using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

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
public sealed unsafe class ImpellerView : FrameworkElement, IDisposable
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

    // ---- Resize debounce ----
    private DispatcherTimer? _resizeDebounce;

    // ---- Ticker subscription ----
    private ViewTicker.TickCallback? _tickCallback;
    private bool _frameReadyFired;

    public bool IsStarted => _isStarted;
    public int PixelWidth => (int)_pixelWidth;
    public int PixelHeight => (int)_pixelHeight;
    public double DpiScaleX => _dpiScaleX;
    public double DpiScaleY => _dpiScaleY;
    public long FrameNumber => _frameNumber;

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
    ///
    /// <para><b>Settings lifetime model</b></para>
    /// The first call (before <c>Initialize</c> runs) consumes every field of
    /// <paramref name="settings"/> to build GPU resources and the per-frame ticker.
    /// Subsequent calls do <b>not</b> rebuild anything; they only re-evaluate the
    /// ticker registration based on <see cref="ImpellerViewSettings.RenderContinuously"/>
    /// (typical use: resume after <see cref="Stop"/>). If the view is already ticking,
    /// this method is a no-op for the ticker — call <see cref="Stop"/> first.
    ///
    /// <para><b>Per-field effect on re-Start</b></para>
    /// <list type="bullet">
    ///   <item><see cref="ImpellerViewSettings.EnableValidation"/> — process-wide and
    ///     locked at the very first <c>Start</c> in the process (the underlying
    ///     <c>ImpellerContext</c> is a singleton); later values are silently ignored.</item>
    ///   <item><see cref="ImpellerViewSettings.UseDeviceDpi"/> — the new value is stored
    ///     and will influence future <c>ComputePixelWidth/Height</c> calls (next resize
    ///     or DPI change), but does NOT immediately rebuild the existing swapchain /
    ///     shared texture. To apply at the GPU level, detach the view, wait for
    ///     <c>Unloaded</c>, then re-attach with a fresh <c>Start</c>.</item>
    ///   <item><see cref="ImpellerViewSettings.LogicalSizeOverride"/> — read on the next
    ///     <c>MeasureOverride</c> pass; call <c>InvalidateMeasure</c> to apply sooner.</item>
    ///   <item><see cref="ImpellerViewSettings.RenderContinuously"/> — read here on
    ///     every call to decide whether to (re)register the ticker. Note: if the view
    ///     is already ticking and you change this to <c>false</c>, the ticker keeps
    ///     running until <see cref="Stop"/> is called.</item>
    /// </list>
    /// </summary>
    public void Start(ImpellerViewSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _startRequested = true;

        if (IsLoaded && !_isInitialized)
        {
            Initialize();
            return;
        }

        // Already initialized but ticker was previously unregistered by Stop() —
        // re-register so the view resumes rendering without rebuilding GPU resources.
        if (_isInitialized && _tickCallback == null && _settings.RenderContinuously)
        {
            RegisterToTicker();
            _isStarted = true;
        }
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
        _isStarted = false;
    }

    /// <summary>
    /// Release all native GPU resources (D3D devices, VkImage, swapchain, shared host
    /// refcount, hidden HWND). Equivalent to the <c>Unloaded</c> teardown path, but
    /// callable from code paths where the view will never be added to the visual tree
    /// (or where the host process exits before <c>Unloaded</c> fires). Idempotent.
    /// </summary>
    public void Dispose()
    {
        Teardown();
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

        // Coalesce burst events to ~one resize per frame: WPF can fire SizeChanged tens of
        // times per second during a drag; we collapse them down to a single Recreate at the
        // next dispatcher tick (~16 ms), which is fast enough to look "live" while still
        // saving 30-50 redundant GPU resource rebuilds per second.
        if (_resizeDebounce == null)
        {
            _resizeDebounce = new DispatcherTimer(
                TimeSpan.FromMilliseconds(16),
                DispatcherPriority.Background,
                OnResizeDebounceTick,
                Dispatcher);
        }
        _resizeDebounce.Stop();
        _resizeDebounce.Start();
    }

    private bool _resizeInProgress;

    private void OnResizeDebounceTick(object? sender, EventArgs e)
    {
        _resizeDebounce!.Stop();
        if (!_isInitialized) return;
        if (_resizeInProgress) return; // re-entrancy guard (extreme edge cases)

        var w = ComputePixelWidth();
        var h = ComputePixelHeight();
        if (w == _pixelWidth && h == _pixelHeight) return;
        if (w < 16 || h < 16) return; // skip degenerate sizes

        _resizeInProgress = true;
        Exception? failure = null;
        try
        {
            RecreateForSize(w, h);
        }
        catch (Exception ex)
        {
            failure = ex;
            TraceLog.Log($"[ImpellerView] RecreateForSize threw: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            _resizeInProgress = false;
        }

        if (failure != null)
        {
            // Surface the failure via Application.DispatcherUnhandledException instead
            // of letting the view rot in a half-torn-down state. Inner exception keeps
            // the original Vulkan/D3D diagnostic.
            throw new ImpellerRenderErrorException(
                $"ImpellerView failed to recreate render resources for size {w}x{h}.",
                failure);
        }

        // If another SizeChanged arrived while we were recreating, the timer was
        // restarted; otherwise check once more in case the window kept growing.
        var w2 = ComputePixelWidth();
        var h2 = ComputePixelHeight();
        if ((w2 != _pixelWidth || h2 != _pixelHeight) && w2 >= 16 && h2 >= 16)
        {
            _resizeDebounce!.Stop();
            _resizeDebounce.Start();
        }
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

        try
        {
            // Acquire shared Impeller host
            _host = ImpellerSharedHost.AcquireAndStart(_settings);

            // Per-view D3D resources (D3D9Ex + D3D11 device + shared texture)
            var hostWindow = Window.GetWindow(this)
                ?? throw new InvalidOperationException(
                    "ImpellerView must be hosted inside a Window before Start() is called. " +
                    "If the view lives in a Popup, custom PresentationSource, or has not yet " +
                    "been added to a Window's visual tree, defer Start() until OnLoaded.");
            _d3dResources = new D3DResources();
            _d3dResources.Initialize(new WindowInteropHelper(hostWindow).Handle);
            _d3dResources.CreateOrResizeRenderTarget(_pixelWidth, _pixelHeight);

            // D3DImage with DPI-aware backing so dc.DrawImage maps physical px -> DIP correctly
            _d3dImage = new D3DImage(96.0 * _dpiScaleX, 96.0 * _dpiScaleY);
            // Recover from front-buffer loss (GPU switch, RDP, lock screen, TDR) by
            // re-attaching the back buffer when it becomes available again.
            _d3dImage.IsFrontBufferAvailableChanged += OnFrontBufferAvailabilityChanged;
            _d3dImage.Lock();
            _d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _d3dResources.BackbufferSurfaceHandle);
            _d3dImage.Unlock();

            // Import D3D shared texture as VkImage on Impeller's device
            _sharedVkImage = new SharedVulkanImage(_host.Vk, _host.KhrExternalMemoryWin32Ext!, _host.VkPhysicalDevice, _host.VkDevice);
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
        catch
        {
            // Roll back partial initialization so the shared host refcount, D3D devices,
            // VkImage and the hidden HWND are not leaked. We intentionally do not consult
            // _isInitialized here — it is still false on this path.
            TraceLog.Log("[ImpellerView] Initialize failed; rolling back partial state");
            CleanupResources();
            throw;
        }
    }

    private void OnFrontBufferAvailabilityChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (_d3dImage == null || _d3dResources == null) return;
        if (!_d3dImage.IsFrontBufferAvailable) return;

        // Reattach the back buffer so subsequent frames are visible again.
        // NOTE: this assumes the underlying D3D devices are still valid. Full
        // device-removed recovery (TDR, driver reset, GPU swap) is a follow-up.
        try
        {
            _d3dImage.Lock();
            _d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _d3dResources.BackbufferSurfaceHandle);
            _d3dImage.Unlock();
            InvalidateVisual();
            TraceLog.Log("[ImpellerView] front buffer available again; back buffer reattached");
        }
        catch (Exception ex)
        {
            TraceLog.Log($"[ImpellerView] reattach back buffer threw: {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Per-monitor DPI change: rebuild D3DImage (its dpiX/dpiY are baked at ctor)
    /// and re-create the swapchain + shared texture at the new physical resolution.
    /// Only fires under PerMonitorV2 DPI awareness; on System / unaware DPI modes
    /// the application DPI is fixed and this override is never invoked.
    /// </summary>
    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);

        if (!_isInitialized) return;
        if (Math.Abs(newDpi.DpiScaleX - _dpiScaleX) < 1e-6 &&
            Math.Abs(newDpi.DpiScaleY - _dpiScaleY) < 1e-6) return;

        var prevX = _dpiScaleX;
        var prevY = _dpiScaleY;
        _dpiScaleX = newDpi.DpiScaleX;
        _dpiScaleY = newDpi.DpiScaleY;
        TraceLog.Log($"[ImpellerView] DPI changed {prevX:0.###}x{prevY:0.###} -> {_dpiScaleX:0.###}x{_dpiScaleY:0.###}");

        var pxW = ComputePixelWidth();
        var pxH = ComputePixelHeight();
        if (pxW < 16 || pxH < 16) return; // degenerate; retry on next size/dpi event

        Exception? failure = null;
        try
        {
            // D3DImage's dpiX/dpiY are baked at construction and cannot be mutated,
            // so we replace the instance and re-subscribe the front-buffer recovery hook.
            if (_d3dImage != null)
                _d3dImage.IsFrontBufferAvailableChanged -= OnFrontBufferAvailabilityChanged;
            _d3dImage = new D3DImage(96.0 * _dpiScaleX, 96.0 * _dpiScaleY);
            _d3dImage.IsFrontBufferAvailableChanged += OnFrontBufferAvailabilityChanged;

            // Rebuild swapchain + shared image at the new physical resolution.
            // RecreateForSize re-attaches the back buffer on the new _d3dImage and
            // calls InvalidateVisual so OnRender picks up the new instance.
            RecreateForSize(pxW, pxH);
        }
        catch (Exception ex)
        {
            failure = ex;
            TraceLog.Log($"[ImpellerView] OnDpiChanged rebuild threw: {ex.GetType().Name}: {ex.Message}");
        }

        if (failure != null)
        {
            // Surface via Application.DispatcherUnhandledException, matching the
            // RecreateForSize failure path (H4).
            throw new ImpellerRenderErrorException(
                $"ImpellerView failed to rebuild for DPI change to {_dpiScaleX:0.###}x{_dpiScaleY:0.###}.",
                failure);
        }
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
        try
        {
            _blitContext = new BlitContext(
                host.Vk, host.VkDevice, host.VkQueue, host.QueueFamilyIndex,
                _sharedVkImage!.VkImage,
                new Extent2D(_pixelWidth, _pixelHeight),
                host.SharedQueueLock);
        }
        catch
        {
            // BlitContext ctor failed (CommandPool / CommandBuffer / Fence allocation) —
            // surface was already created above and is owned by us until swapchain takes it.
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
            // Re-throw after cleanup so the outer code paths (Initialize/RecreateForSize)
            // get a proper exception instead of a silently-leaked surface + blit context.
            CleanupFailedSwapchainCreate(host);
            throw;
        }

        if (_impellerSwapchain == null)
        {
            // VulkanSwapchainCreateNew returned null (no exception, just failure). Surface
            // ownership was NOT transferred — clean up to avoid accumulating one leaked
            // VkSurfaceKHR + BlitContext per failed attempt.
            CleanupFailedSwapchainCreate(host);
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

    /// <summary>
    /// Roll back the side effects of <see cref="CreateSurfaceAndSwapchain"/> when
    /// <c>VulkanSwapchainCreateNew</c> never succeeded: clear the pending blit context
    /// (otherwise it leaks into the next view's swapchain create), dispose the BlitContext
    /// (Impeller never took it), and destroy the VkSurfaceKHR ourselves (no swapchain to
    /// take ownership). Safe to call multiple times.
    /// </summary>
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

    private void RecreateForSize(uint pxW, uint pxH)
    {
        if (_host == null) return;
        TraceLog.Log($"[ImpellerView] resize {_pixelWidth}x{_pixelHeight} -> {pxW}x{pxH}");

        try { _host.Vk.DeviceWaitIdle(_host.VkDevice); }
        catch (Exception ex) { TraceLog.Log($"[ImpellerView] DeviceWaitIdle on resize threw: {ex.Message}"); }

        // Tear down only the swapchain + shared image; keep the host.
        // NOTE: Impeller takes ownership of the VkSurfaceKHR passed to VulkanSwapchainCreateNew
        // and destroys it when the swapchain is disposed. Calling vkDestroySurfaceKHR ourselves
        // would be a double-free and corrupts the native heap (0xc0000374).
        if (_swapchainHandle != 0)
        {
            VkTrampolines.UnregisterSwapchainBlit(_swapchainHandle);
            _swapchainHandle = 0;
        }
        _impellerSwapchain?.Dispose();
        _impellerSwapchain = null;
        _blitContext?.Dispose();
        _blitContext = null;
        _vkSurface = default; // Impeller already destroyed the underlying VkSurfaceKHR
        _sharedVkImage?.Dispose();
        _sharedVkImage = null;

        _pixelWidth = pxW;
        _pixelHeight = pxH;

        _d3dResources!.CreateOrResizeRenderTarget(pxW, pxH);
        _d3dImage!.Lock();
        _d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _d3dResources.BackbufferSurfaceHandle);
        _d3dImage.Unlock();

        _sharedVkImage = new SharedVulkanImage(_host.Vk, _host.KhrExternalMemoryWin32Ext!, _host.VkPhysicalDevice, _host.VkDevice);
        _sharedVkImage.Import(_d3dResources.VulkanSharedHandle, pxW, pxH);

        CreateSurfaceAndSwapchain();
        InvalidateVisual();
    }

    private void Teardown()
    {
        _resizeDebounce?.Stop();
        _resizeDebounce = null;
        UnregisterFromTicker();
        if (!_isInitialized) return;
        CleanupResources();
    }

    /// <summary>
    /// Release all per-view native resources. Safe to call from a partially-initialized
    /// state (e.g. when <see cref="Initialize"/> fails midway) — each handle is checked
    /// for null/zero before being released, and <see cref="_isInitialized"/> is NOT
    /// consulted as a guard.
    /// </summary>
    private void CleanupResources()
    {
        if (_host != null)
        {
            try { _host.Vk.DeviceWaitIdle(_host.VkDevice); }
            catch (Exception ex) { TraceLog.Log($"[ImpellerView] DeviceWaitIdle on cleanup threw: {ex.Message}"); }
        }

        if (_swapchainHandle != 0)
        {
            VkTrampolines.UnregisterSwapchainBlit(_swapchainHandle);
            _swapchainHandle = 0;
        }
        _impellerSwapchain?.Dispose();
        _impellerSwapchain = null;

        // Impeller's swapchain dispose owns the VkSurfaceKHR — do not destroy it ourselves.
        _vkSurface = default;

        _blitContext?.Dispose();
        _blitContext = null;
        _sharedVkImage?.Dispose();
        _sharedVkImage = null;
        if (_d3dImage != null)
        {
            _d3dImage.IsFrontBufferAvailableChanged -= OnFrontBufferAvailabilityChanged;
            _d3dImage = null;
        }
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
                dpiScaleX: (float)_dpiScaleX,
                dpiScaleY: (float)_dpiScaleY,
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

            // Fire Ready only on a frame that actually completed successfully.
            // Errors are caught below; firing Ready in the catch path would mislead
            // upstream code that uses Ready as a "first usable frame" signal.
            if (!_frameReadyFired)
            {
                _frameReadyFired = true;
                Ready?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            TraceLog.Log($"[ImpellerView] frame render failed: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            _d3dImage.Unlock();
        }
    }
}
