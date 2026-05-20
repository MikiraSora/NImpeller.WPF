using System;
using System.Numerics;

using NImpeller;

namespace HelloWPFImpeller.Scenes;

/// <summary>
/// Demo scene showing off Impeller's Vulkan backend via the WPF D3DImage path:
/// animated gradient background, central pulsing disc, ring of orbiting rounded
/// rectangles with their own spin, and Impeller-rendered text overlay.
/// </summary>
internal static class HelloDemoScene
{
    public static void Render(
        ImpellerDisplayListBuilder builder,
        ImpellerTypographyContext? typography,
        float timeSeconds,
        int width,
        int height,
        long frameNumber)
    {
        DrawBackground(builder, timeSeconds, width, height);
        DrawCentralPulse(builder, timeSeconds, width, height);
        DrawOrbitingRectangles(builder, timeSeconds, width, height);
        DrawTextOverlay(builder, typography, width, height, frameNumber);
    }

    private static void DrawBackground(ImpellerDisplayListBuilder builder, float t, int w, int h)
    {
        // Slow drifting dark color
        float r = 0.08f + 0.05f * MathF.Sin(t * 0.31f);
        float g = 0.10f + 0.05f * MathF.Sin(t * 0.37f + 1.3f);
        float b = 0.18f + 0.07f * MathF.Sin(t * 0.43f + 2.1f);

        using var paint = ImpellerPaint.New()!;
        paint.SetColor(new ImpellerColor { Alpha = 1, Red = r, Green = g, Blue = b });
        builder.DrawPaint(paint);
    }

    private static void DrawCentralPulse(ImpellerDisplayListBuilder builder, float t, int w, int h)
    {
        // Soft pulsing disc in the middle
        float cx = w / 2f;
        float cy = h / 2f;
        float baseRadius = MathF.Min(w, h) * 0.18f;
        float pulse = 1.0f + 0.10f * MathF.Sin(t * 1.8f);
        float radius = baseRadius * pulse;

        // Hue rotates over time
        float hue = (t * 0.10f) % 1.0f;
        var color = HsvToRgb(hue, 0.55f, 0.95f);

        using var paint = ImpellerPaint.New()!;
        paint.SetColor(new ImpellerColor { Alpha = 0.85f, Red = color.r, Green = color.g, Blue = color.b });
        var bounds = new ImpellerRect(
            x: (int)(cx - radius), y: (int)(cy - radius),
            width: (int)(radius * 2), height: (int)(radius * 2));
        builder.DrawOval(bounds, paint);
    }

    private static void DrawOrbitingRectangles(ImpellerDisplayListBuilder builder, float t, int w, int h)
    {
        const int count = 8;
        float cx = w / 2f;
        float cy = h / 2f;
        float orbit = MathF.Min(w, h) * 0.32f;
        float boxHalf = 28f;

        using var paint = ImpellerPaint.New()!;

        for (int i = 0; i < count; i++)
        {
            float orbitAngle = t * 0.6f + i * (MathF.PI * 2f / count);
            float spinAngleDeg = (t * 80f + i * 45f) % 360f;

            float x = cx + MathF.Cos(orbitAngle) * orbit;
            float y = cy + MathF.Sin(orbitAngle) * orbit;

            float hue = (i / (float)count + t * 0.05f) % 1.0f;
            var color = HsvToRgb(hue, 0.85f, 1.0f);
            paint.SetColor(new ImpellerColor { Alpha = 0.95f, Red = color.r, Green = color.g, Blue = color.b });

            // Push transform, draw centered-on-origin rect, pop transform.
            builder.Save();
            builder.Translate(x, y);
            builder.Rotate(spinAngleDeg);

            var rect = new ImpellerRect(-(int)boxHalf, -(int)boxHalf, (int)(boxHalf * 2), (int)(boxHalf * 2));
            var corner = new ImpellerPoint { X = 10, Y = 10 };
            var radii = new ImpellerRoundingRadii
            {
                Top_left = corner,
                Top_right = corner,
                Bottom_left = corner,
                Bottom_right = corner,
            };
            builder.DrawRoundedRect(rect, radii, paint);
            builder.Restore();
        }
    }

    private static void DrawTextOverlay(
        ImpellerDisplayListBuilder builder,
        ImpellerTypographyContext? typography,
        int width, int height, long frameNumber)
    {
        if (typography == null) return;

        DrawCenteredText(builder, typography,
            "Hello WPF + Impeller",
            yTop: 24, fontSize: 28, width: width,
            color: ImpellerColor.FromRgb(255, 255, 255));

        DrawCenteredText(builder, typography,
            "Vulkan backend  →  shared VkImage  →  D3DImage",
            yTop: 64, fontSize: 14, width: width,
            color: ImpellerColor.FromRgb(190, 200, 255));

        DrawCenteredText(builder, typography,
            $"frame {frameNumber}",
            yTop: height - 32, fontSize: 12, width: width,
            color: ImpellerColor.FromRgb(160, 160, 160));
    }

    private static void DrawCenteredText(
        ImpellerDisplayListBuilder builder,
        ImpellerTypographyContext typography,
        string text, float yTop, float fontSize, int width, ImpellerColor color)
    {
        using var paragraphBuilder = typography.ParagraphBuilderNew();
        if (paragraphBuilder == null) return;
        using var style = ImpellerParagraphStyle.New();
        if (style == null) return;
        using var paint = ImpellerPaint.New();
        if (paint == null) return;

        paint.SetColor(color);
        style.SetForeground(paint);
        style.SetFontSize(fontSize);
        style.SetTextAlignment(ImpellerTextAlignment.kImpellerTextAlignmentCenter);

        paragraphBuilder.PushStyle(style);
        paragraphBuilder.AddText(text);

        using var paragraph = paragraphBuilder.BuildParagraphNew(width: width);
        if (paragraph == null) return;
        builder.DrawParagraph(paragraph, new ImpellerPoint { X = 0, Y = (int)yTop });
    }

    private static (float r, float g, float b) HsvToRgb(float h, float s, float v)
    {
        float i = MathF.Floor(h * 6);
        float f = h * 6 - i;
        float p = v * (1 - s);
        float q = v * (1 - f * s);
        float t = v * (1 - (1 - f) * s);
        int section = ((int)i % 6 + 6) % 6;
        return section switch
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
