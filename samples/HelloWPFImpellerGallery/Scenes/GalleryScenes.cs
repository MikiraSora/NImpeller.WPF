using System;
using System.Collections.Generic;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;

/// <summary>
/// Static registry of every gallery scene the app exposes. Add new scenes by
/// appending to <see cref="All"/>.
/// </summary>
public static class GalleryScenes
{
    public static IReadOnlyList<IGalleryScene> All { get; } = new IGalleryScene[]
    {
        new BasicShapesScene(),
        new StrokeAndFillScene(),
        new StrokeStylesScene(),
        new PathsScene(),
        new DashedLinesScene(),
        new TransformsScene(),
        new BlendModesScene(),
        new ShadowsScene(),
        new TextBasicsScene(),
        new TextStylesScene(),
        new AnimationShowcaseScene(),
    };
}

// ============================================================================
// 1. Basic shapes — DrawRect / DrawOval / DrawRoundedRect / DrawLine
// ============================================================================
internal sealed class BasicShapesScene : IGalleryScene
{
    public string Name => "Basic Shapes";
    public string? Description => "DrawRect, DrawOval, DrawRoundedRect, DrawLine";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        ClearBackground(b, 0x1A, 0x1D, 0x22);

        var s = e.DpiScale;
        using var paintFill = ImpellerPaint.New()!;

        // Rectangle
        paintFill.SetColor(ImpellerColor.FromRgb(0xE8, 0x6F, 0x6F));
        b.DrawRect(new ImpellerRect(40, 40, 200, 140), paintFill);

        // Oval
        paintFill.SetColor(ImpellerColor.FromRgb(0x6F, 0xC2, 0xE8));
        b.DrawOval(new ImpellerRect(280, 40, 200, 140), paintFill);

        // Rounded rect
        paintFill.SetColor(ImpellerColor.FromRgb(0xE8, 0xCB, 0x6F));
        var corner = new ImpellerPoint { X = 24, Y = 24 };
        var radii = new ImpellerRoundingRadii
        {
            Top_left = corner, Top_right = corner,
            Bottom_left = corner, Bottom_right = corner,
        };
        b.DrawRoundedRect(new ImpellerRect(520, 40, 200, 140), radii, paintFill);

        // Diagonal line
        using var paintLine = ImpellerPaint.New()!;
        paintLine.SetColor(ImpellerColor.FromRgb(0xFF, 0xFF, 0xFF));
        paintLine.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        paintLine.SetStrokeWidth(4f * s);
        b.DrawLine(new ImpellerPoint { X = 40, Y = 240 }, new ImpellerPoint { X = 720, Y = 380 }, paintLine);

        // Rounded rect difference (outer minus inner) — like a ring shape
        paintFill.SetColor(ImpellerColor.FromRgb(0xB8, 0xE8, 0x6F));
        var outerR = new ImpellerRect(40, 420, 200, 200);
        var innerR = new ImpellerRect(80, 460, 120, 120);
        b.DrawRoundedRectDifference(outerR, radii, innerR, radii, paintFill);
    }

    private static void ClearBackground(ImpellerDisplayListBuilder b, byte r, byte g, byte bb)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(r, g, bb));
        b.DrawPaint(p);
    }
}

// ============================================================================
// 2. Stroke vs Fill — DrawStyle + StrokeWidth
// ============================================================================
internal sealed class StrokeAndFillScene : IGalleryScene
{
    public string Name => "Stroke vs Fill";
    public string? Description => "ImpellerDrawStyle Fill / Stroke / StrokeAndFill, varying width";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        ClearBg(b);
        var s = e.DpiScale;

