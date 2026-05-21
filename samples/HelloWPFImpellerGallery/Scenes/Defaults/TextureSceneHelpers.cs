using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;

internal sealed class TextureSceneImage : IDisposable
{
    public TextureSceneImage(ImpellerTexture texture, int width, int height)
    {
        Texture = texture;
        Width = width;
        Height = height;
    }

    public ImpellerTexture Texture { get; }
    public int Width { get; }
    public int Height { get; }

    public void Dispose()
    {
        Texture.Dispose();
    }
}

internal static class TextureSceneHelpers
{
    private const string TexturePath = "Resources/tex.png";

    public static TextureSceneImage? LoadTexture(ImpellerContext context)
    {
        using var stream = OpenTextureStream();
        if (stream == null)
            return null;

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();

        var bgra = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
        bgra.Freeze();

        var stride = bgra.PixelWidth * 4;
        var pixels = new byte[stride * bgra.PixelHeight];
        bgra.CopyPixels(pixels, stride, 0);
        for (var i = 0; i < pixels.Length; i += 4)
        {
            (pixels[i], pixels[i + 2]) = (pixels[i + 2], pixels[i]);
        }

        var descriptor = new ImpellerTextureDescriptor
        {
            Pixel_format = ImpellerPixelFormat.kImpellerPixelFormatRGBA8888,
            Size = new ImpellerISize(bgra.PixelWidth, bgra.PixelHeight),
            Mip_count = 1,
        };

        var texture = context.TextureCreateWithContentsNew(descriptor, pixels);
        return texture == null
            ? null
            : new TextureSceneImage(texture, bgra.PixelWidth, bgra.PixelHeight);
    }

    private static Stream? OpenTextureStream()
    {
        foreach (var path in EnumerateTexturePaths())
        {
            if (File.Exists(path))
                return File.OpenRead(path);
        }

        try
        {
            return Application.GetResourceStream(new Uri(TexturePath, UriKind.Relative))?.Stream;
        }
        catch (IOException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static IEnumerable<string> EnumerateTexturePaths()
    {
        yield return Path.Combine(AppContext.BaseDirectory, TexturePath);
        yield return Path.Combine(Environment.CurrentDirectory, TexturePath);

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 8 && directory != null; i++, directory = directory.Parent)
        {
            yield return Path.Combine(directory.FullName, TexturePath);
            yield return Path.Combine(directory.FullName, "samples", "HelloWPFImpellerGallery", TexturePath);
        }
    }

    public static void Clear(ImpellerDisplayListBuilder b)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0x12, 0x16, 0x1B));
        b.DrawPaint(p);
    }

    public static void DrawTitle(ImpellerRenderEventArgs e, string title, string subtitle)
    {
        if (e.Typography == null) return;

        TextBasicsScene.DrawSimpleText(
            e.Builder,
            e.Typography,
            title,
            24 * e.DpiScaleX,
            28 * e.DpiScaleX,
            22 * e.DpiScaleY,
            e.PixelWidth,
            ImpellerColor.FromRgb(0xF5, 0xF7, 0xFA),
            ImpellerFontWeight.kImpellerFontWeight700);

        TextBasicsScene.DrawSimpleText(
            e.Builder,
            e.Typography,
            subtitle,
            14 * e.DpiScaleX,
            30 * e.DpiScaleX,
            56 * e.DpiScaleY,
            e.PixelWidth,
            ImpellerColor.FromRgb(0xA8, 0xB3, 0xC2));
    }

    public static void DrawMissingTexture(ImpellerRenderEventArgs e)
    {
        using var paint = ImpellerPaint.New()!;
        paint.SetColor(ImpellerColor.FromRgb(0x85, 0x32, 0x32));
        e.Builder.DrawRect(new ImpellerRect(40, 100, 520, 96), paint);

        if (e.Typography == null) return;
        TextBasicsScene.DrawSimpleText(
            e.Builder,
            e.Typography,
            $"Missing {TexturePath}",
            18 * e.DpiScaleX,
            58,
            130,
            e.PixelWidth,
            ImpellerColor.FromRgb(0xFF, 0xEE, 0xEE),
            ImpellerFontWeight.kImpellerFontWeight600);
    }

    public static ImpellerRect CenteredRect(int canvasWidth, int canvasHeight, float width, float height, float yOffset = 24)
    {
        var x = (canvasWidth - width) * 0.5f;
        var y = (canvasHeight - height) * 0.5f + yOffset;
        return new ImpellerRect((int)x, (int)y, (int)width, (int)height);
    }

    public static void DrawFrame(ImpellerDisplayListBuilder b, ImpellerRect rect, ImpellerColor color)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(color);
        p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        p.SetStrokeWidth(3);
        b.DrawRect(rect, p);
    }
}
