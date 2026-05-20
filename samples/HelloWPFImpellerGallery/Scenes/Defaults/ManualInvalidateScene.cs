using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;

internal sealed class ManualInvalidateScene : IContentGalleryScene
{
    public string Name => "Manual Invalidate";
    public string? Description => "An independent ImpellerView using RenderContinuously = false";

    public void Render(ImpellerRenderEventArgs e)
    {
        SceneHelpers.ClearBg(e.Builder);
    }

    public FrameworkElement CreateContent() => new ManualInvalidatePanel();

    private sealed class ManualInvalidatePanel : Grid
    {
        private readonly ImpellerView _view;
        private TextBlock _statusText = null!;
        private readonly DispatcherTimer _burstTimer;

        private int _manualRequests;
        private int _burstRemaining;
        private long _lastRenderedFrame;

        public ManualInvalidatePanel()
        {
            Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1D, 0x22));

            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var toolbar = BuildToolbar();
            Children.Add(toolbar);

            _view = new ImpellerView();
            _view.Render += OnRender;
            _view.Ready += (_, _) => UpdateStatus("ready");
            Grid.SetRow(_view, 1);
            Children.Add(_view);

            _burstTimer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(150),
                DispatcherPriority.Background,
                OnBurstTick,
                Dispatcher);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private UIElement BuildToolbar()
        {
            var panel = new DockPanel
            {
                LastChildFill = true,
                Background = new SolidColorBrush(Color.FromRgb(0x23, 0x28, 0x30)),
                Margin = new Thickness(0),
            };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(12, 10, 12, 10),
            };
            DockPanel.SetDock(buttons, Dock.Left);
            panel.Children.Add(buttons);

            buttons.Children.Add(CreateButton("Render once", OnRenderOnceClick));
            buttons.Children.Add(CreateButton("Render 10", OnRenderTenClick));
            buttons.Children.Add(CreateButton("Spam 100", OnSpamClick));
            buttons.Children.Add(CreateButton("Clear", OnClearClick));

            _statusText = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(0xD8, 0xDE, 0xE8)),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(12, 0, 14, 0),
                Text = "RenderContinuously = false",
            };
            panel.Children.Add(_statusText);

            return panel;
        }

        private static Button CreateButton(string text, RoutedEventHandler click)
        {
            var button = new Button
            {
                Content = text,
                MinWidth = 92,
                Height = 32,
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(12, 0, 12, 0),
                Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x33, 0x40)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x50, 0x64)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
            };
            button.Click += click;
            return button;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _view.InitializeRender(new ImpellerViewSettings
            {
                RenderContinuously = false,
            });
            _view.InvalidateRender();
            _manualRequests++;
            UpdateStatus("initial render requested");
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _burstTimer.Stop();
            _view.Dispose();
        }

        private void OnRenderOnceClick(object sender, RoutedEventArgs e)
        {
            RequestRender("single render requested");
        }

        private void OnRenderTenClick(object sender, RoutedEventArgs e)
        {
            _burstRemaining = 10;
            _burstTimer.Stop();
            _burstTimer.Start();
            UpdateStatus("10-frame burst started");
        }

        private void OnSpamClick(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 100; i++)
                RequestRender(null);

            UpdateStatus("100 invalidate calls queued");
        }

        private void OnClearClick(object sender, RoutedEventArgs e)
        {
            _manualRequests = 0;
            _lastRenderedFrame = 0;
            _burstRemaining = 0;
            _burstTimer.Stop();
            RequestRender("counters cleared");
        }

        private void OnBurstTick(object? sender, EventArgs e)
        {
            if (_burstRemaining <= 0)
            {
                _burstTimer.Stop();
                UpdateStatus("burst finished");
                return;
            }

            _burstRemaining--;
            RequestRender($"burst frame requested, remaining {_burstRemaining}");
        }

        private void RequestRender(string? status)
        {
            _manualRequests++;
            _view.InvalidateRender();

            if (status != null)
                UpdateStatus(status);
        }

        private void OnRender(object? sender, ImpellerRenderEventArgs e)
        {
            _lastRenderedFrame = e.FrameNumber;
            DrawScene(e);
            UpdateStatus("rendered");
        }

        private void UpdateStatus(string state)
        {
            _statusText.Text = $"{state} | requests: {_manualRequests:N0} | frames: {_lastRenderedFrame:N0}";
        }

        private static void DrawScene(ImpellerRenderEventArgs e)
        {
            var b = e.Builder;
            SceneHelpers.ClearBg(b, 0x16, 0x1B, 0x24);

            float t = (float)e.TotalTime.TotalSeconds;
            float cx = e.PixelWidth / 2f;
            float cy = e.PixelHeight / 2f;
            float radius = MathF.Min(e.PixelWidth, e.PixelHeight) * 0.18f;

            using (var p = ImpellerPaint.New()!)
            {
                for (int i = 0; i < 9; i++)
                {
                    float hue = (i / 9f + e.FrameNumber * 0.035f) % 1f;
                    var (r, g, bb) = SceneHelpers.HsvToRgb(hue, 0.72f, 1.0f);
                    p.SetColor(new ImpellerColor
                    {
                        Alpha = 0.20f + i * 0.055f,
                        Red = r,
                        Green = g,
                        Blue = bb,
                    });

                    float size = radius * (1.1f + i * 0.22f);
                    float angle = t * 0.9f + i * MathF.PI * 2f / 9f;
                    float x = cx + MathF.Cos(angle) * radius * 0.9f - size / 2f;
                    float y = cy + MathF.Sin(angle) * radius * 0.55f - size / 2f;
                    b.DrawOval(new ImpellerRect((int)x, (int)y, (int)size, (int)size), p);
                }

                p.SetColor(ImpellerColor.FromRgb(0xE8, 0xCB, 0x6F));
                p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
                p.SetStrokeWidth(4f * e.DpiScaleX);
                p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapRound);

                float handAngle = e.FrameNumber * 0.42f;
                b.DrawLine(
                    new ImpellerPoint { X = cx, Y = cy },
                    new ImpellerPoint
                    {
                        X = cx + MathF.Cos(handAngle) * radius * 1.65f,
                        Y = cy + MathF.Sin(handAngle) * radius * 1.65f,
                    },
                    p);
            }

            if (e.Typography == null) return;

            TextBasicsScene.DrawSimpleText(
                b,
                e.Typography,
                $"Manual frame {e.FrameNumber}",
                30 * e.DpiScaleX,
                0,
                32 * e.DpiScaleY,
                e.PixelWidth,
                ImpellerColor.FromRgb(0xF2, 0xF4, 0xF8),
                ImpellerFontWeight.kImpellerFontWeight700,
                ImpellerTextAlignment.kImpellerTextAlignmentCenter);

            TextBasicsScene.DrawSimpleText(
                b,
                e.Typography,
                "This ImpellerView only renders after InvalidateRender().",
                15 * e.DpiScaleX,
                0,
                72 * e.DpiScaleY,
                e.PixelWidth,
                ImpellerColor.FromRgb(0xB8, 0xC0, 0xCC),
                align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
        }
    }
}
