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
        Title = $"{TitleBase}  —  V1:{View1.Fps,5:0.0}  V2:{View2.Fps,5:0.0}  V3:{View3.Fps,5:0.0}  V4:{View4.Fps,5:0.0} fps";
    }

    // Four render handlers — all forward to the shared HelloDemoScene with a different
    // time offset so each view is visually distinguishable.
    private void View1_OnRender(object? s, ImpellerRenderEventArgs e) => RenderSceneVariant(e, timeOffset: 0.0f);
    private void View2_OnRender(object? s, ImpellerRenderEventArgs e) => RenderSceneVariant(e, timeOffset: 0.7f);
    private void View3_OnRender(object? s, ImpellerRenderEventArgs e) => RenderSceneVariant(e, timeOffset: 1.4f);
    private void View4_OnRender(object? s, ImpellerRenderEventArgs e) => RenderSceneVariant(e, timeOffset: 2.1f);

    private void RenderSceneVariant(ImpellerRenderEventArgs e, float timeOffset)
    {
        var t = (float)e.TotalTime.TotalSeconds + timeOffset;
        HelloDemoScene.Render(
            e.Builder,
            e.Typography,
            timeSeconds: t,
            width: e.PixelWidth,
            height: e.PixelHeight,
            frameNumber: e.FrameNumber,
            dpiScale: e.DpiScale);
    }
}
