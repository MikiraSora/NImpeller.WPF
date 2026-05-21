using System;
using System.Windows;
using System.Windows.Media;

using NImpeller;
using NImpeller.Wpf.Interop;

namespace NImpeller.Wpf;

/// <summary>
/// WPF control that hosts an Impeller Vulkan render surface and displays it through
/// <see cref="System.Windows.Interop.D3DImage"/>. Multiple views may coexist; each view
/// owns its per-view render resources while sharing the process-wide Impeller host.
/// </summary>
public sealed unsafe class ImpellerView : FrameworkElement, IDisposable
{
    /// <summary>Fires once per rendered frame on the UI thread.</summary>
    public event EventHandler<ImpellerRenderEventArgs>? Render;

    /// <summary>Fires after the first frame is drawn and presented successfully.</summary>
    public event EventHandler? Ready;

    private readonly ImpellerRenderLoop _renderLoop;
    private readonly ImpellerResizeScheduler _resizeScheduler;
    private ImpellerViewSettings _settings = new();
    private ImpellerViewResources? _resources;
    private bool _initializeRequested;
    private bool _isInitialized;
    private bool _frameReadyFired;
    private double _dpiScaleX = 1.0;
    private double _dpiScaleY = 1.0;
    private uint _pixelWidth;
    private uint _pixelHeight;

    /// <summary>True while this view is registered for continuous rendering.</summary>
    public bool IsStarted => _renderLoop.IsRunning;

    /// <summary>Current backing render-target width in pixels, or 0 before initialization.</summary>
    public int PixelWidth => (int)_pixelWidth;

    /// <summary>Current backing render-target height in pixels, or 0 before initialization.</summary>
    public int PixelHeight => (int)_pixelHeight;

    /// <summary>Current horizontal DPI scale used to convert DIPs to physical pixels.</summary>
    public double DpiScaleX => _dpiScaleX;

    /// <summary>Current vertical DPI scale used to convert DIPs to physical pixels.</summary>
    public double DpiScaleY => _dpiScaleY;

    /// <summary>Number assigned to the most recently rendered frame. The first rendered frame is 1.</summary>
    public long FrameNumber => _renderLoop.FrameNumber;

    /// <summary>Create an uninitialized Impeller view. Call <see cref="InitializeRender()"/> or <see cref="Start"/> to initialize rendering.</summary>
    public ImpellerView()
    {
        _renderLoop = new ImpellerRenderLoop(Dispatcher, RenderOneFrame);
        _resizeScheduler = new ImpellerResizeScheduler(
            Dispatcher,
            () => (ComputePixelWidth(), ComputePixelHeight()),
            () => (_resources?.PixelWidth ?? _pixelWidth, _resources?.PixelHeight ?? _pixelHeight),
            ResizeResources);

        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
    }

    /// <summary>Initialize the view with default settings.</summary>
    public void InitializeRender() => InitializeRender(new ImpellerViewSettings());

    /// <summary>
    /// Initialize GPU resources for this view. Calling initialize more than once on the
    /// same attached view is invalid; use <see cref="Start"/> and <see cref="Stop"/> to
    /// control continuous rendering after initialization. If called before the control is
    /// loaded, initialization is deferred until <c>Loaded</c>.
    /// </summary>
    public void InitializeRender(ImpellerViewSettings settings)
    {
        if (_isInitialized || _initializeRequested)
            throw new InvalidOperationException(
                "ImpellerView has already been initialized or scheduled for initialization. " +
                "Call Start() / Stop() to control the continuous render loop; create a new ImpellerView to initialize with different settings.");

        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _initializeRequested = true;
        _renderLoop.SetStartRequested(_settings.RenderContinuously);

        if (IsLoaded && !_isInitialized)
            Initialize();
    }

    /// <summary>
    /// Start continuous rendering. If initialization was already requested, those settings
    /// are used; otherwise the view initializes with default settings. If the view is not
    /// loaded yet, startup is deferred until <c>Loaded</c>.
    /// </summary>
    public void Start()
    {
        _renderLoop.RequestStart();

        if (!_isInitialized)
        {
            if (!_initializeRequested)
            {
                InitializeRender(new ImpellerViewSettings());
            }
            else if (IsLoaded)
            {
                Initialize();
            }
            return;
        }

        _renderLoop.Start();
    }

    /// <summary>Request one redraw without starting the continuous render loop. No-op before initialization.</summary>
    public void InvalidateRender()
    {
        if (!_isInitialized) return;
        _renderLoop.InvalidateRender();
    }

    /// <summary>Stop continuous rendering. GPU resources stay alive; call <see cref="Start"/> to resume.</summary>
    public void Stop()
    {
        _renderLoop.Stop();
    }

