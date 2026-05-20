using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class AnimationShowcaseScene : IGalleryScene
{
    public string Name => "Animation Showcase";
    public string? Description => "Combined animated scene (background + orbiting rectangles + central pulse + text)";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        float t = (float)e.TotalTime.TotalSeconds;
        int w = e.PixelWidth, h = e.PixelHeight;

        // Animated background
        using (var bg = ImpellerPaint.New()!)
        {
            bg.SetColor(new ImpellerColor
            {
                Alpha = 1,
                Red = 0.08f + 0.05f * MathF.Sin(t * 0.31f),
                Green = 0.10f + 0.05f * MathF.Sin(t * 0.37f + 1.3f),
                Blue = 0.18f + 0.07f * MathF.Sin(t * 0.43f + 2.1f),
            });
            b.DrawPaint(bg);
        }

        // Central pulsing disc
        {
            float cx = w / 2f, cy = h / 2f;
            float baseR = MathF.Min(w, h) * 0.18f;
            float pulse = 1.0f + 0.10f * MathF.Sin(t * 1.8f);
            float r = baseR * pulse;
            float hue = (t * 0.10f) % 1.0f;
            var (cr, cg, cb) = HsvToRgb(hue, 0.55f, 0.95f);
            using var p = ImpellerPaint.New()!;
            p.SetColor(new ImpellerColor { Alpha = 0.85f, Red = cr, Green = cg, Blue = cb });
            b.DrawOval(new ImpellerRect((int)(cx - r), (int)(cy - r), (int)(r * 2), (int)(r * 2)), p);
        }

        // Orbit of 8 spinning rounded rectangles
        {
            const int count = 8;
            float cx = w / 2f, cy = h / 2f;
            float orbit = MathF.Min(w, h) * 0.32f;
            float boxHalf = 28f * e.DpiScaleX;
            float cornerR = 10f * e.DpiScaleX;
            using var p = ImpellerPaint.New()!;
            for (int i = 0; i < count; i++)
            {
                float oAng = t * 0.6f + i * (MathF.PI * 2 / count);
                float sAng = (t * 80 + i * 45) % 360;
                float x = cx + MathF.Cos(oAng) * orbit;
                float y = cy + MathF.Sin(oAng) * orbit;

                float hue = (i / (float)count + t * 0.05f) % 1f;
                var (cr, cg, cb) = HsvToRgb(hue, 0.85f, 1.0f);
                p.SetColor(new ImpellerColor { Alpha = 0.95f, Red = cr, Green = cg, Blue = cb });

                b.Save();
                b.Translate(x, y);
                b.Rotate(sAng);
                var corner = new ImpellerPoint { X = cornerR, Y = cornerR };
                var radii = new ImpellerRoundingRadii { Top_left = corner, Top_right = corner, Bottom_left = corner, Bottom_right = corner };
                b.DrawRoundedRect(new ImpellerRect(-(int)boxHalf, -(int)boxHalf, (int)(boxHalf * 2), (int)(boxHalf * 2)), radii, p);
                b.Restore();
            }
        }

        // Title text
        if (e.Typography != null)
        {
            TextBasicsScene.DrawSimpleText(b, e.Typography, "Impeller Gallery — Showcase", 28 * e.DpiScaleX,
                0, 24 * e.DpiScaleX, w, ImpellerColor.FromRgb(255, 255, 255),
                weight: ImpellerFontWeight.kImpellerFontWeight600,
                align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);

            TextBasicsScene.DrawSimpleText(b, e.Typography, $"frame {e.FrameNumber}", 14 * e.DpiScaleX,
                0, h - 26 * e.DpiScaleX, w, ImpellerColor.FromRgb(180, 180, 180),
                align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
        }
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
            0 => (v, t, p), 1 => (q, v, p), 2 => (p, v, t),
            3 => (p, q, v), 4 => (t, p, v), _ => (v, p, q),
        };
    }
}
