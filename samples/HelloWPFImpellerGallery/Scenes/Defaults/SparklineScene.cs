using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class SparklineScene : IGalleryScene
{
    public string Name => "Sparkline";
    public string? Description => "Six sparkline cards with filled area + trend line";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x1A, 0x1D, 0x22);
        float t = (float)e.TotalTime.TotalSeconds;

        const int cols = 3;
        const int rows = 2;
        const int padOuter = 30;
        const int gap = 16;
        int cellW = (e.PixelWidth - padOuter * 2 - gap * (cols - 1)) / cols;
        int cellH = (e.PixelHeight - padOuter * 2 - gap * (rows - 1)) / rows;

        var titles = new[] { "Latency", "Throughput", "Errors", "Memory", "CPU", "Disk I/O" };

        for (int i = 0; i < 6; i++)
        {
            int col = i % cols;
            int row = i / cols;
            int x = padOuter + col * (cellW + gap);
            int y = padOuter + row * (cellH + gap);

            // Card background
            using (var p = ImpellerPaint.New()!)
            {
                p.SetColor(ImpellerColor.FromRgb(0x2A, 0x2F, 0x38));
                b.DrawRoundedRect(new ImpellerRect(x, y, cellW, cellH), SceneHelpers.UniformRadii(10), p);
            }

            // Title
            if (e.Typography != null)
            {
                TextBasicsScene.DrawSimpleText(b, e.Typography, titles[i], 14 * e.DpiScaleX,
                    x + 16, y + 14, cellW - 32,
                    ImpellerColor.FromRgb(0xA0, 0xA8, 0xB2),
                    weight: ImpellerFontWeight.kImpellerFontWeight500);
            }

            // Big number
            float value = 35 + 60 * (0.5f + 0.5f * MathF.Sin(t * 0.4f + i * 0.7f));
            if (e.Typography != null)
            {
                TextBasicsScene.DrawSimpleText(b, e.Typography, $"{value:0.0}", 28 * e.DpiScaleX,
                    x + 16, y + 38 * e.DpiScaleY, cellW - 32,
                    ImpellerColor.FromRgb(0xFF, 0xFF, 0xFF),
                    weight: ImpellerFontWeight.kImpellerFontWeight700);
            }

            // Sparkline (samples drawn as path)
            const int samples = 40;
            float chartTop = y + cellH * 0.55f;
            float chartH = cellH * 0.40f;
            float chartW = cellW - 32;

            var (cr, cg, cbb) = SceneHelpers.HsvToRgb(i / 6f, 0.7f, 1.0f);

            // Filled area
            using (var pb = ImpellerPathBuilder.New()!)
            {
                pb.MoveTo(new ImpellerPoint { X = x + 16, Y = chartTop + chartH });
                for (int j = 0; j <= samples; j++)
                {
                    float px = x + 16 + j * chartW / samples;
                    float v = 0.5f + 0.5f * MathF.Sin(j * 0.4f + t * 1.5f + i * 0.6f);
                    float py = chartTop + chartH - v * chartH;
                    pb.LineTo(new ImpellerPoint { X = px, Y = py });
                }
                pb.LineTo(new ImpellerPoint { X = x + 16 + chartW, Y = chartTop + chartH });
                pb.Close();
                using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
                using var p = ImpellerPaint.New()!;
                p.SetColor(new ImpellerColor { Alpha = 0.25f, Red = cr, Green = cg, Blue = cbb });
                b.DrawPath(path, p);
            }
            // Stroke line
            using (var pb = ImpellerPathBuilder.New()!)
            {
                for (int j = 0; j <= samples; j++)
                {
                    float px = x + 16 + j * chartW / samples;
                    float v = 0.5f + 0.5f * MathF.Sin(j * 0.4f + t * 1.5f + i * 0.6f);
                    float py = chartTop + chartH - v * chartH;
                    var pt = new ImpellerPoint { X = px, Y = py };
                    if (j == 0) pb.MoveTo(pt); else pb.LineTo(pt);
                }
                using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
                using var p = ImpellerPaint.New()!;
                p.SetColor(new ImpellerColor { Alpha = 1f, Red = cr, Green = cg, Blue = cbb });
                p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
                p.SetStrokeWidth(2f * e.DpiScaleX);
                p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapRound);
                b.DrawPath(path, p);
            }
        }
    }
}
