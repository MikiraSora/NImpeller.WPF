using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class StressTestPathsScene : StressTestSceneBase
{
    public override string Name => "[StressTest] Cubic Paths";
    public override string? Description => "Build + draw N cubic-curve paths per frame (path tessellation pressure)";
    public override string ItemLabel => "paths";

    public StressTestPathsScene() : base(initial: 1000, step: 200, min: 0, max: 40000) { }

    public override void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x0E, 0x10, 0x14);

        var rng = StressHelpers.Seeded(5);
        float t = (float)e.TotalTime.TotalSeconds;
        int count = ItemCount;

        using var p = ImpellerPaint.New()!;
        p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        p.SetStrokeWidth(1.5f);

        for (int i = 0; i < count; i++)
        {
            float cx = rng.Next(e.PixelWidth);
            float cy = rng.Next(e.PixelHeight);
            float r = 6 + rng.Next(20);

            using var pb = ImpellerPathBuilder.New()!;
            pb.MoveTo(new ImpellerPoint { X = cx - r, Y = cy });
            pb.CubicCurveTo(
                new ImpellerPoint { X = cx - r, Y = cy - r },
                new ImpellerPoint { X = cx + r, Y = cy - r },
                new ImpellerPoint { X = cx + r, Y = cy });
            pb.CubicCurveTo(
                new ImpellerPoint { X = cx + r, Y = cy + r },
                new ImpellerPoint { X = cx - r, Y = cy + r },
                new ImpellerPoint { X = cx - r, Y = cy });
            pb.Close();

            using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
            float hue = (i * 0.003f + t * 0.05f) % 1f;
            var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.7f, 1.0f);
            p.SetColor(new ImpellerColor { Alpha = 0.7f, Red = rr, Green = gg, Blue = bb });
            b.DrawPath(path, p);
        }

        StressHelpers.DrawCountOverlay(e, "Cubic Paths", count);
    }
}
