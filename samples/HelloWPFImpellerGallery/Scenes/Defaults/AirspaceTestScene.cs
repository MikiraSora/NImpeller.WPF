using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;

internal sealed class AirspaceTestScene : IContentGalleryScene
{
    public string Name => "Airspace Test";
    public string? Description => "WPF overlay, input, transparency, and hit-test checks above ImpellerView";

    public void Render(ImpellerRenderEventArgs e)
    {
        SceneHelpers.ClearBg(e.Builder);
    }

    public FrameworkElement CreateContent() => new AirspaceTestPanel();

    private sealed class AirspaceTestPanel : Grid
    {
        private readonly ImpellerView _view;

        private Border _probe = null!;
        private Border _probeHeader = null!;
        private TextBlock _frameText = null!;
        private TextBlock _clickText = null!;

        private bool _dragging;
        private bool _renderLoopRunning = true;
        private Point _dragStart;
        private Point _probeStart;
        private int _clickCount;

        public AirspaceTestPanel()
        {
            Background = new SolidColorBrush(Color.FromRgb(0x12, 0x16, 0x1D));
            ClipToBounds = true;

            _view = new ImpellerView();
            _view.Render += OnRender;
            Children.Add(_view);

            var overlay = BuildOverlay();
            Panel.SetZIndex(overlay, 1);
            Children.Add(overlay);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private Grid BuildOverlay()
        {
            var overlay = new Grid();

            overlay.Children.Add(BuildTopBar());

            var controls = BuildControlPanel();
            controls.HorizontalAlignment = HorizontalAlignment.Right;
            controls.VerticalAlignment = VerticalAlignment.Top;
            controls.Margin = new Thickness(0, 74, 18, 0);
            overlay.Children.Add(controls);

            var canvas = new Canvas();
            _probe = BuildProbePanel();
            Canvas.SetLeft(_probe, 56);
            Canvas.SetTop(_probe, 170);
            canvas.Children.Add(_probe);
            overlay.Children.Add(canvas);

            return overlay;
        }

        private Border BuildTopBar()
        {
            var bar = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(14),
                Padding = new Thickness(12, 9, 12, 9),
                Background = new SolidColorBrush(Color.FromArgb(218, 0x22, 0x27, 0x30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x4B, 0x58, 0x68)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7),
            };

            var dock = new DockPanel { LastChildFill = true };
            bar.Child = dock;

            _frameText = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(0xC8, 0xD1, 0xDE)),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Text = "Impeller frames: 0",
            };
            DockPanel.SetDock(_frameText, Dock.Right);
            dock.Children.Add(_frameText);

            dock.Children.Add(new TextBlock
            {
                Foreground = Brushes.White,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Text = "Airspace probe: WPF controls are rendered above the ImpellerView",
            });