        using var fill = ImpellerPaint.New()!;
        fill.SetColor(ImpellerColor.FromRgb(0x70, 0xA8, 0xE8));
        fill.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleFill);
        b.DrawRect(new ImpellerRect(40, 40, 180, 180), fill);

        using var stroke = ImpellerPaint.New()!;
        stroke.SetColor(ImpellerColor.FromRgb(0xE8, 0xA8, 0x70));
        stroke.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        stroke.SetStrokeWidth(6f * s);
        b.DrawRect(new ImpellerRect(260, 40, 180, 180), stroke);

        using var both = ImpellerPaint.New()!;
        both.SetColor(ImpellerColor.FromRgb(0xE8, 0xE8, 0x70));
        both.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStrokeAndFill);
        both.SetStrokeWidth(10f * s);
        b.DrawRect(new ImpellerRect(480, 40, 180, 180), both);

        // Increasing stroke widths
        using var label = ImpellerPaint.New()!;
        label.SetColor(ImpellerColor.FromRgb(0xCC, 0xCC, 0xCC));
        label.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        for (int i = 0; i < 8; i++)
        {
            label.SetStrokeWidth((1 + i * 2) * s);
            int y = 280 + i * 38;
            b.DrawLine(new ImpellerPoint { X = 40, Y = y }, new ImpellerPoint { X = 700, Y = y }, label);
        }
    }

    private static void ClearBg(ImpellerDisplayListBuilder b)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0x14, 0x18, 0x1D));
        b.DrawPaint(p);
    }
}

// ============================================================================
// 3. Stroke caps + joins
// ============================================================================
internal sealed class StrokeStylesScene : IGalleryScene
{
    public string Name => "Stroke Caps & Joins";
    public string? Description => "ImpellerStrokeCap: Butt/Round/Square. ImpellerStrokeJoin: Miter/Round/Bevel";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        ClearBg(b);
        var s = e.DpiScale;

        var caps = new[] { ImpellerStrokeCap.kImpellerStrokeCapButt, ImpellerStrokeCap.kImpellerStrokeCapRound, ImpellerStrokeCap.kImpellerStrokeCapSquare };
        var capNames = new[] { "Butt", "Round", "Square" };

        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0xE8, 0xC8, 0x70));
        p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        p.SetStrokeWidth(28f * s);

        for (int i = 0; i < caps.Length; i++)
        {
            p.SetStrokeCap(caps[i]);
            int y = 80 + i * 80;
            b.DrawLine(new ImpellerPoint { X = 120, Y = y }, new ImpellerPoint { X = 520, Y = y }, p);
        }

        // Joins illustrated by V-shaped paths
        var joins = new[] { ImpellerStrokeJoin.kImpellerStrokeJoinMiter, ImpellerStrokeJoin.kImpellerStrokeJoinRound, ImpellerStrokeJoin.kImpellerStrokeJoinBevel };
        for (int i = 0; i < joins.Length; i++)
        {
            using var pb = ImpellerPathBuilder.New()!;
            float x0 = 80 + i * 240;
            pb.MoveTo(new ImpellerPoint { X = x0, Y = 480 });
            pb.LineTo(new ImpellerPoint { X = x0 + 90, Y = 360 });
            pb.LineTo(new ImpellerPoint { X = x0 + 180, Y = 480 });
            using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;

            p.SetStrokeJoin(joins[i]);
            p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapButt);
            p.SetColor(ImpellerColor.FromRgb(0x70, 0xC8, 0xE8));
            b.DrawPath(path, p);
        }
    }

    private static void ClearBg(ImpellerDisplayListBuilder b)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0x14, 0x18, 0x1D));
        b.DrawPaint(p);
    }
}

// ============================================================================
// 4. Paths — MoveTo / LineTo / Quadratic / Cubic / Close
// ============================================================================
internal sealed class PathsScene : IGalleryScene
{
    public string Name => "Paths";
    public string? Description => "ImpellerPathBuilder: MoveTo, LineTo, QuadraticCurveTo, CubicCurveTo, Close";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        ClearBg(b);

        // 1) Polyline triangle (MoveTo + LineTo + Close)
        using (var pb = ImpellerPathBuilder.New()!)
        {
            pb.MoveTo(new ImpellerPoint { X = 100, Y = 280 });
            pb.LineTo(new ImpellerPoint { X = 240, Y = 60 });
            pb.LineTo(new ImpellerPoint { X = 380, Y = 280 });
            pb.Close();
            using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
            using var p = ImpellerPaint.New()!;
            p.SetColor(ImpellerColor.FromRgb(0xE8, 0x70, 0x70));
            b.DrawPath(path, p);
        }

