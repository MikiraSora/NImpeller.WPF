using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class HexGridScene : IGalleryScene
{
    public string Name => "Hex Grid";
    public string? Description => "Honeycomb hexagonal tiling with animated hue sweep";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x10, 0x12, 0x18);
        float t = (float)e.TotalTime.TotalSeconds;

        float r = 38f * e.DpiScaleX; // hex outer radius
        float dx = r * 1.732f;       // horizontal spacing (sqrt(3))
        float dy = r * 1.5f;         // vertical spacing
        int cols = (int)(e.PixelWidth / dx) + 2;
        int rows = (int)(e.PixelHeight / dy) + 2;

        using var p = ImpellerPaint.New()!;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                float cx = col * dx + (row % 2 == 0 ? 0 : dx / 2);
                float cy = row * dy;

                float dist = MathF.Sqrt((cx - e.PixelWidth / 2f) * (cx - e.PixelWidth / 2f)
                                       + (cy - e.PixelHeight / 2f) * (cy - e.PixelHeight / 2f));
                float hue = ((dist * 0.003f) + t * 0.15f) % 1f;
                var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.7f, 0.95f);
                float alpha = 0.7f + 0.3f * MathF.Sin(t * 1.2f + (col + row) * 0.3f);
                p.SetColor(new ImpellerColor { Alpha = alpha, Red = rr, Green = gg, Blue = bb });

                DrawHex(b, p, cx, cy, r * 0.92f);
            }
        }
    }

    private static void DrawHex(ImpellerDisplayListBuilder b, ImpellerPaint p, float cx, float cy, float r)
    {
        using var pb = ImpellerPathBuilder.New()!;
        for (int i = 0; i < 6; i++)
        {
            float ang = i * MathF.PI / 3 - MathF.PI / 2;
            var pt = new ImpellerPoint { X = cx + MathF.Cos(ang) * r, Y = cy + MathF.Sin(ang) * r };
            if (i == 0) pb.MoveTo(pt); else pb.LineTo(pt);
        }
        pb.Close();
        using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
        b.DrawPath(path, p);
    }
}
