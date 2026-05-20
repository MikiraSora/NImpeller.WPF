using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class StressTestTransformsScene : StressTestSceneBase
{
    public override string Name => "[StressTest] Transforms";
    public override string? Description => "Save + Translate + Rotate + DrawRect + Restore, N times per frame";
    public override string ItemLabel => "transforms";

    public StressTestTransformsScene() : base(initial: 5000, step: 500, min: 0, max: 100000) { }

    public override void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x0E, 0x10, 0x14);

        var rng = StressHelpers.Seeded(7);
        float t = (float)e.TotalTime.TotalSeconds;
        int count = ItemCount;

        using var p = ImpellerPaint.New()!;
        for (int i = 0; i < count; i++)
        {
            float x = rng.Next(e.PixelWidth);
            float y = rng.Next(e.PixelHeight);
            float angle = (t * 60 + i * 7) % 360;

            float hue = (i * 0.001f + t * 0.05f) % 1f;
            var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.7f, 1.0f);
            p.SetColor(new ImpellerColor { Alpha = 0.7f, Red = rr, Green = gg, Blue = bb });

            b.Save();
            b.Translate(x, y);
            b.Rotate(angle);
            b.DrawRect(new ImpellerRect(-6, -6, 12, 12), p);
            b.Restore();
        }

        StressHelpers.DrawCountOverlay(e, "Transformed rects", count);
    }
}