        // 2) Quadratic curve
        using (var pb = ImpellerPathBuilder.New()!)
        {
            pb.MoveTo(new ImpellerPoint { X = 460, Y = 280 });
            pb.QuadraticCurveTo(new ImpellerPoint { X = 600, Y = 40 }, new ImpellerPoint { X = 740, Y = 280 });
            using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
            using var p = ImpellerPaint.New()!;
            p.SetColor(ImpellerColor.FromRgb(0x70, 0xE8, 0xA8));
            p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
            p.SetStrokeWidth(6f * e.DpiScale);
            b.DrawPath(path, p);
        }

        // 3) Cubic curve (heart-ish shape)
        using (var pb = ImpellerPathBuilder.New()!)
        {
            pb.MoveTo(new ImpellerPoint { X = 200, Y = 500 });
            pb.CubicCurveTo(
                new ImpellerPoint { X = 100, Y = 360 },
                new ImpellerPoint { X = 280, Y = 320 },
                new ImpellerPoint { X = 240, Y = 480 });
            pb.CubicCurveTo(
                new ImpellerPoint { X = 200, Y = 380 },
                new ImpellerPoint { X = 380, Y = 360 },
                new ImpellerPoint { X = 280, Y = 500 });
            pb.Close();
            using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
            using var p = ImpellerPaint.New()!;
            p.SetColor(ImpellerColor.FromRgb(0xE8, 0x70, 0xC8));
            b.DrawPath(path, p);
        }

        // 4) Star (alternating outer/inner radius — fill type Odd creates the hollow look)
        using (var pb = ImpellerPathBuilder.New()!)
        {
            const int points = 5;
            var cx = 600f; var cy = 460f;
            var rOuter = 100f; var rInner = 42f;
            for (int i = 0; i <= points * 2; i++)
            {
                var ang = -MathF.PI / 2 + i * MathF.PI / points;
                var r = (i % 2 == 0) ? rOuter : rInner;
                var pt = new ImpellerPoint { X = cx + MathF.Cos(ang) * r, Y = cy + MathF.Sin(ang) * r };
                if (i == 0) pb.MoveTo(pt); else pb.LineTo(pt);
            }
            pb.Close();
            using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeOdd)!;
            using var p = ImpellerPaint.New()!;
            p.SetColor(ImpellerColor.FromRgb(0xE8, 0xE8, 0x70));
            b.DrawPath(path, p);
        }
    }

    private static void ClearBg(ImpellerDisplayListBuilder b)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0x14, 0x18, 0x1D));
        b.DrawPaint(p);
    }
}

// ============================================================================
// 5. Dashed lines — DrawDashedLine on/off lengths
// ============================================================================
internal sealed class DashedLinesScene : IGalleryScene
{
    public string Name => "Dashed Lines";
    public string? Description => "DrawDashedLine with varying on/off lengths and stroke widths";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        ClearBg(b);
        var s = e.DpiScale;

        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0xE8, 0xE8, 0xE8));
        p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);

        // Different dash patterns
        (float onLen, float offLen, float width, string label)[] patterns =
        {
            (16, 10, 3, "16/10"),
            (32, 12, 5, "32/12"),
            ( 8,  8, 4, "8/8"),
            ( 4,  4, 2, "4/4"),
            (40, 20, 8, "40/20"),
            ( 2, 12, 3, "2/12 dots"),
        };

        for (int i = 0; i < patterns.Length; i++)
        {
            p.SetStrokeWidth(patterns[i].width * s);
            int y = 80 + i * 70;
            b.DrawDashedLine(
                new ImpellerPoint { X = 80, Y = y },
                new ImpellerPoint { X = 720, Y = y },
                patterns[i].onLen * s,
                patterns[i].offLen * s,
                p);
        }
    }

    private static void ClearBg(ImpellerDisplayListBuilder b)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0x14, 0x18, 0x1D));
        b.DrawPaint(p);
    }
}

