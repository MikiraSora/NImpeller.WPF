using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class BarChartScene : IGalleryScene
{
    public string Name => "Bar Chart";
    public string? Description => "Bars with rounded tops, baseline axis, value labels — animated";

    private readonly string[] _labels = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug" };
    private readonly float[] _targets = { 0.65f, 0.42f, 0.78f, 0.55f, 0.93f, 0.71f, 0.48f, 0.62f };

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x1A, 0x1D, 0x22);
        float t = (float)e.TotalTime.TotalSeconds;

        float marginL = 80, marginR = 40, marginT = 60, marginB = 80;
        float chartW = e.PixelWidth - marginL - marginR;
        float chartH = e.PixelHeight - marginT - marginB;
        float baseY = marginT + chartH;
        int n = _targets.Length;
        float gap = 14;
        float barW = (chartW - gap * (n - 1)) / n;

        // Axes
        using (var p = ImpellerPaint.New()!)
        {
            p.SetColor(ImpellerColor.FromRgb(0x44, 0x4A, 0x55));
            p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
            p.SetStrokeWidth(1f);
            for (int g = 0; g <= 4; g++)
            {
                float y = marginT + chartH * (1 - g / 4f);
                b.DrawLine(new ImpellerPoint { X = marginL, Y = y },
                           new ImpellerPoint { X = marginL + chartW, Y = y }, p);
            }
        }

        // Bars
        for (int i = 0; i < n; i++)
        {
            float anim = MathF.Min(1f, MathF.Max(0f, t * 0.6f - i * 0.05f));
            anim = 1f - MathF.Pow(1f - anim, 3); // ease out cubic
            float val = _targets[i] * anim;
            float h = chartH * val;
            float x = marginL + i * (barW + gap);
            float y = baseY - h;

            var (r, g, bb) = SceneHelpers.HsvToRgb(i / (float)n * 0.7f + 0.55f, 0.7f, 1.0f);
            using var p = ImpellerPaint.New()!;
            p.SetColor(new ImpellerColor { Alpha = 1, Red = r, Green = g, Blue = bb });

            var radii = SceneHelpers.UniformRadii(8);
            b.DrawRoundedRect(new ImpellerRect((int)x, (int)y, (int)barW, (int)h), radii, p);

            // Value label
            if (e.Typography != null)
            {
                TextBasicsScene.DrawSimpleText(b, e.Typography, $"{(int)(_targets[i] * 100)}", 13 * e.DpiScaleX,
                    x, y - 22 * e.DpiScaleY, (int)barW,
                    ImpellerColor.FromRgb(0xE8, 0xE8, 0xE8),
                    align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
                TextBasicsScene.DrawSimpleText(b, e.Typography, _labels[i], 13 * e.DpiScaleX,
                    x, baseY + 12 * e.DpiScaleY, (int)barW,
                    ImpellerColor.FromRgb(0x9A, 0xA0, 0xAC),
                    align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
            }
        }

        if (e.Typography != null)
            TextBasicsScene.DrawSimpleText(b, e.Typography, "Monthly Activity", 22 * e.DpiScaleX,
                0, 20 * e.DpiScaleX, e.PixelWidth,
                ImpellerColor.FromRgb(0xFF, 0xFF, 0xFF),
                weight: ImpellerFontWeight.kImpellerFontWeight600,
                align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
    }
}
