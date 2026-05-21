using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;

internal sealed class OffscreenCompositeScene : IGalleryScene
{
    public string Name => "Offscreen Composite";
    public string? Description => "Uses SaveLayer as a bounded offscreen pass, then composites the whole result back";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x0E, 0x11, 0x18);

        DrawBackground(b, e);

        var layerWidth = Math.Min(620, Math.Max(260, e.PixelWidth - 120));
        var layerHeight = Math.Min(360, Math.Max(220, e.PixelHeight - 180));
        var layer = new ImpellerRect(
            (e.PixelWidth - layerWidth) / 2,
            (e.PixelHeight - layerHeight) / 2 + 24,
            layerWidth,
            layerHeight);

        using (var shadow = ImpellerPaint.New()!)
        {
            shadow.SetColor(new ImpellerColor { Alpha = 0.18f, Red = 0, Green = 0, Blue = 0 });
            b.DrawRoundedRect(
                RectF(layer.X + 18, layer.Y + 18, layer.Width, layer.Height),
                SceneHelpers.UniformRadii(28),
                shadow);
        }

        b.Save();
        b.ClipRoundedRect(layer, SceneHelpers.UniformRadii(28), ImpellerClipOperation.kImpellerClipOperationIntersect);
        using (var layerPaint = ImpellerPaint.New()!)
        {
            layerPaint.SetColor(new ImpellerColor { Alpha = 0.86f, Red = 1, Green = 1, Blue = 1 });
            using var blur = ImpellerImageFilter.CreateBlurNew(4f, 4f, ImpellerTileMode.kImpellerTileModeClamp)!;
            b.SaveLayer(layer, layerPaint, blur);
        }

        DrawLayerContents(b, e, layer);
        b.Restore();
        b.Restore();

        TextureSceneHelpers.DrawFrame(b, layer, ImpellerColor.FromRgb(0xD8, 0xE8, 0xFF));
        DrawLabels(e, layer);
    }

    private static void DrawBackground(ImpellerDisplayListBuilder b, ImpellerRenderEventArgs e)
    {
        using var paint = ImpellerPaint.New()!;
        for (var i = 0; i < 18; i++)
        {
            var hue = i / 18f;
            var (r, g, blue) = SceneHelpers.HsvToRgb(hue, 0.58f, 0.92f);
            paint.SetColor(new ImpellerColor { Alpha = 0.36f, Red = r, Green = g, Blue = blue });
            var x = 28 + (i * 97) % Math.Max(1, e.PixelWidth - 80);
            var y = 78 + (i * 61) % Math.Max(1, e.PixelHeight - 120);
            b.DrawOval(new ImpellerRect(x - 70, y - 70, 140, 140), paint);
        }
    }

    private static void DrawLayerContents(ImpellerDisplayListBuilder b, ImpellerRenderEventArgs e, ImpellerRect layer)
    {
        var t = (float)e.TotalTime.TotalSeconds;

        using var fill = ImpellerPaint.New()!;
        fill.SetColor(new ImpellerColor { Alpha = 0.34f, Red = 0.95f, Green = 0.98f, Blue = 1.0f });
        b.DrawRect(layer, fill);

        for (var i = 0; i < 9; i++)
        {
            var phase = i / 9f;
            var cx = layer.X + layer.Width * (0.16f + phase * 0.72f);
            var cy = layer.Y + layer.Height * (0.5f + MathF.Sin(t * 1.2f + i * 0.75f) * 0.28f);
            var size = 46 + i * 8;
            var (r, g, blue) = SceneHelpers.HsvToRgb((phase + t * 0.05f) % 1f, 0.78f, 0.96f);

            using var paint = ImpellerPaint.New()!;
            paint.SetColor(new ImpellerColor { Alpha = 0.72f, Red = r, Green = g, Blue = blue });
            paint.SetBlendMode(i % 2 == 0
                ? ImpellerBlendMode.kImpellerBlendModeScreen
                : ImpellerBlendMode.kImpellerBlendModeMultiply);

            b.Save();
            b.Translate(cx, cy);
            b.Rotate(t * 18f + i * 17f);
            b.DrawRoundedRect(
                RectF(-size * 0.5f, -size * 0.5f, size, size),
                SceneHelpers.UniformRadii(12),
                paint);
            b.Restore();
        }

        using var line = ImpellerPaint.New()!;
        line.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        line.SetStrokeWidth(3);
        line.SetColor(new ImpellerColor { Alpha = 0.48f, Red = 0.1f, Green = 0.16f, Blue = 0.26f });
        for (var y = layer.Y + 36; y < layer.Y + layer.Height; y += 42)
        {
            b.DrawLine(
                new ImpellerPoint { X = layer.X + 28, Y = y },
                new ImpellerPoint { X = layer.X + layer.Width - 28, Y = y + MathF.Sin(t + y * 0.02f) * 14f },
                line);
        }
    }

    private static void DrawLabels(ImpellerRenderEventArgs e, ImpellerRect layer)
    {
        if (e.Typography == null) return;

        TextBasicsScene.DrawSimpleText(
            e.Builder,
            e.Typography,
            "Offscreen Composite",
            24 * e.DpiScaleX,
            28,
            24,
            e.PixelWidth,
            ImpellerColor.FromRgb(0xF4, 0xF7, 0xFB),
            ImpellerFontWeight.kImpellerFontWeight700);

        TextBasicsScene.DrawSimpleText(
            e.Builder,
            e.Typography,
            "The clipped group is rendered through SaveLayer, blurred/tinted as one bounded offscreen pass, then composited over the background.",
            14 * e.DpiScaleX,
            30,
            (int)MathF.Round(layer.Y + layer.Height + 24),
            e.PixelWidth - 60,
            ImpellerColor.FromRgb(0xC8, 0xD3, 0xE0));
    }

    private static ImpellerRect RectF(float x, float y, float width, float height) =>
        new(
            (int)MathF.Round(x),
            (int)MathF.Round(y),
            (int)MathF.Round(width),
            (int)MathF.Round(height));
}