// ============================================================================
// 6. Transforms — Save/Restore + Translate/Rotate/Scale
// ============================================================================
internal sealed class TransformsScene : IGalleryScene
{
    public string Name => "Transforms";
    public string? Description => "Save / Restore, Translate, Rotate, Scale (animated)";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        ClearBg(b);
        float t = (float)e.TotalTime.TotalSeconds;
        var s = e.DpiScale;

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

// ============================================================================
// 7. Blend modes — pairs of overlapping shapes under different blend modes
// ============================================================================
internal sealed class BlendModesScene : IGalleryScene
{
    public string Name => "Blend Modes";
    public string? Description => "ImpellerBlendMode: Multiply, Screen, Overlay, Plus, Difference, Modulate";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        ClearBg(b);

        (ImpellerBlendMode mode, string label)[] modes =
        {
            (ImpellerBlendMode.kImpellerBlendModeSourceOver, "SourceOver"),
            (ImpellerBlendMode.kImpellerBlendModeMultiply,   "Multiply"),
            (ImpellerBlendMode.kImpellerBlendModeScreen,     "Screen"),
            (ImpellerBlendMode.kImpellerBlendModeOverlay,    "Overlay"),
            (ImpellerBlendMode.kImpellerBlendModePlus,       "Plus"),
            (ImpellerBlendMode.kImpellerBlendModeDifference, "Difference"),
            (ImpellerBlendMode.kImpellerBlendModeColorDodge, "ColorDodge"),
            (ImpellerBlendMode.kImpellerBlendModeColorBurn,  "ColorBurn"),
        };

        const int cols = 4;
        const int cellW = 180, cellH = 200;
        const int xPad = 30, yPad = 30;

        using var p1 = ImpellerPaint.New()!;
        using var p2 = ImpellerPaint.New()!;
        p1.SetColor(ImpellerColor.FromRgb(0xE8, 0x40, 0x40));
        // p2 color set per-cell with blend mode

        for (int i = 0; i < modes.Length; i++)
        {
            int col = i % cols;
            int row = i / cols;
            float x = xPad + col * (cellW + 10);
            float y = yPad + row * (cellH + 10);

            // bottom red circle
            b.DrawOval(new ImpellerRect((int)x + 20, (int)y + 30, 110, 110), p1);

            // top blue circle with the test blend mode
            p2.SetColor(ImpellerColor.FromRgb(0x40, 0x80, 0xE8));
            p2.SetBlendMode(modes[i].mode);
            b.DrawOval(new ImpellerRect((int)x + 60, (int)y + 60, 110, 110), p2);
        }
    }

    private static void ClearBg(ImpellerDisplayListBuilder b)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0x10, 0x10, 0x10));
        b.DrawPaint(p);
    }
}

// ============================================================================
// 8. Shadows — DrawShadow under filled paths
// ============================================================================
internal sealed class ShadowsScene : IGalleryScene
{
    public string Name => "Shadows";
    public string? Description => "DrawShadow with varying elevation";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        ClearBg(b);

        var elevations = new float[] { 2, 6, 16, 36 };

        using var fill = ImpellerPaint.New()!;
        fill.SetColor(ImpellerColor.FromRgb(0xF2, 0xF2, 0xF2));
        var shadowColor = ImpellerColor.FromRgb(0x00, 0x00, 0x00);

        for (int i = 0; i < elevations.Length; i++)
        {
            float x = 60 + i * 180;
            float y = 220;

            using var pb = ImpellerPathBuilder.New()!;
            var corner = new ImpellerPoint { X = 24, Y = 24 };
            var radii = new ImpellerRoundingRadii
            {
                Top_left = corner, Top_right = corner,
                Bottom_left = corner, Bottom_right = corner,
            };
            pb.AddRoundedRect(new ImpellerRect((int)x, (int)y, 140, 200), radii);
            using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;

            b.DrawShadow(path, shadowColor, elevations[i], 0, (float)e.DpiScale);
            b.DrawPath(path, fill);
        }
    }

    private static void ClearBg(ImpellerDisplayListBuilder b)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0x30, 0x32, 0x38));
        b.DrawPaint(p);
    }
}

// ============================================================================
// 9. Text basics — font size and weight
// ============================================================================
internal sealed class TextBasicsScene : IGalleryScene
{
    public string Name => "Text Basics";
    public string? Description => "Font size & weight via ImpellerParagraphBuilder + ImpellerParagraphStyle";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        ClearBg(b);
        if (e.Typography == null) return;

