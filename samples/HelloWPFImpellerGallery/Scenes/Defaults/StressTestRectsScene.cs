using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class StressTestRectsScene : StressTestSceneBase
{
    public override string Name => "[StressTest] Rects";
    public override string? Description => "Draw N small filled rectangles at random positions per frame";
    public override string ItemLabel => "rects";

    public StressTestRectsScene() : base(initial: 10000, step: 1000, min: 0, max: 200000) { }

    public override void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x0E, 0x10, 0x14);

        var rng = StressHelpers.Seeded(1);
        float t = (float)e.TotalTime.TotalSeconds;
        int count = ItemCount;

        using var p = ImpellerPaint.New()!;
        for (int i = 0; i < count; i++)
        {
            int x = rng.Next(e.PixelWidth);
            int y = rng.Next(e.PixelHeight);
            int sz = 4 + rng.Next(8);

            // Slowly drift hue to make it visually obvious frames advance
            float hue = (i * 0.001f + t * 0.05f) % 1f;
            var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.7f, 1.0f);
            p.SetColor(new ImpellerColor { Alpha = 0.8f, Red = rr, Green = gg, Blue = bb });
            b.DrawRect(new ImpellerRect(x, y, sz, sz), p);
        }

        StressHelpers.DrawCountOverlay(e, "Rects", count);
    }
}
