using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using HelloWPFImpellerGallery.Scenes;

using NImpeller.Wpf;

namespace HelloWPFImpellerGallery;

public partial class MainWindow : Window
{
    private const string TitleBase = "HelloWPFImpellerGallery";

    private IGalleryScene? _currentScene;

    // FPS measurement (drives the title bar)
    // Strategy: count frames inside OnRender, recompute every >=500ms of wall-clock
    // (Stopwatch — Background-priority DispatcherTimer skews badly under load).
    // Light EWMA smoothing so the displayed number doesn't flicker frame-to-frame
    // but still tracks real changes within a second or two.
    private readonly Stopwatch _fpsClock = Stopwatch.StartNew();
    private int _frameCount;
    private double _instantFps;
    private double _smoothedFps;
    private const double FpsSampleSeconds = 0.5;
    private const double FpsSmoothing = 0.4; // 0 = no smoothing, 1 = freeze

    // Title-bar refresh — Render priority so it isn't starved by background work
    private readonly DispatcherTimer _titleTimer;

    /// <summary>Remembered "default" item count for each configurable scene (for the reset button).</summary>
    private readonly Dictionary<IConfigurableScene, int> _defaultCounts = new();

    public MainWindow()
    {
        InitializeComponent();

        SceneList.ItemsSource = GalleryScenes.All;
        if (GalleryScenes.All.Count > 0)
        {
            SceneList.SelectedIndex = 0;
            _currentScene = GalleryScenes.All[0];
            UpdateCountControlsVisibility();
        }

        GalleryView.Start();

        // Refresh title 4x/sec at Render priority so the number feels responsive
        // and isn't delayed when the renderer is busy. The actual measurement runs
        // inside OnRender — this timer just pushes the latest value to the title bar.
        _titleTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Render, OnTitleTick, Dispatcher);
        _titleTimer.Start();
    }

    private void SceneList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _currentScene = SceneList.SelectedItem as IGalleryScene;
        UpdateCountControlsVisibility();

        // Reset FPS measurement so a scene switch doesn't carry over old throughput.
        _frameCount = 0;
        _instantFps = 0;
        _smoothedFps = 0;
        _fpsClock.Restart();
    }

    private void UpdateCountControlsVisibility()
    {
        if (_currentScene is IConfigurableScene cs)
        {
            if (!_defaultCounts.ContainsKey(cs))
                _defaultCounts[cs] = cs.ItemCount;

            CountControls.Visibility = Visibility.Visible;
            RefreshCountLabel(cs);
        }
        else
        {
            CountControls.Visibility = Visibility.Collapsed;
        }
    }

    private void RefreshCountLabel(IConfigurableScene cs)
    {
        CountLabel.Text = $"{cs.ItemLabel}: {cs.ItemCount:N0}   (min {cs.ItemMin:N0} • max {cs.ItemMax:N0} • step {cs.ItemStep:N0})";
    }

    private void Adjust(int multiplier)
    {
        if (_currentScene is not IConfigurableScene cs) return;
        cs.ItemCount += cs.ItemStep * multiplier;
        RefreshCountLabel(cs);
    }

    private void OnIncrementClick(object sender, RoutedEventArgs e)    => Adjust(+1);
    private void OnDecrementClick(object sender, RoutedEventArgs e)    => Adjust(-1);
    private void OnIncrementBigClick(object sender, RoutedEventArgs e) => Adjust(+5);
    private void OnDecrementBigClick(object sender, RoutedEventArgs e) => Adjust(-5);

    private void OnZeroClick(object sender, RoutedEventArgs e)
    {
        if (_currentScene is not IConfigurableScene cs) return;
        cs.ItemCount = cs.ItemMin;
        RefreshCountLabel(cs);
    }

    private void OnResetClick(object sender, RoutedEventArgs e)
    {
        if (_currentScene is not IConfigurableScene cs) return;
        if (_defaultCounts.TryGetValue(cs, out var def))
            cs.ItemCount = def;
        RefreshCountLabel(cs);
    }

    private void GalleryView_OnRender(object? s, ImpellerRenderEventArgs e)
    {
        var scene = _currentScene;
        if (scene == null)
        {
            using var paint = NImpeller.ImpellerPaint.New();
            paint?.SetColor(NImpeller.ImpellerColor.FromRgb(0x14, 0x18, 0x1D));
            if (paint != null) e.Builder.DrawPaint(paint);
        }
        else
        {
            scene.Render(e);
        }

        // FPS bookkeeping — runs on the same UI thread as the render, so no locking.
        _frameCount++;
        double elapsed = _fpsClock.Elapsed.TotalSeconds;
        if (elapsed >= FpsSampleSeconds)
        {
            _instantFps = _frameCount / elapsed;
            _smoothedFps = _smoothedFps == 0
                ? _instantFps
                : _smoothedFps * FpsSmoothing + _instantFps * (1 - FpsSmoothing);
            _frameCount = 0;
            _fpsClock.Restart();
        }
    }

    private void OnTitleTick(object? sender, EventArgs e)
    {
        // If no frame has fired for over 2 seconds (e.g. window minimized, or scene render
        // is hanging) the smoothed value is stale — bias it towards zero so the title bar
        // doesn't keep showing a misleadingly high number.
        if (_fpsClock.Elapsed.TotalSeconds > 2.0)
        {
            _smoothedFps *= 0.5;
            if (_smoothedFps < 0.05) _smoothedFps = 0;
        }

        var name = _currentScene?.Name ?? "—";
        Title = $"{TitleBase}  —  {name}  —  {_smoothedFps,5:0.0} fps";
    }
}