        var s = e.DpiScale;
        var sizes = new (float size, string label)[]
        {
            (12 * s, "12pt"), (16 * s, "16pt"), (22 * s, "22pt"),
            (32 * s, "32pt"), (48 * s, "48pt"),
        };
        float y = 40 * s;
        foreach (var (size, label) in sizes)
        {
            DrawSimpleText(b, e.Typography, $"{label} The quick brown fox", size, x: 40 * s, y, e.PixelWidth,
                ImpellerColor.FromRgb(255, 255, 255), ImpellerFontWeight.kImpellerFontWeight400);
            y += size + 12 * s;
        }

        y += 24 * s;
        var weights = new (ImpellerFontWeight w, string label)[]
        {
            (ImpellerFontWeight.kImpellerFontWeight300, "Light"),
            (ImpellerFontWeight.kImpellerFontWeight400, "Regular"),
            (ImpellerFontWeight.kImpellerFontWeight500, "Medium"),
            (ImpellerFontWeight.kImpellerFontWeight700, "Bold"),
            (ImpellerFontWeight.kImpellerFontWeight900, "Black"),
        };
        foreach (var (w, label) in weights)
        {
            DrawSimpleText(b, e.Typography, $"{label} weight", 24 * s, x: 40 * s, y, e.PixelWidth,
                ImpellerColor.FromRgb(0xE8, 0xE8, 0xE8), w);
            y += 32 * s;
        }
    }

    internal static void DrawSimpleText(
        ImpellerDisplayListBuilder b, ImpellerTypographyContext typography,
        string text, float fontSize, float x, float y, int width, ImpellerColor color,
        ImpellerFontWeight weight = ImpellerFontWeight.kImpellerFontWeight400,
        ImpellerTextAlignment align = ImpellerTextAlignment.kImpellerTextAlignmentLeft)
    {
        using var paragraphBuilder = typography.ParagraphBuilderNew();
        if (paragraphBuilder == null) return;
        using var style = ImpellerParagraphStyle.New();
        if (style == null) return;
        using var paint = ImpellerPaint.New();
        if (paint == null) return;

        paint.SetColor(color);
        style.SetForeground(paint);
        style.SetFontSize(MathF.Round(fontSize));
        style.SetFontWeight(weight);
        style.SetHeight(1.0f);
        style.SetTextAlignment(align);
        paragraphBuilder.PushStyle(style);
        paragraphBuilder.AddText(text);
        using var paragraph = paragraphBuilder.BuildParagraphNew(width: width);
        if (paragraph == null) return;
        b.DrawParagraph(paragraph, new ImpellerPoint { X = MathF.Round(x), Y = MathF.Round(y) });
    }

    private static void ClearBg(ImpellerDisplayListBuilder b)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0x14, 0x18, 0x1D));
        b.DrawPaint(p);
    }
}

// ============================================================================
// 10. Text styles — alignment + decorations
// ============================================================================
internal sealed class TextStylesScene : IGalleryScene
{
    public string Name => "Text Alignment & Decoration";
    public string? Description => "Left / Center / Right alignment, underline, strikethrough";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        ClearBg(b);
        if (e.Typography == null) return;

        var s = e.DpiScale;
        float y = 30 * s;
        float lineH = 36 * s;

        TextBasicsScene.DrawSimpleText(b, e.Typography, "Left aligned", 22 * s,
            40 * s, y, e.PixelWidth - 80, ImpellerColor.FromRgb(255, 255, 255),
            align: ImpellerTextAlignment.kImpellerTextAlignmentLeft);
        y += lineH;

