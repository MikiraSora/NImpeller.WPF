using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class AnalogClockScene : IGalleryScene
{
    public string Name => "Analog Clock";
    public string? Description => "Live wall clock — minute marks, tick marks, smooth-sweep seconds hand";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x18, 0x1C, 0x24);

        float cx = e.PixelWidth / 2f;
        float cy = e.PixelHeight / 2f;
        float r = MathF.Min(cx, cy) * 0.80f;

        // Face background
        using (var p = ImpellerPaint.New()!)
        {
            p.SetColor(ImpellerColor.FromRgb(0xF2, 0xEE, 0xE3));
            b.DrawOval(new ImpellerRect((int)(cx - r), (int)(cy - r), (int)(r * 2), (int)(r * 2)), p);

            // Bezel
            p.SetColor(ImpellerColor.FromRgb(0x32, 0x26, 0x1B));
            p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
            p.SetStrokeWidth(8f * e.DpiScaleX);
            b.DrawOval(new ImpellerRect((int)(cx - r), (int)(cy - r), (int)(r * 2), (int)(r * 2)), p);
        }

        // Tick marks
        using (var p = ImpellerPaint.New()!)
        {
            p.SetColor(ImpellerColor.FromRgb(0x32, 0x26, 0x1B));
            p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
            p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapRound);
            for (int i = 0; i < 60; i++)
            {
                bool isHour = i % 5 == 0;
                p.SetStrokeWidth((isHour ? 5f : 2f) * e.DpiScaleX);
                float ang = i * MathF.PI / 30 - MathF.PI / 2;
                float outerR = r - 8 * e.DpiScaleX;
                float innerR = outerR - (isHour ? 22 * e.DpiScaleX : 10 * e.DpiScaleX);
                b.DrawLine(
                    new ImpellerPoint { X = cx + MathF.Cos(ang) * outerR, Y = cy + MathF.Sin(ang) * outerR },
                    new ImpellerPoint { X = cx + MathF.Cos(ang) * innerR, Y = cy + MathF.Sin(ang) * innerR },
                    p);
            }
        }

        // Get current time as fractional values (for smooth sweep)
        var now = DateTime.Now;
        float secF = now.Second + now.Millisecond / 1000f;
        float minF = now.Minute + secF / 60f;
        float hrF = (now.Hour % 12) + minF / 60f;

        // Hour hand
        DrawHand(b, cx, cy, hrF / 12f, r * 0.50f, 9f * e.DpiScaleX, ImpellerColor.FromRgb(0x32, 0x26, 0x1B));
        // Minute hand
        DrawHand(b, cx, cy, minF / 60f, r * 0.72f, 6f * e.DpiScaleX, ImpellerColor.FromRgb(0x32, 0x26, 0x1B));
        // Second hand (red, thin)
        DrawHand(b, cx, cy, secF / 60f, r * 0.80f, 2f * e.DpiScaleX, ImpellerColor.FromRgb(0xD0, 0x40, 0x40));

        // Center cap
        using (var p = ImpellerPaint.New()!)
        {
            p.SetColor(ImpellerColor.FromRgb(0x32, 0x26, 0x1B));
            b.DrawOval(new ImpellerRect((int)(cx - 8 * e.DpiScaleX), (int)(cy - 8 * e.DpiScaleY), (int)(16 * e.DpiScaleX), (int)(16 * e.DpiScaleY)), p);
            p.SetColor(ImpellerColor.FromRgb(0xD0, 0x40, 0x40));
            b.DrawOval(new ImpellerRect((int)(cx - 4 * e.DpiScaleX), (int)(cy - 4 * e.DpiScaleY), (int)(8 * e.DpiScaleX), (int)(8 * e.DpiScaleY)), p);
        }
    }

    private static void DrawHand(ImpellerDisplayListBuilder b, float cx, float cy, float fraction, float length, float width, ImpellerColor color)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(color);
        p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapRound);
        p.SetStrokeWidth(width);
        float ang = fraction * MathF.PI * 2 - MathF.PI / 2;
        b.DrawLine(
            new ImpellerPoint { X = cx, Y = cy },
            new ImpellerPoint { X = cx + MathF.Cos(ang) * length, Y = cy + MathF.Sin(ang) * length },
            p);
    }
}
