using System;
using System.Windows;
using System.Windows.Threading;

using HelloWPFImpeller.Scenes;

using NImpeller.Wpf;

namespace HelloWPFImpeller;

public partial class MainWindow : Window
{
    private const string TitleBase = "HelloWPFImpeller — 4× ImpellerView";

    private readonly DispatcherTimer _titleTimer;
    private readonly int[] _frameCount = new int[4];
    private readonly double[] _fps = new double[4];
    private DateTime _lastFpsSample = DateTime.UtcNow;

    public MainWindow()
    {
        InitializeComponent();

        // Start all four views with default settings.
        View1.Start();
        View2.Start();
        View3.Start();
        View4.Start();

        // Refresh window title with each view's FPS once per second.
        _titleTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, OnTitleTick, Dispatcher);
        _titleTimer.Start();
    }

    private void OnTitleTick(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastFpsSample).TotalSeconds;
        if (elapsed > 0)
        {
            for (int i = 0; i < 4; i++)
            {
                _fps[i] = _frameCount[i] / elapsed;
                _frameCount[i] = 0;
            }
            _lastFpsSample = now;
        }
        Title = $"{TitleBase}  —  V1:{_fps[0],5:0.0}  V2:{_fps[1],5:0.0}  V3:{_fps[2],5:0.0}  V4:{_fps[3],5:0.0} fps";
    }

    // Four render handlers — all forward to the shared HelloDemoScene with a different
    // time offset so each view is visually distinguishable.
    private void View1_OnRender(object? s, ImpellerRenderEventArgs e) => RenderSceneVariant(e, viewIndex: 0, timeOffset: 0.0f);
    private void View2_OnRender(object? s, ImpellerRenderEventArgs e) => RenderSceneVariant(e, viewIndex: 1, timeOffset: 0.7f);
    private void View3_OnRender(object? s, ImpellerRenderEventArgs e) => RenderSceneVariant(e, viewIndex: 2, timeOffset: 1.4f);
    private void View4_OnRender(object? s, ImpellerRenderEventArgs e) => RenderSceneVariant(e, viewIndex: 3, timeOffset: 2.1f);

    private void RenderSceneVariant(ImpellerRenderEventArgs e, int viewIndex, float timeOffset)
    {
        var t = (float)e.TotalTime.TotalSeconds + timeOffset;
        HelloDemoScene.Render(
            e.Builder,
            e.Typography,
            timeSeconds: t,
            width: e.PixelWidth,
            height: e.PixelHeight,
            frameNumber: e.FrameNumber,
            dpiScale: e.DpiScaleX);
        _frameCount[viewIndex]++;
    }
}
