using System;
using System.Collections.Generic;
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
    private readonly DispatcherTimer _titleTimer;
    private int _frameCount;
    private double _fps;
    private DateTime _lastFpsSample = DateTime.UtcNow;

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

        _titleTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, OnTitleTick, Dispatcher);
        _titleTimer.Start();
    }

    private void SceneList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _currentScene = SceneList.SelectedItem as IGalleryScene;
        UpdateCountControlsVisibility();
    }

    private void UpdateCountControlsVisibility()
    {
        if (_currentScene is IConfigurableScene cs)
        {
            // Remember the initial count the very first time we see this scene, so the reset button works.
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
        _frameCount++;
    }

    private void OnTitleTick(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastFpsSample).TotalSeconds;
        if (elapsed > 0)
        {
            _fps = _frameCount / elapsed;
            _frameCount = 0;
            _lastFpsSample = now;
        }
        var name = _currentScene?.Name ?? "—";
        Title = $"{TitleBase}  —  {name}  —  {_fps,5:0.0} fps";
    }
}
