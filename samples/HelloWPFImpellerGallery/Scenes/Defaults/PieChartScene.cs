using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class PieChartScene : IGalleryScene
{
    public string Name => "Pie Chart";
    public string? Description => "Animated pie slices drawn via Paths (DrawArc not exposed)";

    private readonly (string label, float value)[] _data =
    {
        ("Server",   38),
        ("Client",   24),
        ("Storage",  18),
        ("Network",  12),
        ("Other",     8),
    };

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x1A, 0x1D, 0x22);
        float t = (float)e.TotalTime.TotalSeconds;

        float total = 0;
        foreach (var d in _data) total += d.value;

        float cx = e.PixelWidth * 0.36f;
        float cy = e.PixelHeight * 0.5f;
        float radius = MathF.Min(e.PixelWidth, e.PixelHeight) * 0.32f;

        float anim = MathF.Min(1f, t * 0.5f);
        anim = 1f - MathF.Pow(1f - anim, 3);

        float startAng = -MathF.PI / 2;
        for (int i = 0; i < _data.Length; i++)
        {
            float sweep = _data[i].value / total * MathF.PI * 2 * anim;
            DrawSlice(b, cx, cy, radius, startAng, sweep, i);
            startAng += _data[i].value / total * MathF.PI * 2;
        }

        // Legend on the right
        if (e.Typography != null)
        {
            float legendX = e.PixelWidth * 0.65f;
            float legendY = e.PixelHeight * 0.32f;
            using var sw = ImpellerPaint.New()!;
            for (int i = 0; i < _data.Length; i++)
            {
                var (r, g, bb) = SceneHelpers.HsvToRgb(i / (float)_data.Length, 0.7f, 1.0f);
                sw.SetColor(new ImpellerColor { Alpha = 1, Red = r, Green = g, Blue = bb });
                b.DrawRoundedRect(new ImpellerRect((int)legendX, (int)(legendY + i * 40 * e.DpiScaleY), (int)(20 * e.DpiScaleX), (int)(20 * e.DpiScaleY)),
                    SceneHelpers.UniformRadii(4 * e.DpiScaleX), sw);
                TextBasicsScene.DrawSimpleText(b, e.Typography,
                    $"{_data[i].label}  {_data[i].value:0}%",
                    15 * e.DpiScaleX, legendX + 32 * e.DpiScaleX, legendY + i * 40 * e.DpiScaleY + 1 * e.DpiScaleY, e.PixelWidth,
                    ImpellerColor.FromRgb(0xE8, 0xE8, 0xE8));
            }
        }
    }

    private static void DrawSlice(ImpellerDisplayListBuilder b, float cx, float cy, float r, float a0, float sweep, int colorIndex)
    {
        if (sweep <= 0) return;
        using var pb = ImpellerPathBuilder.New()!;
        pb.MoveTo(new ImpellerPoint { X = cx, Y = cy });
        // Approximate the arc with line segments
        const int segs = 64;
        for (int i = 0; i <= segs; i++)
        {
            float aa = a0 + sweep * i / segs;
            pb.LineTo(new ImpellerPoint { X = cx + MathF.Cos(aa) * r, Y = cy + MathF.Sin(aa) * r });
        }
        pb.Close();
        using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
        using var p = ImpellerPaint.New()!;
        var (cr, cg, cbb) = SceneHelpers.HsvToRgb(colorIndex / 5f, 0.7f, 1.0f);
        p.SetColor(new ImpellerColor { Alpha = 1, Red = cr, Green = cg, Blue = cbb });
        b.DrawPath(path, p);
    }
}
