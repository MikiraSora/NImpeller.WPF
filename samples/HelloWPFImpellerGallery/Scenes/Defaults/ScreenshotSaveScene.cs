using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using Microsoft.Win32;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;

internal sealed class ScreenshotSaveScene : IContentGalleryScene
{
    public string Name => "Screenshot PNG";
    public string? Description => "Captures the current ImpellerView pixels and writes them to a PNG file";

    public void Render(ImpellerRenderEventArgs e)
    {
        SceneHelpers.ClearBg(e.Builder);
    }

    public FrameworkElement CreateContent() => new ScreenshotPanel();

    private sealed class ScreenshotPanel : Grid
    {
        private readonly ImpellerView _view = new();
        private readonly TextBlock _statusText = new();
        private string? _pendingSavePath;

        public ScreenshotPanel()
        {
            Background = new SolidColorBrush(Color.FromRgb(0x12, 0x16, 0x1D));
            ClipToBounds = true;

            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Children.Add(BuildToolbar());

            _view.Render += OnRender;
            Grid.SetRow(_view, 1);
            Children.Add(_view);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private UIElement BuildToolbar()
        {
            var bar = new Border
            {
                Padding = new Thickness(12, 9, 12, 9),
                Background = new SolidColorBrush(Color.FromRgb(0x22, 0x27, 0x30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x3C, 0x47, 0x56)),
                BorderThickness = new Thickness(0, 0, 0, 1),
            };

            var dock = new DockPanel { LastChildFill = true };
            bar.Child = dock;

            var button = new Button
            {
                Content = "Save PNG",
                MinWidth = 104,
                Height = 32,
                Padding = new Thickness(14, 0, 14, 0),
            };
            button.Click += OnSaveClick;
            DockPanel.SetDock(button, Dock.Right);
            dock.Children.Add(button);

            _statusText.Foreground = new SolidColorBrush(Color.FromRgb(0xB8, 0xC4, 0xD2));
            _statusText.FontSize = 12;
            _statusText.VerticalAlignment = VerticalAlignment.Center;
            _statusText.Margin = new Thickness(0, 0, 14, 0);
            DockPanel.SetDock(_statusText, Dock.Right);
            dock.Children.Add(_statusText);

            dock.Children.Add(new TextBlock
            {
                Text = "Screenshot PNG",
                Foreground = Brushes.White,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
            });

            return bar;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _view.Start();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _view.Dispose();
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = ".png",
                FileName = $"NImpellerGallery-{DateTime.Now:yyyyMMdd-HHmmss}.png",
                Filter = "PNG image (*.png)|*.png",
                OverwritePrompt = true,
            };

            var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            if (!string.IsNullOrWhiteSpace(pictures) && Directory.Exists(pictures))
                dialog.InitialDirectory = pictures;

            if (dialog.ShowDialog(Window.GetWindow(this)) != true)
                return;

            _pendingSavePath = dialog.FileName;
            _statusText.Text = "Saving...";
            _view.InvalidateRender();
        }

        private void OnRender(object? sender, ImpellerRenderEventArgs e)
        {
            DrawScreenshotScene(e);

            if (_pendingSavePath is not { } path) return;
            _pendingSavePath = null;
            Dispatcher.BeginInvoke(() => SaveViewToPng(path), DispatcherPriority.ContextIdle);
        }

        private void SaveViewToPng(string path)
        {
            try
            {
                var bitmap = CaptureScreenPixels(_view);
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                using var stream = File.Create(path);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);

                _statusText.Text = $"Saved {Path.GetFileName(path)}";
            }
            catch (Exception ex)
            {
                _statusText.Text = $"Save failed: {ex.Message}";
            }
        }

        private static BitmapSource CaptureScreenPixels(FrameworkElement visual)
        {
            if (visual.ActualWidth <= 0 || visual.ActualHeight <= 0)
                throw new InvalidOperationException("The render view has no visible size.");

            var source = PresentationSource.FromVisual(visual);
            if (source?.CompositionTarget == null)
                throw new InvalidOperationException("The render view is not connected to a presentation source.");

            var transform = source.CompositionTarget.TransformToDevice;
            var width = Math.Max(1, (int)Math.Round(visual.ActualWidth * transform.M11));
            var height = Math.Max(1, (int)Math.Round(visual.ActualHeight * transform.M22));
            var topLeft = visual.PointToScreen(new Point(0, 0));

            return ScreenCapture.Capture(
                (int)Math.Round(topLeft.X),
                (int)Math.Round(topLeft.Y),
                width,
                height);
        }

