using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class SpirographScene : IGalleryScene
{
    public string Name => "Spirograph";
    public string? Description => "Parametric hypotrochoid curve plotted via ImpellerPathBuilder";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x10, 0x10, 0x16);
        float t = (float)e.TotalTime.TotalSeconds;

        float cx = e.PixelWidth / 2f;
        float cy = e.PixelHeight / 2f;
        float R = MathF.Min(cx, cy) * 0.55f;          // outer radius
        float r = R * (0.42f + 0.10f * MathF.Sin(t * 0.3f));  // animated inner radius
        float d = R * 0.85f;                          // pen distance

        using var pb = ImpellerPathBuilder.New()!;
        const int steps = 800;
        const float revs = 12;
        for (int i = 0; i <= steps; i++)
        {
            float theta = i / (float)steps * MathF.PI * 2 * revs;
            float x = cx + (R - r) * MathF.Cos(theta) + d * MathF.Cos((R - r) / r * theta);
            float y = cy + (R - r) * MathF.Sin(theta) - d * MathF.Sin((R - r) / r * theta);
            var pt = new ImpellerPoint { X = x, Y = y };
            if (i == 0) pb.MoveTo(pt); else pb.LineTo(pt);
        }
        using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;

        using var p = ImpellerPaint.New()!;
        var (cr, cg, cbb) = SceneHelpers.HsvToRgb((t * 0.05f) % 1f, 0.8f, 1.0f);
        p.SetColor(new ImpellerColor { Alpha = 0.85f, Red = cr, Green = cg, Blue = cbb });
        p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        p.SetStrokeWidth(1.5f);
        b.DrawPath(path, p);
    }
}
