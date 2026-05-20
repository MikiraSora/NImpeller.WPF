using System;
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

    public MainWindow()
    {
        InitializeComponent();

        // Populate the gallery list and select the first item by default.
        SceneList.ItemsSource = GalleryScenes.All;
        if (GalleryScenes.All.Count > 0)
        {
            SceneList.SelectedIndex = 0;
            _currentScene = GalleryScenes.All[0];
        }

        // Start the render surface.
        GalleryView.Start();

        // Refresh window title with current FPS once per second.
        _titleTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, OnTitleTick, Dispatcher);
        _titleTimer.Start();
    }

    private void SceneList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _currentScene = SceneList.SelectedItem as IGalleryScene;
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
