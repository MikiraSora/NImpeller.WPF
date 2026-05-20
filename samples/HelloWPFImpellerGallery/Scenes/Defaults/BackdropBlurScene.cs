using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class BackdropBlurScene : IGalleryScene
{
    public string Name => "Backdrop Blur (Frosted Glass)";
    public string? Description => "SaveLayer with backdrop blur ImageFilter — iOS-style frosted glass effect";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b);
        float t = (float)e.TotalTime.TotalSeconds;

        // Animated colorful background
        using (var p = ImpellerPaint.New()!)
        {
            for (int i = 0; i < 12; i++)
            {
                float ang = t * 0.4f + i * (MathF.PI * 2 / 12);
                float cx = e.PixelWidth / 2f + MathF.Cos(ang) * 240;
                float cy = e.PixelHeight / 2f + MathF.Sin(ang) * 180;
                var (r, g, bb) = SceneHelpers.HsvToRgb(i / 12f, 0.85f, 1.0f);
                p.SetColor(new ImpellerColor { Alpha = 0.9f, Red = r, Green = g, Blue = bb });
                b.DrawOval(new ImpellerRect((int)cx - 80, (int)cy - 80, 160, 160), p);
            }
        }

        // Frosted glass strip across the middle
        int yBand = (int)(e.PixelHeight / 2 - 80);
        int hBand = 160;

        b.Save();
        b.ClipRoundedRect(new ImpellerRect(40, yBand, e.PixelWidth - 80, hBand),
            SceneHelpers.UniformRadii(28), ImpellerClipOperation.kImpellerClipOperationIntersect);
        using (var glassPaint = ImpellerPaint.New()!)
        {
            glassPaint.SetColor(new ImpellerColor { Alpha = 0.25f, Red = 1, Green = 1, Blue = 1 });
            using var blur = ImpellerImageFilter.CreateBlurNew(18f, 18f, ImpellerTileMode.kImpellerTileModeClamp)!;
            b.SaveLayer(new ImpellerRect(40, yBand, e.PixelWidth - 80, hBand), glassPaint, blur);
            // Tint
            using var tint = ImpellerPaint.New()!;
            tint.SetColor(new ImpellerColor { Alpha = 0.35f, Red = 1, Green = 1, Blue = 1 });
            b.DrawRect(new ImpellerRect(40, yBand, e.PixelWidth - 80, hBand), tint);
            b.Restore();
        }
        b.Restore();

        if (e.Typography != null)
        {
            TextBasicsScene.DrawSimpleText(b, e.Typography, "Frosted Glass via Backdrop Blur",
                24 * e.DpiScaleX, 0, yBand + 60, e.PixelWidth,
                ImpellerColor.FromRgb(0x18, 0x18, 0x18),
                weight: ImpellerFontWeight.kImpellerFontWeight600,
                align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
        }
    }
}