            return bar;
        }

        private Border BuildControlPanel()
        {
            var panel = new Border
            {
                Width = 300,
                Padding = new Thickness(14),
                Background = new SolidColorBrush(Color.FromArgb(226, 0x2A, 0x31, 0x3B)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x5D, 0x6A, 0x7D)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7),
            };

            var stack = new StackPanel { Orientation = Orientation.Vertical };
            panel.Child = stack;

            stack.Children.Add(new TextBlock
            {
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                Text = "WPF controls",
            });

            stack.Children.Add(new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(0xB8, 0xC1, 0xCF)),
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 12),
                TextWrapping = TextWrapping.Wrap,
                Text = "These controls should remain visible, translucent, and clickable while Impeller animates behind them.",
            });

            var textBox = new TextBox
            {
                Text = "Type here over the render surface",
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(8, 5, 8, 5),
                MinHeight = 30,
            };
            stack.Children.Add(textBox);

            var combo = new ComboBox
            {
                Margin = new Thickness(0, 0, 0, 8),
                MinHeight = 30,
                SelectedIndex = 0,
            };
            combo.Items.Add("ComboBox item A");
            combo.Items.Add("ComboBox item B");
            combo.Items.Add("ComboBox item C");
            stack.Children.Add(combo);

            var checkbox = new CheckBox
            {
                Content = "Semi-transparent WPF overlay",
                IsChecked = true,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 8),
            };
            checkbox.Checked += (_, _) => panel.Opacity = 1.0;
            checkbox.Unchecked += (_, _) => panel.Opacity = 0.58;
            stack.Children.Add(checkbox);

            stack.Children.Add(new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(0xD8, 0xDE, 0xE8)),
                FontSize = 12,
                Text = "Draggable panel opacity",
            });

            var slider = new Slider
            {
                Minimum = 0.25,
                Maximum = 1.0,
                Value = 0.86,
                TickFrequency = 0.25,
                IsSnapToTickEnabled = false,
                Margin = new Thickness(0, 0, 0, 12),
            };
            slider.ValueChanged += (_, e) =>
            {
                if (_probe != null)
                    _probe.Opacity = e.NewValue;
            };
            stack.Children.Add(slider);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal };
            buttons.Children.Add(CreateButton("Reset", OnResetProbeClick));
            buttons.Children.Add(CreateButton("Stop loop", OnToggleLoopClick));
            stack.Children.Add(buttons);

            return panel;
        }

        private Border BuildProbePanel()
        {
            var border = new Border
            {
                Width = 330,
                Padding = new Thickness(0),
                Opacity = 0.86,
                Background = new SolidColorBrush(Color.FromArgb(232, 0xEA, 0xF4, 0xFF)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0x88, 0xD1)),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(7),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 18,
                    ShadowDepth = 4,
                    Opacity = 0.35,
                },
            };

            var stack = new StackPanel();
            border.Child = stack;

            _probeHeader = new Border
            {
                Padding = new Thickness(12, 8, 12, 8),
                Background = new SolidColorBrush(Color.FromRgb(0x21, 0x67, 0xA6)),
                CornerRadius = new CornerRadius(7, 7, 0, 0),
                Cursor = Cursors.SizeAll,
            };
            _probeHeader.MouseLeftButtonDown += OnProbeMouseDown;
            _probeHeader.MouseMove += OnProbeMouseMove;
            _probeHeader.MouseLeftButtonUp += OnProbeMouseUp;
            stack.Children.Add(_probeHeader);

            _probeHeader.Child = new TextBlock
            {
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                Text = "Drag this WPF panel across the ImpellerView",
            };

            var body = new StackPanel
            {
                Margin = new Thickness(12),
                Orientation = Orientation.Vertical,
            };
            stack.Children.Add(body);

            body.Children.Add(new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(0x13, 0x1A, 0x22)),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Text = "If an airspace issue exists, this panel may disappear behind the render surface or stop receiving mouse input.",
            });

            _clickText = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(0x13, 0x1A, 0x22)),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 10, 0, 8),
                Text = "Button clicks: 0",
            };
            body.Children.Add(_clickText);

            body.Children.Add(CreateButton("Click test", OnClickTest));

            return border;
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
                Cursor = Cursors.Hand,
            };
            button.Click += click;
            return button;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _view.Start();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _view.Dispose();
        }

        private void OnResetProbeClick(object sender, RoutedEventArgs e)
        {
            Canvas.SetLeft(_probe, 56);
            Canvas.SetTop(_probe, 170);
        }

        private void OnToggleLoopClick(object sender, RoutedEventArgs e)
        {
            if (_renderLoopRunning)
            {
                _view.Stop();
                _renderLoopRunning = false;
                ((Button)sender).Content = "Start loop";
            }
            else
            {
                _view.Start();
                _renderLoopRunning = true;
                ((Button)sender).Content = "Stop loop";
            }
        }

        private void OnClickTest(object sender, RoutedEventArgs e)
        {
            _clickCount++;
            _clickText.Text = $"Button clicks: {_clickCount:N0}";
        }

        private void OnProbeMouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragging = true;
            _dragStart = e.GetPosition(this);
            _probeStart = new Point(Canvas.GetLeft(_probe), Canvas.GetTop(_probe));
            _probeHeader.CaptureMouse();
            e.Handled = true;
        }

        private void OnProbeMouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;

            var current = e.GetPosition(this);
            double nextX = _probeStart.X + current.X - _dragStart.X;
            double nextY = _probeStart.Y + current.Y - _dragStart.Y;
            double maxX = Math.Max(0, ActualWidth - _probe.ActualWidth);
            double maxY = Math.Max(0, ActualHeight - _probe.ActualHeight);

            Canvas.SetLeft(_probe, Math.Clamp(nextX, 0, maxX));
            Canvas.SetTop(_probe, Math.Clamp(nextY, 0, maxY));
            e.Handled = true;
        }

        private void OnProbeMouseUp(object sender, MouseButtonEventArgs e)
        {
            _dragging = false;
            _probeHeader.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void OnRender(object? sender, ImpellerRenderEventArgs e)
        {
            DrawImpellerScene(e);

            if (e.FrameNumber % 15 == 0)
                _frameText.Text = $"Impeller frames: {e.FrameNumber:N0}";
        }

        private static void DrawImpellerScene(ImpellerRenderEventArgs e)
        {
            var b = e.Builder;
            SceneHelpers.ClearBg(b, 0x10, 0x14, 0x1D);

            float t = (float)e.TotalTime.TotalSeconds;
            int width = e.PixelWidth;
            int height = e.PixelHeight;

            using (var p = ImpellerPaint.New()!)
            {
                int stripeWidth = Math.Max(24, width / 24);
                for (int i = -1; i <= width / stripeWidth + 1; i++)
                {
                    float hue = (i * 0.045f + t * 0.035f) % 1f;
                    if (hue < 0) hue += 1;
                    var (r, g, bb) = SceneHelpers.HsvToRgb(hue, 0.58f, 0.85f);
                    p.SetColor(new ImpellerColor
                    {
                        Alpha = i % 2 == 0 ? 0.22f : 0.12f,
                        Red = r,
                        Green = g,
                        Blue = bb,
                    });

                    int x = (int)(i * stripeWidth + MathF.Sin(t * 0.8f + i) * stripeWidth * 0.35f);
                    b.DrawRect(new ImpellerRect(x, 0, stripeWidth, height), p);
                }

                for (int i = 0; i < 16; i++)
                {
                    float phase = t * (0.45f + i * 0.025f) + i * 0.7f;
                    float x = width * (0.5f + 0.42f * MathF.Cos(phase * 0.77f));
                    float y = height * (0.5f + 0.36f * MathF.Sin(phase));
                    float size = MathF.Min(width, height) * (0.06f + (i % 5) * 0.018f);
                    var (r, g, bb) = SceneHelpers.HsvToRgb((i / 16f + t * 0.04f) % 1f, 0.74f, 1.0f);
                    p.SetColor(new ImpellerColor
                    {
                        Alpha = 0.34f,
                        Red = r,
                        Green = g,
                        Blue = bb,
                    });
                    b.DrawOval(new ImpellerRect((int)(x - size / 2), (int)(y - size / 2), (int)size, (int)size), p);
                }

                p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
                p.SetStrokeWidth(2f * e.DpiScaleX);
                p.SetColor(new ImpellerColor { Alpha = 0.50f, Red = 1, Green = 1, Blue = 1 });

                for (int y = 80; y < height; y += 80)
                {
                    b.DrawLine(
                        new ImpellerPoint { X = 0, Y = y },
                        new ImpellerPoint { X = width, Y = y },
                        p);
                }
            }

            if (e.Typography == null) return;

            TextBasicsScene.DrawSimpleText(
                b,
                e.Typography,
                "ImpellerView render surface",
                34 * e.DpiScaleX,
                0,
                height - 112 * e.DpiScaleY,
                width,
                ImpellerColor.FromRgb(0xF2, 0xF4, 0xF8),
                ImpellerFontWeight.kImpellerFontWeight700,
                ImpellerTextAlignment.kImpellerTextAlignmentCenter);

            TextBasicsScene.DrawSimpleText(
                b,
                e.Typography,
                "WPF overlays above this animated content should remain visible and interactive.",
                16 * e.DpiScaleX,
                0,
                height - 68 * e.DpiScaleY,
                width,
                ImpellerColor.FromRgb(0xC0, 0xCA, 0xD8),
                align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
        }
    }
}