        private static void DrawScreenshotScene(ImpellerRenderEventArgs e)
        {
            var b = e.Builder;
            SceneHelpers.ClearBg(b, 0x0D, 0x12, 0x1B);

            var t = (float)e.TotalTime.TotalSeconds;
            using var paint = ImpellerPaint.New()!;

            for (var i = 0; i < 20; i++)
            {
                var phase = i / 20f;
                var x = e.PixelWidth * (0.5f + MathF.Cos(t * 0.35f + i * 0.64f) * 0.42f);
                var y = e.PixelHeight * (0.5f + MathF.Sin(t * 0.48f + i * 0.51f) * 0.36f);
                var size = 42 + (i % 6) * 18;
                var (r, g, blue) = SceneHelpers.HsvToRgb((phase + t * 0.04f) % 1f, 0.72f, 0.95f);

                paint.SetColor(new ImpellerColor { Alpha = 0.38f, Red = r, Green = g, Blue = blue });
                b.Save();
                b.Translate(x, y);
                b.Rotate(t * 16f + i * 19f);
                b.DrawRoundedRect(
                    new ImpellerRect(-size / 2, -size / 2, size, size),
                    SceneHelpers.UniformRadii(10),
                    paint);
                b.Restore();
            }

            paint.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
            paint.SetStrokeWidth(2f * e.DpiScaleX);
            paint.SetColor(new ImpellerColor { Alpha = 0.42f, Red = 1, Green = 1, Blue = 1 });
            for (var y = 80; y < e.PixelHeight; y += 72)
            {
                b.DrawLine(
                    new ImpellerPoint { X = 0, Y = y },
                    new ImpellerPoint { X = e.PixelWidth, Y = y + MathF.Sin(t + y * 0.02f) * 18f },
                    paint);
            }

            if (e.Typography == null) return;

            TextBasicsScene.DrawSimpleText(
                b,
                e.Typography,
                "Screenshot PNG",
                36 * e.DpiScaleX,
                0,
                e.PixelHeight / 2f - 34,
                e.PixelWidth,
                ImpellerColor.FromRgb(0xF5, 0xF8, 0xFC),
                ImpellerFontWeight.kImpellerFontWeight700,
                ImpellerTextAlignment.kImpellerTextAlignmentCenter);

            TextBasicsScene.DrawSimpleText(
                b,
                e.Typography,
                $"Frame {e.FrameNumber:N0}",
                16 * e.DpiScaleX,
                0,
                e.PixelHeight / 2f + 18,
                e.PixelWidth,
                ImpellerColor.FromRgb(0xB8, 0xC4, 0xD2),
                align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
        }
    }

    private static class ScreenCapture
    {
        private const int Srccopy = 0x00CC0020;

        [DllImport("user32", SetLastError = true)]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32", SetLastError = true)]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32", SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32", SetLastError = true)]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32", SetLastError = true)]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);

        [DllImport("gdi32", SetLastError = true)]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

        [DllImport("gdi32", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr ho);

        [DllImport("gdi32", SetLastError = true)]
        private static extern bool BitBlt(
            IntPtr hdc,
            int x,
            int y,
            int cx,
            int cy,
            IntPtr hdcSrc,
            int x1,
            int y1,
            int rop);

        public static BitmapSource Capture(int x, int y, int width, int height)
        {
            var screenDc = GetDC(IntPtr.Zero);
            if (screenDc == IntPtr.Zero)
                throw LastWin32("GetDC");

            IntPtr memoryDc = IntPtr.Zero;
            IntPtr bitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;

            try
            {
                memoryDc = CreateCompatibleDC(screenDc);
                if (memoryDc == IntPtr.Zero)
                    throw LastWin32("CreateCompatibleDC");

                bitmap = CreateCompatibleBitmap(screenDc, width, height);
                if (bitmap == IntPtr.Zero)
                    throw LastWin32("CreateCompatibleBitmap");

                oldBitmap = SelectObject(memoryDc, bitmap);
                if (oldBitmap == IntPtr.Zero)
                    throw LastWin32("SelectObject");

                if (!BitBlt(memoryDc, 0, 0, width, height, screenDc, x, y, Srccopy))
                    throw LastWin32("BitBlt");

                var source = Imaging.CreateBitmapSourceFromHBitmap(
                    bitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                source.Freeze();
                return source;
            }
            finally
            {
                if (oldBitmap != IntPtr.Zero && memoryDc != IntPtr.Zero)
                    SelectObject(memoryDc, oldBitmap);
                if (bitmap != IntPtr.Zero)
                    DeleteObject(bitmap);
                if (memoryDc != IntPtr.Zero)
                    DeleteDC(memoryDc);
                ReleaseDC(IntPtr.Zero, screenDc);
            }
        }

        private static Exception LastWin32(string api) =>
            new InvalidOperationException($"{api} failed.", new Win32Exception(Marshal.GetLastWin32Error()));
    }
}
