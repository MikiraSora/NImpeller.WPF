using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class WaveLinesScene : IGalleryScene
{
    public string Name => "Wave Lines";
    public string? Description => "Stacked sine waves with phase + amplitude offsets";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x0C, 0x10, 0x18);
        float t = (float)e.TotalTime.TotalSeconds;

        const int waves = 8;
        const int samples = 200;
        using var p = ImpellerPaint.New()!;
        p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        p.SetStrokeWidth(2.5f);
        p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapRound);

        for (int w = 0; w < waves; w++)
        {
            float baseY = (w + 1) * (float)e.PixelHeight / (waves + 1);
            float amp = 30 + 8 * w;
            float freq = 0.012f + w * 0.001f;
            float phase = t * (1.2f + w * 0.18f);

            var (cr, cg, cb) = SceneHelpers.HsvToRgb((w / (float)waves + t * 0.05f) % 1f, 0.8f, 1.0f);
            p.SetColor(new ImpellerColor { Alpha = 0.85f, Red = cr, Green = cg, Blue = cb });

            using var pb = ImpellerPathBuilder.New()!;
            for (int i = 0; i <= samples; i++)
            {
                float x = i * (float)e.PixelWidth / samples;
                float y = baseY + MathF.Sin(x * freq + phase) * amp;
                var pt = new ImpellerPoint { X = x, Y = y };
                if (i == 0) pb.MoveTo(pt); else pb.LineTo(pt);
            }
            using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
            b.DrawPath(path, p);
        }
    }
}