    /// <summary>Release all per-view native resources. Idempotent.</summary>
    public void Dispose()
    {
        Teardown();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_settings.LogicalSizeOverride is { } sz) return sz;
        var w = double.IsPositiveInfinity(availableSize.Width) ? 0 : availableSize.Width;
        var h = double.IsPositiveInfinity(availableSize.Height) ? 0 : availableSize.Height;
        return new Size(w, h);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var image = _resources?.D3DImage;
        if (image == null) return;
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        dc.DrawImage(image, new Rect(0, 0, ActualWidth, ActualHeight));
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);

        if (!_isInitialized || _resources == null) return;
        if (Math.Abs(newDpi.DpiScaleX - _dpiScaleX) < 1e-6 &&
            Math.Abs(newDpi.DpiScaleY - _dpiScaleY) < 1e-6) return;

        var prevX = _dpiScaleX;
        var prevY = _dpiScaleY;
        _dpiScaleX = newDpi.DpiScaleX;
        _dpiScaleY = newDpi.DpiScaleY;
        TraceLog.Log($"[ImpellerView] DPI changed {prevX:0.###}x{prevY:0.###} -> {_dpiScaleX:0.###}x{_dpiScaleY:0.###}");

        var pxW = ComputePixelWidth();
        var pxH = ComputePixelHeight();
        if (pxW < 16 || pxH < 16) return;

        try
        {
            _pixelWidth = pxW;
            _pixelHeight = pxH;
            _resources.RebuildForDpi(_dpiScaleX, _dpiScaleY, pxW, pxH, OnFrontBufferAvailabilityChanged);
            InvalidateVisual();
        }
        catch (Exception ex)
        {
            TraceLog.Log($"[ImpellerView] OnDpiChanged rebuild threw: {ex.GetType().Name}: {ex.Message}");
            throw new ImpellerRenderErrorException(
                $"ImpellerView failed to rebuild for DPI change to {_dpiScaleX:0.###}x{_dpiScaleY:0.###}.",
                ex);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initializeRequested && !_isInitialized)
            Initialize();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Teardown();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!_isInitialized) return;
        _resizeScheduler.ScheduleIfChanged();
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

        UpdateDpiFromPresentationSource();

        _pixelWidth = ComputePixelWidth();
        _pixelHeight = ComputePixelHeight();
        TraceLog.Log($"[ImpellerView] initial render target = {_pixelWidth}x{_pixelHeight} physical px");

        try
        {
            _resources = ImpellerViewResources.Create(this, _settings, _pixelWidth, _pixelHeight, _dpiScaleX, _dpiScaleY);
            _resources.AttachFrontBufferHandler(OnFrontBufferAvailabilityChanged);
            _renderLoop.RestartClock();
            _isInitialized = true;
            _resizeScheduler.IsEnabled = true;

            if (_renderLoop.StartRequested)
                _renderLoop.Start();

            InvalidateVisual();
        }
        catch
        {
            TraceLog.Log("[ImpellerView] Initialize failed; rolling back partial state");
            _resources?.DetachFrontBufferHandler(OnFrontBufferAvailabilityChanged);
            _resources?.Dispose();
            _resources = null;
            _isInitialized = false;
            _resizeScheduler.IsEnabled = false;
            _renderLoop.Suspend();
            throw;
        }
    }

    private void UpdateDpiFromPresentationSource()
    {
        var src = PresentationSource.FromVisual(this);
        if (src?.CompositionTarget != null)
        {
            var m = src.CompositionTarget.TransformToDevice;
            _dpiScaleX = m.M11;
            _dpiScaleY = m.M22;
        }
        TraceLog.Log($"[ImpellerView] DPI scale = {_dpiScaleX:0.###} x {_dpiScaleY:0.###}");
    }

    private void OnFrontBufferAvailabilityChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (_resources?.D3DImage is not { IsFrontBufferAvailable: true }) return;

        try
        {
            _resources.ReattachBackBuffer();
            InvalidateVisual();
            TraceLog.Log("[ImpellerView] front buffer available again; back buffer reattached");
        }
        catch (Exception ex)
        {
            TraceLog.Log($"[ImpellerView] reattach back buffer threw: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void ResizeResources(uint pixelWidth, uint pixelHeight)
    {
        if (!_isInitialized || _resources == null) return;

        try
        {
            _pixelWidth = pixelWidth;
            _pixelHeight = pixelHeight;
            _resources.Resize(pixelWidth, pixelHeight);
            InvalidateVisual();
        }
        catch (Exception ex)
        {
            TraceLog.Log($"[ImpellerView] resize threw: {ex.GetType().Name}: {ex.Message}");
            throw new ImpellerRenderErrorException(
                $"ImpellerView failed to recreate render resources for size {pixelWidth}x{pixelHeight}.",
                ex);
        }
    }

    private void Teardown()
    {
        _resizeScheduler.Stop();
        _resizeScheduler.IsEnabled = false;
        _renderLoop.Suspend();

        if (!_isInitialized && _resources == null) return;

        _resources?.DetachFrontBufferHandler(OnFrontBufferAvailabilityChanged);
        _resources?.Dispose();
        _resources = null;
        _isInitialized = false;
        _frameReadyFired = false;
    }

    private void RenderOneFrame()
    {
        var resources = _resources;
        if (!_isInitialized || resources == null || !resources.CanRender) return;
        if (Visibility != Visibility.Visible) return;
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        if (resources.D3DImage is not { IsFrontBufferAvailable: true }) return;

        var timing = _renderLoop.AdvanceFrame();

        resources.LockImage();
        try
        {
            using var builder = ImpellerDisplayListBuilder.New(new ImpellerRect(0, 0, (int)resources.PixelWidth, (int)resources.PixelHeight))
                                ?? throw new InvalidOperationException("ImpellerDisplayListBuilder.New returned null");

            var args = new ImpellerRenderEventArgs(
                source: this,
                builder: builder,
                typography: resources.Typography,
                pixelWidth: (int)resources.PixelWidth,
                pixelHeight: (int)resources.PixelHeight,
                dpiScaleX: (float)_dpiScaleX,
                dpiScaleY: (float)_dpiScaleY,
                deltaTime: timing.DeltaTime,
                totalTime: timing.TotalTime,
                frameNumber: timing.FrameNumber);

            Render?.Invoke(this, args);

            using var displayList = builder.CreateDisplayListNew()
                                    ?? throw new InvalidOperationException("CreateDisplayListNew returned null");
            resources.DrawDisplayListAndPresent(displayList);
            resources.AddDirtyRect();

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
            resources.UnlockImage();
        }
    }
}
