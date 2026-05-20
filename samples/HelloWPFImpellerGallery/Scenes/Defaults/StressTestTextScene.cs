using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class StressTestTextScene : StressTestSceneBase
{
    public override string Name => "[StressTest] Text Paragraphs";
    public override string? Description => "Lay out + draw N small text paragraphs per frame";
    public override string ItemLabel => "paragraphs";

    public StressTestTextScene() : base(initial: 500, step: 100, min: 0, max: 10000) { }

    public override void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x0E, 0x10, 0x14);
        if (e.Typography == null) return;

        var rng = StressHelpers.Seeded(6);
        float t = (float)e.TotalTime.TotalSeconds;
        int count = ItemCount;

        var sample = new[]
        {
            "Hello", "Impeller", "Vulkan", "WPF",
            "GPU", "Render", "Pixel", "Frame",
            "Sigma", "Layer", "Path", "Glyph",
        };

        for (int i = 0; i < count; i++)
        {
            int x = rng.Next(e.PixelWidth - 100);
            int y = rng.Next(e.PixelHeight - 30);
            float hue = (i * 0.005f + t * 0.05f) % 1f;
            var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.6f, 1.0f);
            TextBasicsScene.DrawSimpleText(b, e.Typography, sample[i % sample.Length],
                14, x, y, 120, new ImpellerColor { Alpha = 1, Red = rr, Green = gg, Blue = bb });
        }

        StressHelpers.DrawCountOverlay(e, "Paragraphs", count);
    }
}
