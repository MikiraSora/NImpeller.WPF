using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class TransformsScene : IGalleryScene
{
    public string Name => "Transforms";
    public string? Description => "Save / Restore, Translate, Rotate, Scale (animated)";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        ClearBg(b);
        float t = (float)e.TotalTime.TotalSeconds;

        using var p = ImpellerPaint.New()!;

        // Row 1: rotation around different anchors
        for (int i = 0; i < 6; i++)
        {
            b.Save();
            b.Translate(120 + i * 110, 120);
            b.Rotate((t * 60 + i * 15) % 360);

            float hue = i / 6f;
            var (r, g, bb) = HsvToRgb(hue, 0.8f, 1.0f);
            p.SetColor(new ImpellerColor { Alpha = 1, Red = r, Green = g, Blue = bb });
            b.DrawRect(new ImpellerRect(-40, -40, 80, 80), p);
            b.Restore();
        }

        // Row 2: scale animation
        for (int i = 0; i < 6; i++)
        {
            b.Save();
            b.Translate(120 + i * 110, 320);
            float scale = 0.5f + 0.5f * MathF.Sin(t * 1.2f + i * 0.6f);
            b.Scale(scale, scale);

            float hue = (i / 6f + 0.3f) % 1f;
            var (r, g, bb) = HsvToRgb(hue, 0.7f, 1.0f);
            p.SetColor(new ImpellerColor { Alpha = 1, Red = r, Green = g, Blue = bb });

            var corner = new ImpellerPoint { X = 14, Y = 14 };
            var radii = new ImpellerRoundingRadii { Top_left = corner, Top_right = corner, Bottom_left = corner, Bottom_right = corner };
            b.DrawRoundedRect(new ImpellerRect(-50, -50, 100, 100), radii, p);
            b.Restore();
        }

        // Row 3: combined rotation + scale + translate orbit
        for (int i = 0; i < 12; i++)
        {
            b.Save();
            float ang = t * 0.8f + i * (MathF.PI * 2 / 12);
            float ox = 380 + MathF.Cos(ang) * 240;
            float oy = 560 + MathF.Sin(ang) * 90;
            b.Translate(ox, oy);
            b.Rotate((t * 120 + i * 30) % 360);

            float hue = (i / 12f) % 1f;
            var (r, g, bb) = HsvToRgb(hue, 1.0f, 1.0f);
            p.SetColor(new ImpellerColor { Alpha = 1, Red = r, Green = g, Blue = bb });
            b.DrawOval(new ImpellerRect(-20, -10, 40, 20), p);
            b.Restore();
        }
    }

    private static void ClearBg(ImpellerDisplayListBuilder b)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0x14, 0x18, 0x1D));
        b.DrawPaint(p);
    }

    private static (float r, float g, float b) HsvToRgb(float h, float s, float v)
    {
        float i = MathF.Floor(h * 6);
        float f = h * 6 - i;
        float p = v * (1 - s);
        float q = v * (1 - f * s);
        float t = v * (1 - (1 - f) * s);
        return (((int)i) % 6) switch
        {
            0 => (v, t, p),
            1 => (q, v, p),
            2 => (p, v, t),
            3 => (p, q, v),
            4 => (t, p, v),
            _ => (v, p, q),
        };
    }
}
