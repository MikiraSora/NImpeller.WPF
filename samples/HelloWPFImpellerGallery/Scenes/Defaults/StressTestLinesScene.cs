using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class StressTestLinesScene : StressTestSceneBase
{
    public override string Name => "[StressTest] Lines";
    public override string? Description => "Draw N stroked lines (round cap)";
    public override string ItemLabel => "lines";

    public StressTestLinesScene() : base(initial: 5000, step: 500, min: 0, max: 100000) { }

    public override void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x0E, 0x10, 0x14);

        var rng = StressHelpers.Seeded(4);
        float t = (float)e.TotalTime.TotalSeconds;
        int count = ItemCount;

        using var p = ImpellerPaint.New()!;
        p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapRound);
        p.SetStrokeWidth(2f);

        for (int i = 0; i < count; i++)
        {
            float x0 = rng.Next(e.PixelWidth);
            float y0 = rng.Next(e.PixelHeight);
            float x1 = x0 + (float)(rng.NextDouble() - 0.5) * 80;
            float y1 = y0 + (float)(rng.NextDouble() - 0.5) * 80;

            float hue = (i * 0.0008f + t * 0.05f) % 1f;
            var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.7f, 1.0f);
            p.SetColor(new ImpellerColor { Alpha = 0.7f, Red = rr, Green = gg, Blue = bb });
            b.DrawLine(new ImpellerPoint { X = x0, Y = y0 }, new ImpellerPoint { X = x1, Y = y1 }, p);
        }

        StressHelpers.DrawCountOverlay(e, "Lines", count);
    }
}