        TextBasicsScene.DrawSimpleText(b, e.Typography, "Center aligned", 22 * s,
            40 * s, y, e.PixelWidth - 80, ImpellerColor.FromRgb(255, 255, 255),
            align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
        y += lineH;

        TextBasicsScene.DrawSimpleText(b, e.Typography, "Right aligned", 22 * s,
            40 * s, y, e.PixelWidth - 80, ImpellerColor.FromRgb(255, 255, 255),
            align: ImpellerTextAlignment.kImpellerTextAlignmentRight);
        y += lineH * 2;

        // Decorations
        DrawTextWithDecoration(b, e.Typography, "Underlined text", 26 * s, 40 * s, y, e.PixelWidth - 80,
            ImpellerColor.FromRgb(255, 255, 255),
            new ImpellerTextDecoration(
                ImpellerTextDecorationType.kImpellerTextDecorationTypeUnderline,
                ImpellerTextDecorationStyle.kImpellerTextDecorationStyleSolid,
                ImpellerColor.FromRgb(255, 80, 80)));
        y += lineH * 1.4f;

        DrawTextWithDecoration(b, e.Typography, "Strikethrough text", 26 * s, 40 * s, y, e.PixelWidth - 80,
            ImpellerColor.FromRgb(255, 255, 255),
            new ImpellerTextDecoration(
                ImpellerTextDecorationType.kImpellerTextDecorationTypeLineThrough,
                ImpellerTextDecorationStyle.kImpellerTextDecorationStyleSolid,
                ImpellerColor.FromRgb(80, 200, 255)));
        y += lineH * 1.4f;

        DrawTextWithDecoration(b, e.Typography, "Underline + Overline + dashed", 26 * s, 40 * s, y, e.PixelWidth - 80,
            ImpellerColor.FromRgb(220, 220, 220),
            new ImpellerTextDecoration(
                ImpellerTextDecorationType.kImpellerTextDecorationTypeUnderline | ImpellerTextDecorationType.kImpellerTextDecorationTypeOverline,
                ImpellerTextDecorationStyle.kImpellerTextDecorationStyleDashed,
                ImpellerColor.FromRgb(160, 220, 80)));
        y += lineH * 1.6f;

        // CJK rendering
        TextBasicsScene.DrawSimpleText(b, e.Typography, "中文字符渲染：你好，世界！", 26 * s,
            40 * s, y, e.PixelWidth - 80, ImpellerColor.FromRgb(255, 200, 100));
    }

    private static void DrawTextWithDecoration(
        ImpellerDisplayListBuilder b, ImpellerTypographyContext typography,
        string text, float fontSize, float x, float y, int width, ImpellerColor color, ImpellerTextDecoration decoration)
    {
        using var paragraphBuilder = typography.ParagraphBuilderNew();
        if (paragraphBuilder == null) return;
        using var style = ImpellerParagraphStyle.New();
        if (style == null) return;
        using var paint = ImpellerPaint.New();
        if (paint == null) return;

        paint.SetColor(color);
        style.SetForeground(paint);
        style.SetFontSize(MathF.Round(fontSize));
        style.SetHeight(1.0f);
        style.SetTextDecoration(decoration);
        paragraphBuilder.PushStyle(style);
        paragraphBuilder.AddText(text);
        using var paragraph = paragraphBuilder.BuildParagraphNew(width: width);
        if (paragraph == null) return;
        b.DrawParagraph(paragraph, new ImpellerPoint { X = MathF.Round(x), Y = MathF.Round(y) });
    }

    private static void ClearBg(ImpellerDisplayListBuilder b)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0x14, 0x18, 0x1D));
        b.DrawPaint(p);
    }
}

// ============================================================================
// 11. Animation showcase — combined Transforms + Paths + Text + Shapes
// ============================================================================
internal sealed class AnimationShowcaseScene : IGalleryScene
{
    public string Name => "Animation Showcase";
    public string? Description => "Combined animated scene (background + orbiting rectangles + central pulse + text)";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        float t = (float)e.TotalTime.TotalSeconds;
        int w = e.PixelWidth, h = e.PixelHeight;
        var s = e.DpiScale;

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
            float boxHalf = 28f * s;
            float cornerR = 10f * s;
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
            TextBasicsScene.DrawSimpleText(b, e.Typography, "Impeller Gallery — Showcase", 28 * s,
                0, 24 * s, w, ImpellerColor.FromRgb(255, 255, 255),
                weight: ImpellerFontWeight.kImpellerFontWeight600,
                align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);

            TextBasicsScene.DrawSimpleText(b, e.Typography, $"frame {e.FrameNumber}", 14 * s,
                0, h - 26 * s, w, ImpellerColor.FromRgb(180, 180, 180),
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
