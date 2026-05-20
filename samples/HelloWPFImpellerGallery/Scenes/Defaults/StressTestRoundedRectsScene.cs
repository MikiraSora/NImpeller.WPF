using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class StressTestRoundedRectsScene : StressTestSceneBase
{
    public override string Name => "[StressTest] Rounded Rects";
    public override string? Description => "Draw N rounded rectangles — heavier than plain rects";
    public override string ItemLabel => "rounded rects";

    public StressTestRoundedRectsScene() : base(initial: 5000, step: 500, min: 0, max: 100000) { }

    public override void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x0E, 0x10, 0x14);

        var rng = StressHelpers.Seeded(3);
        float t = (float)e.TotalTime.TotalSeconds;
        var radii = SceneHelpers.UniformRadii(4);
        int count = ItemCount;

        using var p = ImpellerPaint.New()!;
        for (int i = 0; i < count; i++)
        {
            int x = rng.Next(e.PixelWidth);
            int y = rng.Next(e.PixelHeight);
            int w = 8 + rng.Next(20);
            int h = 8 + rng.Next(20);

            float hue = (i * 0.001f + t * 0.05f) % 1f;
            var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.75f, 1.0f);
            p.SetColor(new ImpellerColor { Alpha = 0.85f, Red = rr, Green = gg, Blue = bb });
            b.DrawRoundedRect(new ImpellerRect(x, y, w, h), radii, p);
        }

        StressHelpers.DrawCountOverlay(e, "Rounded Rects", count);
    }
}
