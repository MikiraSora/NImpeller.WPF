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
        new SystemInfoScene(),
        new BasicShapesScene(),
        new StrokeAndFillScene(),
        new StrokeStylesScene(),
        new PathsScene(),
        new DashedLinesScene(),
        new TransformsScene(),
        new BlendModesScene(),
        new ShadowsScene(),
        new ClippingScene(),
        new SaveLayerScene(),
        new MaskBlurScene(),
        new ColorMatrixScene(),
        new BackdropBlurScene(),
        new TextBasicsScene(),
        new TextStylesScene(),
        new AnalogClockScene(),
        new BarChartScene(),
        new PieChartScene(),
        new LoadingSpinnersScene(),
        new ParticleFieldScene(),
        new SpirographScene(),
        new CardLayoutScene(),
        new HexGridScene(),
        new WaveLinesScene(),
        new DonutChartScene(),
        new GaugeScene(),
        new SparklineScene(),
        new AnimationShowcaseScene(),
        // [StressTest] series — push base APIs to find frame-time ceilings
        new StressTestRectsScene(),
        new StressTestCirclesScene(),
        new StressTestRoundedRectsScene(),
        new StressTestLinesScene(),
        new StressTestPathsScene(),
        new StressTestTextScene(),
        new StressTestTransformsScene(),
        new StressTestBlurScene(),
        new StressTestShadowsScene(),
        new StressTestSaveLayersScene(),
        new StressTestMixedPipelineScene(),
    };
}

// ============================================================================
// 0. System Info — Impeller / Vulkan / GPU details (shown first by default)
// ============================================================================
internal sealed class SystemInfoScene : IGalleryScene
{
    public string Name => "System Info";
    public string? Description => "Impeller version, Vulkan API, selected GPU, memory heaps, current view metrics";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x18, 0x1C, 0x24);
        if (e.Typography == null) return;
        var s = e.DpiScale;

        var info = ImpellerSystemInfo.GpuInfo;

        float x = 40 * s;
        float y = 30 * s;
        float lineH = 24 * s;

        TextBasicsScene.DrawSimpleText(b, e.Typography, "Impeller × Vulkan × GPU", 30 * s,
            x, y, e.PixelWidth, ImpellerColor.FromRgb(0xFF, 0xFF, 0xFF),
            weight: ImpellerFontWeight.kImpellerFontWeight700);
        y += 50 * s;

        if (info == null)
        {
            TextBasicsScene.DrawSimpleText(b, e.Typography, "ImpellerSystemInfo.GpuInfo is null (host not initialized yet).",
                14 * s, x, y, e.PixelWidth - (int)x,
                ImpellerColor.FromRgb(0xE8, 0x70, 0x70));
            return;
        }

        var groupHeader = ImpellerColor.FromRgb(0x6F, 0xC2, 0xE8);
        var labelColor = ImpellerColor.FromRgb(0xA0, 0xA8, 0xB2);
        var valueColor = ImpellerColor.FromRgb(0xE8, 0xE8, 0xE8);

        void DrawHeader(string text)
        {
            TextBasicsScene.DrawSimpleText(b, e.Typography!, text, 16 * s,
                x, y, e.PixelWidth - (int)x, groupHeader,
                weight: ImpellerFontWeight.kImpellerFontWeight700);
            y += lineH + 4 * s;
        }
        void DrawRow(string label, string value)
        {
            TextBasicsScene.DrawSimpleText(b, e.Typography!, label, 14 * s,
                x + 18 * s, y, 200, labelColor);
            TextBasicsScene.DrawSimpleText(b, e.Typography!, value, 14 * s,
                x + 230 * s, y, e.PixelWidth - 280, valueColor,
                weight: ImpellerFontWeight.kImpellerFontWeight500);
            y += lineH;
        }

        // === Impeller ===
        DrawHeader("Impeller");
        DrawRow("API version",     $"{info.ImpellerApiVersion}  (raw 0x{info.ImpellerApiVersionRaw:X8})");
        DrawRow("Backend",          "Vulkan");
        y += 8 * s;

        // === Vulkan ===
        DrawHeader("Vulkan");
        DrawRow("API version",      $"{info.VulkanApiVersion}  (raw 0x{info.VulkanApiVersionRaw:X8})");
        DrawRow("Driver version",   $"0x{info.DriverVersionRaw:X8}");
        DrawRow("Instance",         $"0x{(long)info.VkInstance:X16}");
        DrawRow("Physical device",  $"0x{(long)info.VkPhysicalDevice:X16}");
        DrawRow("Logical device",   $"0x{(long)info.VkDevice:X16}");
        DrawRow("Queue",            $"0x{(long)info.VkQueue:X16}  family {info.QueueFamilyIndex}  index {info.QueueIndex}");
        y += 8 * s;

        // === GPU ===
        DrawHeader("GPU");
        DrawRow("Vendor",           $"{info.VendorName}  (id 0x{info.VendorId:X4})");
        DrawRow("Device name",      info.DeviceName);
        DrawRow("Device ID",        $"0x{info.DeviceId:X4}");
        DrawRow("Device type",      info.DeviceType);
        DrawRow("D3D adapter LUID", $"0x{info.AdapterLuid:X16}");
        DrawRow("DeviceLocal mem",  FormatBytes(info.DeviceLocalMemoryBytes));
        DrawRow("HostVisible mem",  FormatBytes(info.HostVisibleMemoryBytes));
        DrawRow("Max 2D image",     $"{info.MaxImageDimension2D} × {info.MaxImageDimension2D}");
        DrawRow("Max framebuffer",  $"{info.MaxFramebufferWidth} × {info.MaxFramebufferHeight}");
        y += 8 * s;

        // === Current ImpellerView ===
        DrawHeader("Current ImpellerView");
        DrawRow("Pixel size",       $"{e.PixelWidth} × {e.PixelHeight}");
        DrawRow("DPI scale",        $"{e.DpiScale:0.###}×");
        DrawRow("Frame number",     e.FrameNumber.ToString("N0"));
        DrawRow("Frame delta",      $"{e.DeltaTime.TotalMilliseconds:0.00} ms");
        DrawRow("Total time",       $"{e.TotalTime.TotalSeconds:0.0} s");
    }

    private static string FormatBytes(ulong bytes)
    {
        if (bytes == 0) return "—";
        if (bytes >= 1L << 30) return $"{bytes / (double)(1L << 30):0.00} GiB";
        if (bytes >= 1L << 20) return $"{bytes / (double)(1L << 20):0.0} MiB";
        if (bytes >= 1L << 10) return $"{bytes / (double)(1L << 10):0.0} KiB";
        return $"{bytes} B";
    }
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

// ============================================================================
// Helpers shared by the new scenes below
// ============================================================================
internal static class SceneHelpers
{
    public static void ClearBg(ImpellerDisplayListBuilder b, byte r = 0x14, byte g = 0x18, byte bb = 0x1D)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(r, g, bb));
        b.DrawPaint(p);
    }

    public static (float r, float g, float b) HsvToRgb(float h, float s, float v)
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

    public static ImpellerRoundingRadii UniformRadii(float r) =>
        new ImpellerRoundingRadii
        {
            Top_left = new ImpellerPoint { X = r, Y = r },
            Top_right = new ImpellerPoint { X = r, Y = r },
            Bottom_left = new ImpellerPoint { X = r, Y = r },
            Bottom_right = new ImpellerPoint { X = r, Y = r },
        };
}

// ============================================================================
// 12. Clipping — ClipRect / ClipOval / ClipPath / ClipRoundedRect
// ============================================================================
internal sealed class ClippingScene : IGalleryScene
{
    public string Name => "Clipping";
    public string? Description => "ClipRect, ClipOval, ClipRoundedRect, ClipPath with Intersect/Difference";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b);
        var s = e.DpiScale;

        // The thing being clipped: a rainbow striped rectangle
        void DrawStripes(ImpellerDisplayListBuilder b, ImpellerRect bounds)
        {
            using var p = ImpellerPaint.New()!;
            for (int i = 0; i < 12; i++)
            {
                var (r, g, bb) = SceneHelpers.HsvToRgb(i / 12f, 0.85f, 1.0f);
                p.SetColor(new ImpellerColor { Alpha = 1, Red = r, Green = g, Blue = bb });
                int sliceH = (int)(bounds.Height / 12);
                b.DrawRect(new ImpellerRect((int)bounds.X, (int)bounds.Y + i * sliceH, (int)bounds.Width, sliceH), p);
            }
        }

        // 1) ClipRect intersect
        b.Save();
        b.ClipRect(new ImpellerRect(60, 60, 240, 200), ImpellerClipOperation.kImpellerClipOperationIntersect);
        DrawStripes(b, new ImpellerRect(0, 0, e.PixelWidth, e.PixelHeight));
        b.Restore();

        // 2) ClipOval intersect
        b.Save();
        b.ClipOval(new ImpellerRect(340, 60, 240, 200), ImpellerClipOperation.kImpellerClipOperationIntersect);
        DrawStripes(b, new ImpellerRect(0, 0, e.PixelWidth, e.PixelHeight));
        b.Restore();

        // 3) ClipRoundedRect intersect
        b.Save();
        b.ClipRoundedRect(new ImpellerRect(620, 60, 240, 200),
            SceneHelpers.UniformRadii(40), ImpellerClipOperation.kImpellerClipOperationIntersect);
        DrawStripes(b, new ImpellerRect(0, 0, e.PixelWidth, e.PixelHeight));
        b.Restore();

        // 4) ClipPath (star) intersect
        b.Save();
        using (var pb = ImpellerPathBuilder.New()!)
        {
            const int points = 5;
            var cx = 180f; var cy = 460f;
            var rOuter = 110f; var rInner = 46f;
            for (int i = 0; i <= points * 2; i++)
            {
                var ang = -MathF.PI / 2 + i * MathF.PI / points;
                var r = (i % 2 == 0) ? rOuter : rInner;
                var pt = new ImpellerPoint { X = cx + MathF.Cos(ang) * r, Y = cy + MathF.Sin(ang) * r };
                if (i == 0) pb.MoveTo(pt); else pb.LineTo(pt);
            }
            pb.Close();
            using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
            b.ClipPath(path, ImpellerClipOperation.kImpellerClipOperationIntersect);
        }
        DrawStripes(b, new ImpellerRect(0, 0, e.PixelWidth, e.PixelHeight));
        b.Restore();

        // 5) ClipOval *Difference* — hole punched through the rainbow
        b.Save();
        b.ClipRect(new ImpellerRect(360, 340, 480, 240), ImpellerClipOperation.kImpellerClipOperationIntersect);
        b.ClipOval(new ImpellerRect(520, 380, 160, 160), ImpellerClipOperation.kImpellerClipOperationDifference);
        DrawStripes(b, new ImpellerRect(0, 0, e.PixelWidth, e.PixelHeight));
        b.Restore();
    }
}

// ============================================================================
// 13. SaveLayer — group transparency
// ============================================================================
internal sealed class SaveLayerScene : IGalleryScene
{
    public string Name => "SaveLayer";
    public string? Description => "SaveLayer applies an opacity / blend to a group of drawings as one unit";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b);

        // LEFT: per-paint alpha = 0.5 — each shape is half transparent and overlaps blend visibly
        using (var p = ImpellerPaint.New()!)
        {
            p.SetColor(ImpellerColor.FromRgb(0xE8, 0xE8, 0xE8));
            using var pb = ImpellerPathBuilder.New()!;
            pb.AddRect(new ImpellerRect(40, 40, 280, 280));
            using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
            p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
            p.SetStrokeWidth(1f);
            b.DrawPath(path, p);
        }
        b.Save();
        b.ClipRect(new ImpellerRect(40, 40, 280, 280), ImpellerClipOperation.kImpellerClipOperationIntersect);
        using (var p = ImpellerPaint.New()!)
        {
            p.SetColor(new ImpellerColor { Alpha = 0.5f, Red = 1, Green = 0.2f, Blue = 0.2f });
            b.DrawOval(new ImpellerRect(80, 90, 120, 120), p);
            p.SetColor(new ImpellerColor { Alpha = 0.5f, Red = 0.2f, Green = 1, Blue = 0.2f });
            b.DrawOval(new ImpellerRect(140, 90, 120, 120), p);
            p.SetColor(new ImpellerColor { Alpha = 0.5f, Red = 0.2f, Green = 0.2f, Blue = 1 });
            b.DrawOval(new ImpellerRect(110, 150, 120, 120), p);
        }
        b.Restore();
        DrawLabel(b, e, "Per-paint alpha=0.5", 60, 340);

        // RIGHT: SaveLayer with alpha=0.5 — all 3 shapes are opaque inside the layer,
        //        then the entire layer is composited at 0.5 alpha
        b.Save();
        using (var layerPaint = ImpellerPaint.New()!)
        {
            layerPaint.SetColor(new ImpellerColor { Alpha = 0.5f, Red = 1, Green = 1, Blue = 1 });
            using var noBackdrop = ImpellerImageFilter.CreateBlurNew(0f, 0f, ImpellerTileMode.kImpellerTileModeClamp)!;
            b.SaveLayer(new ImpellerRect(380, 40, 280, 280), layerPaint, noBackdrop);
        }
        using (var p = ImpellerPaint.New()!)
        {
            p.SetColor(new ImpellerColor { Alpha = 1, Red = 1, Green = 0.2f, Blue = 0.2f });
            b.DrawOval(new ImpellerRect(420, 90, 120, 120), p);
            p.SetColor(new ImpellerColor { Alpha = 1, Red = 0.2f, Green = 1, Blue = 0.2f });
            b.DrawOval(new ImpellerRect(480, 90, 120, 120), p);
            p.SetColor(new ImpellerColor { Alpha = 1, Red = 0.2f, Green = 0.2f, Blue = 1 });
            b.DrawOval(new ImpellerRect(450, 150, 120, 120), p);
        }
        b.Restore(); // pops SaveLayer
        b.Restore();
        DrawLabel(b, e, "SaveLayer with alpha=0.5", 400, 340);
    }

    private static void DrawLabel(ImpellerDisplayListBuilder b, ImpellerRenderEventArgs e, string text, float x, float y)
    {
        if (e.Typography == null) return;
        TextBasicsScene.DrawSimpleText(b, e.Typography, text, 16, x, y, e.PixelWidth - (int)x,
            ImpellerColor.FromRgb(0xE8, 0xE8, 0xE8));
    }
}

// ============================================================================
// 14. Mask Filter — gaussian blur on shape edges
// ============================================================================
internal sealed class MaskBlurScene : IGalleryScene
{
    public string Name => "Mask Filter (Blur)";
    public string? Description => "ImpellerMaskFilter.CreateBlurNew with Normal/Solid/Outer/Inner styles";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b);
        var s = e.DpiScale;

        (ImpellerBlurStyle style, string label)[] styles =
        {
            (ImpellerBlurStyle.kImpellerBlurStyleNormal, "Normal"),
            (ImpellerBlurStyle.kImpellerBlurStyleSolid,  "Solid"),
            (ImpellerBlurStyle.kImpellerBlurStyleOuter,  "Outer"),
            (ImpellerBlurStyle.kImpellerBlurStyleInner,  "Inner"),
        };

        const int cellW = 220;
        const int cellH = 220;
        const int xPad = 30;
        const int yPad = 60;

        for (int i = 0; i < styles.Length; i++)
        {
            int x = xPad + i * (cellW + 10);
            using var mask = ImpellerMaskFilter.CreateBlurNew(styles[i].style, 12f)!;
            using var p = ImpellerPaint.New()!;
            p.SetColor(ImpellerColor.FromRgb(0xE8, 0x8F, 0x6F));
            p.SetMaskFilter(mask);
            b.DrawOval(new ImpellerRect(x + 30, yPad + 30, cellW - 60, cellH - 60), p);

            if (e.Typography != null)
                TextBasicsScene.DrawSimpleText(b, e.Typography, styles[i].label, 16 * s,
                    x, yPad + cellH + 10, cellW, ImpellerColor.FromRgb(0xCC, 0xCC, 0xCC),
                    align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
        }

        // Comparison row: varying sigma
        for (int i = 0; i < 6; i++)
        {
            float sigma = 1 + i * 6f;
            using var mask = ImpellerMaskFilter.CreateBlurNew(ImpellerBlurStyle.kImpellerBlurStyleNormal, sigma)!;
            using var p = ImpellerPaint.New()!;
            p.SetColor(ImpellerColor.FromRgb(0x8F, 0xC8, 0xE8));
            p.SetMaskFilter(mask);
            int x = 60 + i * 130;
            b.DrawRect(new ImpellerRect(x, 440, 90, 90), p);

            if (e.Typography != null)
                TextBasicsScene.DrawSimpleText(b, e.Typography, $"σ={sigma:0.#}", 12 * s,
                    x, 545, 90, ImpellerColor.FromRgb(0xAA, 0xAA, 0xAA),
                    align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
        }
    }
}

// ============================================================================
// 15. Color Matrix — grayscale, invert, sepia, hue rotate
// ============================================================================
internal sealed class ColorMatrixScene : IGalleryScene
{
    public string Name => "Color Matrix Filter";
    public string? Description => "ImpellerColorFilter.CreateColorMatrixNew — grayscale, invert, sepia, hue";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b);

        // Draw a colorful test pattern, then re-draw it 4 more times in clipped
        // regions, each with a different color matrix.
        (string label, float[] m)[] filters =
        {
            ("Identity", new float[] {
                1,0,0,0,0,
                0,1,0,0,0,
                0,0,1,0,0,
                0,0,0,1,0,
            }),
            ("Grayscale", new float[] {
                0.299f,0.587f,0.114f,0,0,
                0.299f,0.587f,0.114f,0,0,
                0.299f,0.587f,0.114f,0,0,
                0,0,0,1,0,
            }),
            ("Invert", new float[] {
                -1,0,0,0,1,
                0,-1,0,0,1,
                0,0,-1,0,1,
                0,0,0,1,0,
            }),
            ("Sepia", new float[] {
                0.393f,0.769f,0.189f,0,0,
                0.349f,0.686f,0.168f,0,0,
                0.272f,0.534f,0.131f,0,0,
                0,0,0,1,0,
            }),
        };

        const int cellW = 240;
        const int cellH = 180;
        const int xPad = 30;
        const int yPad = 40;

        for (int i = 0; i < filters.Length; i++)
        {
            int col = i % 4;
            int x = xPad + col * (cellW + 8);
            int y = yPad;

            b.Save();
            b.ClipRect(new ImpellerRect(x, y, cellW, cellH), ImpellerClipOperation.kImpellerClipOperationIntersect);

            using var layerPaint = ImpellerPaint.New()!;
            unsafe
            {
                var cm = new ImpellerColorMatrix();
                for (int k = 0; k < 20; k++) cm.m[k] = filters[i].m[k];
                using var cf = ImpellerColorFilter.CreateColorMatrixNew(cm)!;
                layerPaint.SetColorFilter(cf);
            }
            // Use a SaveLayer so the color filter applies to the *entire group* of drawings below.
            using var nullBackdrop = ImpellerImageFilter.CreateBlurNew(0f, 0f, ImpellerTileMode.kImpellerTileModeClamp)!;
            b.SaveLayer(new ImpellerRect(x, y, cellW, cellH), layerPaint, nullBackdrop);

            DrawColorfulPattern(b, x, y, cellW, cellH);

            b.Restore(); // SaveLayer
            b.Restore(); // ClipRect

            if (e.Typography != null)
                TextBasicsScene.DrawSimpleText(b, e.Typography, filters[i].label, 16 * e.DpiScale,
                    x, y + cellH + 8, cellW, ImpellerColor.FromRgb(0xE8, 0xE8, 0xE8),
                    align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
        }
    }

    private static void DrawColorfulPattern(ImpellerDisplayListBuilder b, int x, int y, int w, int h)
    {
        using var p = ImpellerPaint.New()!;
        var colors = new (byte r, byte g, byte bb)[]
        {
            (0xE8, 0x6F, 0x6F), (0xE8, 0xCB, 0x6F), (0x6F, 0xC2, 0xE8),
            (0xB8, 0xE8, 0x6F), (0xE8, 0x6F, 0xC8), (0x6F, 0x80, 0xE8),
        };
        for (int i = 0; i < colors.Length; i++)
        {
            var c = colors[i];
            p.SetColor(ImpellerColor.FromRgb(c.r, c.g, c.bb));
            int cellW = w / 3;
            int cellH = h / 2;
            int cx = x + (i % 3) * cellW;
            int cy = y + (i / 3) * cellH;
            b.DrawOval(new ImpellerRect(cx + 6, cy + 6, cellW - 12, cellH - 12), p);
        }
    }
}

// ============================================================================
// 16. Backdrop Blur — frosted glass effect using SaveLayer with backdrop ImageFilter
// ============================================================================
internal sealed class BackdropBlurScene : IGalleryScene
{
    public string Name => "Backdrop Blur (Frosted Glass)";
    public string? Description => "SaveLayer with backdrop blur ImageFilter — iOS-style frosted glass effect";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b);
        var s = e.DpiScale;
        float t = (float)e.TotalTime.TotalSeconds;

        // Animated colorful background
        using (var p = ImpellerPaint.New()!)
        {
            for (int i = 0; i < 12; i++)
            {
                float ang = t * 0.4f + i * (MathF.PI * 2 / 12);
                float cx = e.PixelWidth / 2f + MathF.Cos(ang) * 240;
                float cy = e.PixelHeight / 2f + MathF.Sin(ang) * 180;
                var (r, g, bb) = SceneHelpers.HsvToRgb(i / 12f, 0.85f, 1.0f);
                p.SetColor(new ImpellerColor { Alpha = 0.9f, Red = r, Green = g, Blue = bb });
                b.DrawOval(new ImpellerRect((int)cx - 80, (int)cy - 80, 160, 160), p);
            }
        }

        // Frosted glass strip across the middle
        int yBand = (int)(e.PixelHeight / 2 - 80);
        int hBand = 160;

        b.Save();
        b.ClipRoundedRect(new ImpellerRect(40, yBand, e.PixelWidth - 80, hBand),
            SceneHelpers.UniformRadii(28), ImpellerClipOperation.kImpellerClipOperationIntersect);
        using (var glassPaint = ImpellerPaint.New()!)
        {
            glassPaint.SetColor(new ImpellerColor { Alpha = 0.25f, Red = 1, Green = 1, Blue = 1 });
            using var blur = ImpellerImageFilter.CreateBlurNew(18f, 18f, ImpellerTileMode.kImpellerTileModeClamp)!;
            b.SaveLayer(new ImpellerRect(40, yBand, e.PixelWidth - 80, hBand), glassPaint, blur);
            // Tint
            using var tint = ImpellerPaint.New()!;
            tint.SetColor(new ImpellerColor { Alpha = 0.35f, Red = 1, Green = 1, Blue = 1 });
            b.DrawRect(new ImpellerRect(40, yBand, e.PixelWidth - 80, hBand), tint);
            b.Restore();
        }
        b.Restore();

        if (e.Typography != null)
        {
            TextBasicsScene.DrawSimpleText(b, e.Typography, "Frosted Glass via Backdrop Blur",
                24 * s, 0, yBand + 60, e.PixelWidth,
                ImpellerColor.FromRgb(0x18, 0x18, 0x18),
                weight: ImpellerFontWeight.kImpellerFontWeight600,
                align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
        }
    }
}

// ============================================================================
// 17. Analog Clock — real-time clock with smooth seconds hand
// ============================================================================
internal sealed class AnalogClockScene : IGalleryScene
{
    public string Name => "Analog Clock";
    public string? Description => "Live wall clock — minute marks, tick marks, smooth-sweep seconds hand";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x18, 0x1C, 0x24);
        var s = e.DpiScale;

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
            p.SetStrokeWidth(8f * s);
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
                p.SetStrokeWidth((isHour ? 5f : 2f) * s);
                float ang = i * MathF.PI / 30 - MathF.PI / 2;
                float outerR = r - 8 * s;
                float innerR = outerR - (isHour ? 22 * s : 10 * s);
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
        DrawHand(b, cx, cy, hrF / 12f, r * 0.50f, 9f * s, ImpellerColor.FromRgb(0x32, 0x26, 0x1B));
        // Minute hand
        DrawHand(b, cx, cy, minF / 60f, r * 0.72f, 6f * s, ImpellerColor.FromRgb(0x32, 0x26, 0x1B));
        // Second hand (red, thin)
        DrawHand(b, cx, cy, secF / 60f, r * 0.80f, 2f * s, ImpellerColor.FromRgb(0xD0, 0x40, 0x40));

        // Center cap
        using (var p = ImpellerPaint.New()!)
        {
            p.SetColor(ImpellerColor.FromRgb(0x32, 0x26, 0x1B));
            b.DrawOval(new ImpellerRect((int)(cx - 8 * s), (int)(cy - 8 * s), (int)(16 * s), (int)(16 * s)), p);
            p.SetColor(ImpellerColor.FromRgb(0xD0, 0x40, 0x40));
            b.DrawOval(new ImpellerRect((int)(cx - 4 * s), (int)(cy - 4 * s), (int)(8 * s), (int)(8 * s)), p);
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

// ============================================================================
// 18. Bar Chart — animated values, axis, labels
// ============================================================================
internal sealed class BarChartScene : IGalleryScene
{
    public string Name => "Bar Chart";
    public string? Description => "Bars with rounded tops, baseline axis, value labels — animated";

    private readonly string[] _labels = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug" };
    private readonly float[] _targets = { 0.65f, 0.42f, 0.78f, 0.55f, 0.93f, 0.71f, 0.48f, 0.62f };

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x1A, 0x1D, 0x22);
        var s = e.DpiScale;
        float t = (float)e.TotalTime.TotalSeconds;

        float marginL = 80, marginR = 40, marginT = 60, marginB = 80;
        float chartW = e.PixelWidth - marginL - marginR;
        float chartH = e.PixelHeight - marginT - marginB;
        float baseY = marginT + chartH;
        int n = _targets.Length;
        float gap = 14;
        float barW = (chartW - gap * (n - 1)) / n;

        // Axes
        using (var p = ImpellerPaint.New()!)
        {
            p.SetColor(ImpellerColor.FromRgb(0x44, 0x4A, 0x55));
            p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
            p.SetStrokeWidth(1f);
            for (int g = 0; g <= 4; g++)
            {
                float y = marginT + chartH * (1 - g / 4f);
                b.DrawLine(new ImpellerPoint { X = marginL, Y = y },
                           new ImpellerPoint { X = marginL + chartW, Y = y }, p);
            }
        }

        // Bars
        for (int i = 0; i < n; i++)
        {
            float anim = MathF.Min(1f, MathF.Max(0f, t * 0.6f - i * 0.05f));
            anim = 1f - MathF.Pow(1f - anim, 3); // ease out cubic
            float val = _targets[i] * anim;
            float h = chartH * val;
            float x = marginL + i * (barW + gap);
            float y = baseY - h;

            var (r, g, bb) = SceneHelpers.HsvToRgb(i / (float)n * 0.7f + 0.55f, 0.7f, 1.0f);
            using var p = ImpellerPaint.New()!;
            p.SetColor(new ImpellerColor { Alpha = 1, Red = r, Green = g, Blue = bb });

            var radii = SceneHelpers.UniformRadii(8);
            b.DrawRoundedRect(new ImpellerRect((int)x, (int)y, (int)barW, (int)h), radii, p);

            // Value label
            if (e.Typography != null)
            {
                TextBasicsScene.DrawSimpleText(b, e.Typography, $"{(int)(_targets[i] * 100)}", 13 * s,
                    x, y - 22 * s, (int)barW,
                    ImpellerColor.FromRgb(0xE8, 0xE8, 0xE8),
                    align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
                TextBasicsScene.DrawSimpleText(b, e.Typography, _labels[i], 13 * s,
                    x, baseY + 12 * s, (int)barW,
                    ImpellerColor.FromRgb(0x9A, 0xA0, 0xAC),
                    align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
            }
        }

        if (e.Typography != null)
            TextBasicsScene.DrawSimpleText(b, e.Typography, "Monthly Activity", 22 * s,
                0, 20 * s, e.PixelWidth,
                ImpellerColor.FromRgb(0xFF, 0xFF, 0xFF),
                weight: ImpellerFontWeight.kImpellerFontWeight600,
                align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
    }
}

// ============================================================================
// 19. Pie Chart — animated slices
// ============================================================================
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
        var s = e.DpiScale;
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
                b.DrawRoundedRect(new ImpellerRect((int)legendX, (int)(legendY + i * 40 * s), (int)(20 * s), (int)(20 * s)),
                    SceneHelpers.UniformRadii(4 * s), sw);
                TextBasicsScene.DrawSimpleText(b, e.Typography,
                    $"{_data[i].label}  {_data[i].value:0}%",
                    15 * s, legendX + 32 * s, legendY + i * 40 * s + 1 * s, e.PixelWidth,
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

// ============================================================================
// 20. Loading Spinners — six different styles
// ============================================================================
internal sealed class LoadingSpinnersScene : IGalleryScene
{
    public string Name => "Loading Spinners";
    public string? Description => "Common loading-indicator patterns animated with the frame clock";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b);
        float t = (float)e.TotalTime.TotalSeconds;
        var s = e.DpiScale;

        float[] cxs = { e.PixelWidth * 0.18f, e.PixelWidth * 0.50f, e.PixelWidth * 0.82f };
        float[] cys = { e.PixelHeight * 0.30f, e.PixelHeight * 0.70f };

        // 1. Rotating arc
        DrawArcSpinner(b, cxs[0], cys[0], 50 * s, t * 240f);
        // 2. Dot ring fade
        DrawDotRing(b, cxs[1], cys[0], 50 * s, t);
        // 3. Pulsing dots
        DrawPulsingDots(b, cxs[2], cys[0], 50 * s, t);
        // 4. Bouncing bars
        DrawBouncingBars(b, cxs[0], cys[1], 50 * s, t);
        // 5. Ring trail
        DrawRingTrail(b, cxs[1], cys[1], 50 * s, t);
        // 6. Orbiting balls
        DrawOrbitingBalls(b, cxs[2], cys[1], 50 * s, t);
    }

    private static void DrawArcSpinner(ImpellerDisplayListBuilder b, float cx, float cy, float r, float angleDeg)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0x6F, 0xC2, 0xE8));
        p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        p.SetStrokeWidth(6);
        p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapRound);

        const int segs = 28;
        const float sweep = MathF.PI * 1.2f;
        float a0 = angleDeg * MathF.PI / 180;
        using var pb = ImpellerPathBuilder.New()!;
        for (int i = 0; i <= segs; i++)
        {
            float aa = a0 + sweep * i / segs;
            var pt = new ImpellerPoint { X = cx + MathF.Cos(aa) * r, Y = cy + MathF.Sin(aa) * r };
            if (i == 0) pb.MoveTo(pt); else pb.LineTo(pt);
        }
        using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
        b.DrawPath(path, p);
    }

    private static void DrawDotRing(ImpellerDisplayListBuilder b, float cx, float cy, float r, float t)
    {
        const int n = 12;
        using var p = ImpellerPaint.New()!;
        for (int i = 0; i < n; i++)
        {
            float phase = (i / (float)n - t * 0.5f);
            phase -= MathF.Floor(phase);
            float alpha = 1f - phase;
            p.SetColor(new ImpellerColor { Alpha = alpha, Red = 0.9f, Green = 0.6f, Blue = 0.4f });
            float ang = i * MathF.PI * 2 / n;
            float x = cx + MathF.Cos(ang) * r;
            float y = cy + MathF.Sin(ang) * r;
            b.DrawOval(new ImpellerRect((int)(x - 6), (int)(y - 6), 12, 12), p);
        }
    }

    private static void DrawPulsingDots(ImpellerDisplayListBuilder b, float cx, float cy, float r, float t)
    {
        const int n = 3;
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0xB8, 0xE8, 0x6F));
        for (int i = 0; i < n; i++)
        {
            float pulse = 0.5f + 0.5f * MathF.Sin(t * 4 + i * 0.8f);
            float sz = 8 + 10 * pulse;
            float x = cx - 36 + i * 36;
            b.DrawOval(new ImpellerRect((int)(x - sz / 2), (int)(cy - sz / 2), (int)sz, (int)sz), p);
        }
    }

    private static void DrawBouncingBars(ImpellerDisplayListBuilder b, float cx, float cy, float r, float t)
    {
        const int n = 5;
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0xE8, 0x70, 0xC8));
        var radii = SceneHelpers.UniformRadii(3);
        for (int i = 0; i < n; i++)
        {
            float phase = t * 6 + i * 0.4f;
            float h = (20 + 30 * (0.5f + 0.5f * MathF.Sin(phase)));
            float w = 10;
            float x = cx - (n * (w + 4)) / 2 + i * (w + 4);
            float y = cy - h / 2;
            b.DrawRoundedRect(new ImpellerRect((int)x, (int)y, (int)w, (int)h), radii, p);
        }
    }

    private static void DrawRingTrail(ImpellerDisplayListBuilder b, float cx, float cy, float r, float t)
    {
        using var p = ImpellerPaint.New()!;
        p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        p.SetStrokeWidth(5);
        p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapRound);

        // Track
        p.SetColor(new ImpellerColor { Alpha = 0.15f, Red = 1, Green = 1, Blue = 1 });
        b.DrawOval(new ImpellerRect((int)(cx - r), (int)(cy - r), (int)(r * 2), (int)(r * 2)), p);

        // Animated trail (using a path arc)
        p.SetColor(ImpellerColor.FromRgb(0xE8, 0xCB, 0x6F));
        const int segs = 28;
        float trailLen = MathF.PI * 0.6f;
        float a0 = t * 2.5f;
        using var pb = ImpellerPathBuilder.New()!;
        for (int i = 0; i <= segs; i++)
        {
            float aa = a0 + trailLen * i / segs;
            var pt = new ImpellerPoint { X = cx + MathF.Cos(aa) * r, Y = cy + MathF.Sin(aa) * r };
            if (i == 0) pb.MoveTo(pt); else pb.LineTo(pt);
        }
        using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
        b.DrawPath(path, p);
    }

    private static void DrawOrbitingBalls(ImpellerDisplayListBuilder b, float cx, float cy, float r, float t)
    {
        const int n = 3;
        using var p = ImpellerPaint.New()!;
        for (int i = 0; i < n; i++)
        {
            float ang = t * 2 + i * MathF.PI * 2 / n;
            float x = cx + MathF.Cos(ang) * r;
            float y = cy + MathF.Sin(ang) * r;
            var (cr, cg, cbb) = SceneHelpers.HsvToRgb(i / (float)n, 0.85f, 1.0f);
            p.SetColor(new ImpellerColor { Alpha = 1, Red = cr, Green = cg, Blue = cbb });
            b.DrawOval(new ImpellerRect((int)(x - 10), (int)(y - 10), 20, 20), p);
        }
    }
}

// ============================================================================
// 21. Particle Field — simple physics-driven sprinkles
// ============================================================================
internal sealed class ParticleFieldScene : IGalleryScene
{
    public string Name => "Particle Field";
    public string? Description => "100 particles with deterministic motion + color sweep";

    private const int N = 100;
    private readonly (float seed1, float seed2, float seed3)[] _seeds = new (float, float, float)[N];

    public ParticleFieldScene()
    {
        var rng = new Random(42);
        for (int i = 0; i < N; i++)
            _seeds[i] = ((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble());
    }

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x0E, 0x10, 0x15);
        float t = (float)e.TotalTime.TotalSeconds;

        using var p = ImpellerPaint.New()!;
        for (int i = 0; i < N; i++)
        {
            var (s1, s2, s3) = _seeds[i];
            float speed = 0.3f + s1 * 1.2f;
            float ang = (t * speed + s2 * MathF.PI * 2) % (MathF.PI * 2);
            float radius = 60 + s3 * MathF.Min(e.PixelWidth, e.PixelHeight) * 0.4f;
            float jitter = MathF.Sin(t * 1.5f + s1 * 10) * 30;

            float x = e.PixelWidth / 2f + MathF.Cos(ang) * (radius + jitter);
            float y = e.PixelHeight / 2f + MathF.Sin(ang) * (radius + jitter);

            float hue = (s2 + t * 0.05f) % 1f;
            var (r, g, bb) = SceneHelpers.HsvToRgb(hue, 0.8f, 1.0f);
            float alpha = 0.4f + 0.4f * MathF.Sin(t * 2 + s3 * 6);
            p.SetColor(new ImpellerColor { Alpha = alpha, Red = r, Green = g, Blue = bb });

            float sz = 4 + s3 * 8;
            b.DrawOval(new ImpellerRect((int)(x - sz / 2), (int)(y - sz / 2), (int)sz, (int)sz), p);
        }
    }
}

// ============================================================================
// 22. Spirograph — parametric curve via path
// ============================================================================
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

// ============================================================================
// 23. Card Layout — UI mockup: shadow + rounded rect + text composition
// ============================================================================
internal sealed class CardLayoutScene : IGalleryScene
{
    public string Name => "Card Layout (UI Mockup)";
    public string? Description => "Material-style cards: shadow + rounded rect + multi-line text + accent bar";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x1F, 0x22, 0x29);
        var s = e.DpiScale;

        var cards = new (string title, string body, byte r, byte g, byte bb)[]
        {
            ("Performance", "GPU-accelerated 2D vector rendering powered by Vulkan, with consistent frame pacing.", 0x6F, 0xC2, 0xE8),
            ("Cross-platform", "The Impeller engine targets Windows, macOS, Linux, iOS, and Android from one codebase.", 0xB8, 0xE8, 0x6F),
            ("Modern APIs", "Display lists, color filters, image filters, blend modes and full typography support.", 0xE8, 0xCB, 0x6F),
            ("Native interop", "Integrates with WPF via D3DImage thanks to VK_KHR_external_memory_win32.", 0xE8, 0x6F, 0xC8),
        };

        const int cols = 2;
        const int padOuter = 30;
        const int gap = 24;
        int cellW = (e.PixelWidth - padOuter * 2 - gap * (cols - 1)) / cols;
        int cellH = (e.PixelHeight - padOuter * 2 - gap) / 2;

        for (int i = 0; i < cards.Length; i++)
        {
            int col = i % cols;
            int row = i / cols;
            int x = padOuter + col * (cellW + gap);
            int y = padOuter + row * (cellH + gap);

            // Drop shadow
            using (var pb = ImpellerPathBuilder.New()!)
            {
                pb.AddRoundedRect(new ImpellerRect(x, y, cellW, cellH), SceneHelpers.UniformRadii(14));
                using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
                b.DrawShadow(path, ImpellerColor.FromRgb(0x00, 0x00, 0x00), 8f, 0, (float)e.DpiScale);
            }

            // Card background
            using (var p = ImpellerPaint.New()!)
            {
                p.SetColor(ImpellerColor.FromRgb(0x2D, 0x32, 0x3C));
                b.DrawRoundedRect(new ImpellerRect(x, y, cellW, cellH), SceneHelpers.UniformRadii(14), p);
            }

            // Accent strip (left)
            using (var p = ImpellerPaint.New()!)
            {
                p.SetColor(ImpellerColor.FromRgb(cards[i].r, cards[i].g, cards[i].bb));
                b.Save();
                b.ClipRoundedRect(new ImpellerRect(x, y, cellW, cellH), SceneHelpers.UniformRadii(14),
                    ImpellerClipOperation.kImpellerClipOperationIntersect);
                b.DrawRect(new ImpellerRect(x, y, 6, cellH), p);
                b.Restore();
            }

            if (e.Typography != null)
            {
                int tx = x + 24;
                int tw = cellW - 48;
                TextBasicsScene.DrawSimpleText(b, e.Typography, cards[i].title, 22 * s,
                    tx, y + 22 * s, tw,
                    ImpellerColor.FromRgb(cards[i].r, cards[i].g, cards[i].bb),
                    weight: ImpellerFontWeight.kImpellerFontWeight700);

                DrawWrappedBody(b, e.Typography, cards[i].body, 14 * s, tx, y + 64 * s, tw,
                    ImpellerColor.FromRgb(0xC8, 0xCD, 0xD5));
            }
        }
    }

    private static void DrawWrappedBody(ImpellerDisplayListBuilder b, ImpellerTypographyContext typography,
        string text, float fontSize, float x, float y, int width, ImpellerColor color)
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
        style.SetHeight(1.35f);
        paragraphBuilder.PushStyle(style);
        paragraphBuilder.AddText(text);
        using var paragraph = paragraphBuilder.BuildParagraphNew(width: width);
        if (paragraph == null) return;
        b.DrawParagraph(paragraph, new ImpellerPoint { X = MathF.Round(x), Y = MathF.Round(y) });
    }
}

// ============================================================================
// 24. Hex Grid — colored hexagonal tiling
// ============================================================================
internal sealed class HexGridScene : IGalleryScene
{
    public string Name => "Hex Grid";
    public string? Description => "Honeycomb hexagonal tiling with animated hue sweep";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x10, 0x12, 0x18);
        float t = (float)e.TotalTime.TotalSeconds;

        float r = 38f * e.DpiScale; // hex outer radius
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

// ============================================================================
// 25. Wave Lines — sine-wave animated lines
// ============================================================================
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

// ============================================================================
// 26. Donut Chart — animated arcs with hole
// ============================================================================
internal sealed class DonutChartScene : IGalleryScene
{
    public string Name => "Donut Chart";
    public string? Description => "Pie chart with center hole — drawn as stroked arcs";

    private readonly (string label, float value)[] _data =
    {
        ("Code",     42),
        ("Review",   18),
        ("Meetings", 22),
        ("Email",    11),
        ("Coffee",    7),
    };

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x18, 0x1C, 0x24);
        var s = e.DpiScale;
        float t = (float)e.TotalTime.TotalSeconds;

        float total = 0;
        foreach (var d in _data) total += d.value;

        float cx = e.PixelWidth * 0.35f;
        float cy = e.PixelHeight * 0.5f;
        float radius = MathF.Min(e.PixelWidth, e.PixelHeight) * 0.30f;
        float thickness = radius * 0.30f;

        float anim = MathF.Min(1f, t * 0.5f);
        anim = 1f - MathF.Pow(1f - anim, 3);

        float startAng = -MathF.PI / 2;
        for (int i = 0; i < _data.Length; i++)
        {
            float sweep = _data[i].value / total * MathF.PI * 2 * anim;
            DrawArcRing(b, cx, cy, radius, thickness, startAng, sweep, i / (float)_data.Length);
            startAng += _data[i].value / total * MathF.PI * 2;
        }

        // Center label
        if (e.Typography != null)
        {
            TextBasicsScene.DrawSimpleText(b, e.Typography, "100h",
                36 * s, cx - 100, cy - 28 * s, 200,
                ImpellerColor.FromRgb(0xFF, 0xFF, 0xFF),
                weight: ImpellerFontWeight.kImpellerFontWeight700,
                align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
            TextBasicsScene.DrawSimpleText(b, e.Typography, "this week",
                14 * s, cx - 100, cy + 18 * s, 200,
                ImpellerColor.FromRgb(0xA0, 0xA8, 0xB2),
                align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);

            // Legend
            float legendX = e.PixelWidth * 0.62f;
            float legendY = e.PixelHeight * 0.30f;
            using var sw = ImpellerPaint.New()!;
            for (int i = 0; i < _data.Length; i++)
            {
                var (rr, gg, bb) = SceneHelpers.HsvToRgb(i / (float)_data.Length, 0.7f, 1.0f);
                sw.SetColor(new ImpellerColor { Alpha = 1, Red = rr, Green = gg, Blue = bb });
                b.DrawOval(new ImpellerRect((int)legendX, (int)(legendY + i * 42 * s), (int)(20 * s), (int)(20 * s)), sw);
                TextBasicsScene.DrawSimpleText(b, e.Typography,
                    $"{_data[i].label}  —  {_data[i].value:0}h",
                    16 * s, legendX + 32 * s, legendY + i * 42 * s, e.PixelWidth,
                    ImpellerColor.FromRgb(0xE8, 0xE8, 0xE8));
            }
        }
    }

    private static void DrawArcRing(ImpellerDisplayListBuilder b, float cx, float cy, float r, float thickness, float a0, float sweep, float hue)
    {
        if (sweep <= 0) return;
        using var p = ImpellerPaint.New()!;
        var (cr, cg, cbb) = SceneHelpers.HsvToRgb(hue, 0.75f, 1.0f);
        p.SetColor(new ImpellerColor { Alpha = 1, Red = cr, Green = cg, Blue = cbb });
        p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        p.SetStrokeWidth(thickness);
        p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapButt);

        using var pb = ImpellerPathBuilder.New()!;
        const int segs = 96;
        for (int i = 0; i <= segs; i++)
        {
            float aa = a0 + sweep * i / segs;
            var pt = new ImpellerPoint { X = cx + MathF.Cos(aa) * r, Y = cy + MathF.Sin(aa) * r };
            if (i == 0) pb.MoveTo(pt); else pb.LineTo(pt);
        }
        using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
        b.DrawPath(path, p);
    }
}

// ============================================================================
// 27. Gauge / Speedometer
// ============================================================================
internal sealed class GaugeScene : IGalleryScene
{
    public string Name => "Gauge / Speedometer";
    public string? Description => "Half-circle gauge with animated needle, tick marks, value readout";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x18, 0x1C, 0x24);
        var s = e.DpiScale;
        float t = (float)e.TotalTime.TotalSeconds;

        float cx = e.PixelWidth / 2f;
        float cy = e.PixelHeight * 0.62f;
        float radius = MathF.Min(cx, e.PixelHeight * 0.42f);

        // Background arc
        DrawHalfArc(b, cx, cy, radius, 0xE8, 0xE8, 0xE8, alpha: 0.10f, thickness: 26f * s);

        // Value fill arc (animated)
        float value = 0.5f + 0.5f * MathF.Sin(t * 0.6f); // 0..1
        DrawColoredArc(b, cx, cy, radius, value, 26f * s);

        // Ticks
        using (var p = ImpellerPaint.New()!)
        {
            p.SetColor(ImpellerColor.FromRgb(0xC0, 0xC8, 0xD0));
            p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
            p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapRound);
            for (int i = 0; i <= 10; i++)
            {
                bool major = i % 2 == 0;
                p.SetStrokeWidth((major ? 3f : 1.5f) * s);
                float ang = MathF.PI + i * MathF.PI / 10;
                float r0 = radius - 38 * s;
                float r1 = r0 - (major ? 14 * s : 7 * s);
                b.DrawLine(
                    new ImpellerPoint { X = cx + MathF.Cos(ang) * r0, Y = cy + MathF.Sin(ang) * r0 },
                    new ImpellerPoint { X = cx + MathF.Cos(ang) * r1, Y = cy + MathF.Sin(ang) * r1 },
                    p);
            }
        }

        // Needle
        using (var p = ImpellerPaint.New()!)
        {
            float ang = MathF.PI + value * MathF.PI;
            p.SetColor(ImpellerColor.FromRgb(0xE8, 0x6F, 0x6F));
            p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
            p.SetStrokeWidth(5f * s);
            p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapRound);
            b.DrawLine(
                new ImpellerPoint { X = cx, Y = cy },
                new ImpellerPoint { X = cx + MathF.Cos(ang) * (radius - 50 * s), Y = cy + MathF.Sin(ang) * (radius - 50 * s) },
                p);

            p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleFill);
            p.SetColor(ImpellerColor.FromRgb(0x2D, 0x32, 0x3C));
            b.DrawOval(new ImpellerRect((int)(cx - 12 * s), (int)(cy - 12 * s), (int)(24 * s), (int)(24 * s)), p);
        }

        // Value readout
        if (e.Typography != null)
        {
            int pct = (int)(value * 100);
            TextBasicsScene.DrawSimpleText(b, e.Typography, $"{pct}%",
                40 * s, cx - 100, cy + 30 * s, 200,
                ImpellerColor.FromRgb(0xF0, 0xF0, 0xF0),
                weight: ImpellerFontWeight.kImpellerFontWeight700,
                align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
        }
    }

    private static void DrawHalfArc(ImpellerDisplayListBuilder b, float cx, float cy, float r, byte rc, byte gc, byte bc, float alpha, float thickness)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(new ImpellerColor { Alpha = alpha, Red = rc / 255f, Green = gc / 255f, Blue = bc / 255f });
        p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        p.SetStrokeWidth(thickness);
        p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapRound);
        using var pb = ImpellerPathBuilder.New()!;
        const int segs = 64;
        for (int i = 0; i <= segs; i++)
        {
            float ang = MathF.PI + i * MathF.PI / segs;
            var pt = new ImpellerPoint { X = cx + MathF.Cos(ang) * r, Y = cy + MathF.Sin(ang) * r };
            if (i == 0) pb.MoveTo(pt); else pb.LineTo(pt);
        }
        using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
        b.DrawPath(path, p);
    }

    private static void DrawColoredArc(ImpellerDisplayListBuilder b, float cx, float cy, float r, float fill01, float thickness)
    {
        using var p = ImpellerPaint.New()!;
        // Color goes green -> yellow -> red as fill grows
        var (cr, cg, cbb) = SceneHelpers.HsvToRgb((1f - fill01) * 0.33f, 0.85f, 1.0f);
        p.SetColor(new ImpellerColor { Alpha = 1, Red = cr, Green = cg, Blue = cbb });
        p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        p.SetStrokeWidth(thickness);
        p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapRound);
        using var pb = ImpellerPathBuilder.New()!;
        const int segs = 96;
        int n = (int)MathF.Max(1, segs * fill01);
        for (int i = 0; i <= n; i++)
        {
            float ang = MathF.PI + i * MathF.PI / segs;
            var pt = new ImpellerPoint { X = cx + MathF.Cos(ang) * r, Y = cy + MathF.Sin(ang) * r };
            if (i == 0) pb.MoveTo(pt); else pb.LineTo(pt);
        }
        using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
        b.DrawPath(path, p);
    }
}

// ============================================================================
// 28. Sparkline — small trend chart with area + line
// ============================================================================
internal sealed class SparklineScene : IGalleryScene
{
    public string Name => "Sparkline";
    public string? Description => "Six sparkline cards with filled area + trend line";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x1A, 0x1D, 0x22);
        var s = e.DpiScale;
        float t = (float)e.TotalTime.TotalSeconds;

        const int cols = 3;
        const int rows = 2;
        const int padOuter = 30;
        const int gap = 16;
        int cellW = (e.PixelWidth - padOuter * 2 - gap * (cols - 1)) / cols;
        int cellH = (e.PixelHeight - padOuter * 2 - gap * (rows - 1)) / rows;

        var titles = new[] { "Latency", "Throughput", "Errors", "Memory", "CPU", "Disk I/O" };

        for (int i = 0; i < 6; i++)
        {
            int col = i % cols;
            int row = i / cols;
            int x = padOuter + col * (cellW + gap);
            int y = padOuter + row * (cellH + gap);

            // Card background
            using (var p = ImpellerPaint.New()!)
            {
                p.SetColor(ImpellerColor.FromRgb(0x2A, 0x2F, 0x38));
                b.DrawRoundedRect(new ImpellerRect(x, y, cellW, cellH), SceneHelpers.UniformRadii(10), p);
            }

            // Title
            if (e.Typography != null)
            {
                TextBasicsScene.DrawSimpleText(b, e.Typography, titles[i], 14 * s,
                    x + 16, y + 14, cellW - 32,
                    ImpellerColor.FromRgb(0xA0, 0xA8, 0xB2),
                    weight: ImpellerFontWeight.kImpellerFontWeight500);
            }

            // Big number
            float value = 35 + 60 * (0.5f + 0.5f * MathF.Sin(t * 0.4f + i * 0.7f));
            if (e.Typography != null)
            {
                TextBasicsScene.DrawSimpleText(b, e.Typography, $"{value:0.0}", 28 * s,
                    x + 16, y + 38 * s, cellW - 32,
                    ImpellerColor.FromRgb(0xFF, 0xFF, 0xFF),
                    weight: ImpellerFontWeight.kImpellerFontWeight700);
            }

            // Sparkline (samples drawn as path)
            const int samples = 40;
            float chartTop = y + cellH * 0.55f;
            float chartH = cellH * 0.40f;
            float chartW = cellW - 32;

            var (cr, cg, cbb) = SceneHelpers.HsvToRgb(i / 6f, 0.7f, 1.0f);

            // Filled area
            using (var pb = ImpellerPathBuilder.New()!)
            {
                pb.MoveTo(new ImpellerPoint { X = x + 16, Y = chartTop + chartH });
                for (int j = 0; j <= samples; j++)
                {
                    float px = x + 16 + j * chartW / samples;
                    float v = 0.5f + 0.5f * MathF.Sin(j * 0.4f + t * 1.5f + i * 0.6f);
                    float py = chartTop + chartH - v * chartH;
                    pb.LineTo(new ImpellerPoint { X = px, Y = py });
                }
                pb.LineTo(new ImpellerPoint { X = x + 16 + chartW, Y = chartTop + chartH });
                pb.Close();
                using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
                using var p = ImpellerPaint.New()!;
                p.SetColor(new ImpellerColor { Alpha = 0.25f, Red = cr, Green = cg, Blue = cbb });
                b.DrawPath(path, p);
            }
            // Stroke line
            using (var pb = ImpellerPathBuilder.New()!)
            {
                for (int j = 0; j <= samples; j++)
                {
                    float px = x + 16 + j * chartW / samples;
                    float v = 0.5f + 0.5f * MathF.Sin(j * 0.4f + t * 1.5f + i * 0.6f);
                    float py = chartTop + chartH - v * chartH;
                    var pt = new ImpellerPoint { X = px, Y = py };
                    if (j == 0) pb.MoveTo(pt); else pb.LineTo(pt);
                }
                using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
                using var p = ImpellerPaint.New()!;
                p.SetColor(new ImpellerColor { Alpha = 1f, Red = cr, Green = cg, Blue = cbb });
                p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
                p.SetStrokeWidth(2f * s);
                p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapRound);
                b.DrawPath(path, p);
            }
        }
    }
}

// ============================================================================
// [StressTest] series — push base APIs to find frame-time ceilings.
// Each scene draws thousands of one kind of primitive using a deterministic PRNG.
// ============================================================================

internal static class StressHelpers
{
    public static Random Seeded(int seed) => new Random(seed);

    public static void DrawCountOverlay(ImpellerRenderEventArgs e, string label, int count)
    {
        if (e.Typography == null) return;
        TextBasicsScene.DrawSimpleText(e.Builder, e.Typography,
            $"{label}: {count:N0}  •  frame {e.FrameNumber}",
            16 * e.DpiScale, 12, 12, e.PixelWidth,
            ImpellerColor.FromRgb(0xFF, 0xFF, 0xFF),
            weight: ImpellerFontWeight.kImpellerFontWeight600);
    }
}

/// <summary>
/// Base class for [StressTest] scenes — implements <see cref="IConfigurableScene"/>
/// so the main window can render +/- buttons to grow/shrink the item count live.
/// </summary>
internal abstract class StressTestSceneBase : IConfigurableScene
{
    public abstract string Name { get; }
    public abstract string? Description { get; }

    private int _itemCount;
    public int ItemCount
    {
        get => _itemCount;
        set => _itemCount = Math.Clamp(value, ItemMin, ItemMax);
    }

    public int ItemStep { get; }
    public int ItemMin { get; }
    public int ItemMax { get; }
    public virtual string ItemLabel => "items";

    protected StressTestSceneBase(int initial, int step, int min, int max)
    {
        ItemStep = step;
        ItemMin = min;
        ItemMax = max;
        _itemCount = Math.Clamp(initial, min, max);
    }

    public abstract void Render(ImpellerRenderEventArgs e);
}

// ----------------------------------------------------------------------------
// [StressTest] 10000 Rects
// ----------------------------------------------------------------------------
internal sealed class StressTestRectsScene : StressTestSceneBase
{
    public override string Name => "[StressTest] Rects";
    public override string? Description => "Draw N small filled rectangles at random positions per frame";
    public override string ItemLabel => "rects";

    public StressTestRectsScene() : base(initial: 10000, step: 1000, min: 0, max: 200000) { }

    public override void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x0E, 0x10, 0x14);

        var rng = StressHelpers.Seeded(1);
        float t = (float)e.TotalTime.TotalSeconds;
        int count = ItemCount;

        using var p = ImpellerPaint.New()!;
        for (int i = 0; i < count; i++)
        {
            int x = rng.Next(e.PixelWidth);
            int y = rng.Next(e.PixelHeight);
            int sz = 4 + rng.Next(8);

            // Slowly drift hue to make it visually obvious frames advance
            float hue = (i * 0.001f + t * 0.05f) % 1f;
            var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.7f, 1.0f);
            p.SetColor(new ImpellerColor { Alpha = 0.8f, Red = rr, Green = gg, Blue = bb });
            b.DrawRect(new ImpellerRect(x, y, sz, sz), p);
        }

        StressHelpers.DrawCountOverlay(e, "Rects", count);
    }
}

// ----------------------------------------------------------------------------
// [StressTest] 10000 Circles
// ----------------------------------------------------------------------------
internal sealed class StressTestCirclesScene : StressTestSceneBase
{
    public override string Name => "[StressTest] Circles";
    public override string? Description => "Draw N small filled ovals per frame";
    public override string ItemLabel => "circles";

    public StressTestCirclesScene() : base(initial: 10000, step: 1000, min: 0, max: 200000) { }

    public override void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x0E, 0x10, 0x14);

        var rng = StressHelpers.Seeded(2);
        float t = (float)e.TotalTime.TotalSeconds;
        int count = ItemCount;

        using var p = ImpellerPaint.New()!;
        for (int i = 0; i < count; i++)
        {
            int x = rng.Next(e.PixelWidth);
            int y = rng.Next(e.PixelHeight);
            int sz = 4 + rng.Next(10);

            float hue = (i * 0.0007f + t * 0.05f) % 1f;
            var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.7f, 1.0f);
            p.SetColor(new ImpellerColor { Alpha = 0.7f, Red = rr, Green = gg, Blue = bb });
            b.DrawOval(new ImpellerRect(x, y, sz, sz), p);
        }

        StressHelpers.DrawCountOverlay(e, "Circles", count);
    }
}

// ----------------------------------------------------------------------------
// [StressTest] 5000 Rounded Rects
// ----------------------------------------------------------------------------
internal sealed class StressTestRoundedRectsScene : StressTestSceneBase
{
    public override string Name => "[StressTest] Rounded Rects";
    public override string? Description => "Draw N rounded rectangles — heavier than plain rects";
    public override string ItemLabel => "rounded rects";

    public StressTestRoundedRectsScene() : base(initial: 5000, step: 500, min: 0, max: 100000) { }

    public override void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x0E, 0x10, 0x14);

        var rng = StressHelpers.Seeded(3);
        float t = (float)e.TotalTime.TotalSeconds;
        var radii = SceneHelpers.UniformRadii(4);
        int count = ItemCount;

        using var p = ImpellerPaint.New()!;
        for (int i = 0; i < count; i++)
        {
            int x = rng.Next(e.PixelWidth);
            int y = rng.Next(e.PixelHeight);
            int w = 8 + rng.Next(20);
            int h = 8 + rng.Next(20);

            float hue = (i * 0.001f + t * 0.05f) % 1f;
            var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.75f, 1.0f);
            p.SetColor(new ImpellerColor { Alpha = 0.85f, Red = rr, Green = gg, Blue = bb });
            b.DrawRoundedRect(new ImpellerRect(x, y, w, h), radii, p);
        }

        StressHelpers.DrawCountOverlay(e, "Rounded Rects", count);
    }
}

// ----------------------------------------------------------------------------
// [StressTest] 5000 Lines
// ----------------------------------------------------------------------------
internal sealed class StressTestLinesScene : StressTestSceneBase
{
    public override string Name => "[StressTest] Lines";
    public override string? Description => "Draw N stroked lines (round cap)";
    public override string ItemLabel => "lines";

    public StressTestLinesScene() : base(initial: 5000, step: 500, min: 0, max: 100000) { }

    public override void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x0E, 0x10, 0x14);

        var rng = StressHelpers.Seeded(4);
        float t = (float)e.TotalTime.TotalSeconds;
        int count = ItemCount;

        using var p = ImpellerPaint.New()!;
        p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapRound);
        p.SetStrokeWidth(2f);

        for (int i = 0; i < count; i++)
        {
            float x0 = rng.Next(e.PixelWidth);
            float y0 = rng.Next(e.PixelHeight);
            float x1 = x0 + (float)(rng.NextDouble() - 0.5) * 80;
            float y1 = y0 + (float)(rng.NextDouble() - 0.5) * 80;

            float hue = (i * 0.0008f + t * 0.05f) % 1f;
            var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.7f, 1.0f);
            p.SetColor(new ImpellerColor { Alpha = 0.7f, Red = rr, Green = gg, Blue = bb });
            b.DrawLine(new ImpellerPoint { X = x0, Y = y0 }, new ImpellerPoint { X = x1, Y = y1 }, p);
        }

        StressHelpers.DrawCountOverlay(e, "Lines", count);
    }
}

// ----------------------------------------------------------------------------
// [StressTest] 1000 Paths (cubic curves)
// ----------------------------------------------------------------------------
internal sealed class StressTestPathsScene : StressTestSceneBase
{
    public override string Name => "[StressTest] Cubic Paths";
    public override string? Description => "Build + draw N cubic-curve paths per frame (path tessellation pressure)";
    public override string ItemLabel => "paths";

    public StressTestPathsScene() : base(initial: 1000, step: 200, min: 0, max: 40000) { }

    public override void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x0E, 0x10, 0x14);

        var rng = StressHelpers.Seeded(5);
        float t = (float)e.TotalTime.TotalSeconds;
        int count = ItemCount;

        using var p = ImpellerPaint.New()!;
        p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        p.SetStrokeWidth(1.5f);

        for (int i = 0; i < count; i++)
        {
            float cx = rng.Next(e.PixelWidth);
            float cy = rng.Next(e.PixelHeight);
            float r = 6 + rng.Next(20);

            using var pb = ImpellerPathBuilder.New()!;
            pb.MoveTo(new ImpellerPoint { X = cx - r, Y = cy });
            pb.CubicCurveTo(
                new ImpellerPoint { X = cx - r, Y = cy - r },
                new ImpellerPoint { X = cx + r, Y = cy - r },
                new ImpellerPoint { X = cx + r, Y = cy });
            pb.CubicCurveTo(
                new ImpellerPoint { X = cx + r, Y = cy + r },
                new ImpellerPoint { X = cx - r, Y = cy + r },
                new ImpellerPoint { X = cx - r, Y = cy });
            pb.Close();

            using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
            float hue = (i * 0.003f + t * 0.05f) % 1f;
            var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.7f, 1.0f);
            p.SetColor(new ImpellerColor { Alpha = 0.7f, Red = rr, Green = gg, Blue = bb });
            b.DrawPath(path, p);
        }

        StressHelpers.DrawCountOverlay(e, "Cubic Paths", count);
    }
}

// ----------------------------------------------------------------------------
// [StressTest] 500 Text Paragraphs
// ----------------------------------------------------------------------------
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

// ----------------------------------------------------------------------------
// [StressTest] 5000 Transforms (Save/Translate/Rotate/Draw/Restore)
// ----------------------------------------------------------------------------
internal sealed class StressTestTransformsScene : StressTestSceneBase
{
    public override string Name => "[StressTest] Transforms";
    public override string? Description => "Save + Translate + Rotate + DrawRect + Restore, N times per frame";
    public override string ItemLabel => "transforms";

    public StressTestTransformsScene() : base(initial: 5000, step: 500, min: 0, max: 100000) { }

    public override void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x0E, 0x10, 0x14);

        var rng = StressHelpers.Seeded(7);
        float t = (float)e.TotalTime.TotalSeconds;
        int count = ItemCount;

        using var p = ImpellerPaint.New()!;
        for (int i = 0; i < count; i++)
        {
            float x = rng.Next(e.PixelWidth);
            float y = rng.Next(e.PixelHeight);
            float angle = (t * 60 + i * 7) % 360;

            float hue = (i * 0.001f + t * 0.05f) % 1f;
            var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.7f, 1.0f);
            p.SetColor(new ImpellerColor { Alpha = 0.7f, Red = rr, Green = gg, Blue = bb });

            b.Save();
            b.Translate(x, y);
            b.Rotate(angle);
            b.DrawRect(new ImpellerRect(-6, -6, 12, 12), p);
            b.Restore();
        }

        StressHelpers.DrawCountOverlay(e, "Transformed rects", count);
    }
}

// ----------------------------------------------------------------------------
// [StressTest] 200 Blurred Shapes (mask filter)
// ----------------------------------------------------------------------------
internal sealed class StressTestBlurScene : StressTestSceneBase
{
    public override string Name => "[StressTest] Blurred Shapes";
    public override string? Description => "N ovals with ImpellerMaskFilter blur — bandwidth-bound, very heavy";
    public override string ItemLabel => "blurred ovals";

    public StressTestBlurScene() : base(initial: 200, step: 50, min: 0, max: 5000) { }

    public override void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x0E, 0x10, 0x14);

        var rng = StressHelpers.Seeded(8);
        float t = (float)e.TotalTime.TotalSeconds;
        int count = ItemCount;

        for (int i = 0; i < count; i++)
        {
            int x = rng.Next(e.PixelWidth);
            int y = rng.Next(e.PixelHeight);
            int sz = 30 + rng.Next(40);
            float sigma = 4 + rng.Next(10);

            float hue = (i * 0.005f + t * 0.05f) % 1f;
            var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.8f, 1.0f);

            using var mask = ImpellerMaskFilter.CreateBlurNew(ImpellerBlurStyle.kImpellerBlurStyleNormal, sigma)!;
            using var p = ImpellerPaint.New()!;
            p.SetColor(new ImpellerColor { Alpha = 0.85f, Red = rr, Green = gg, Blue = bb });
            p.SetMaskFilter(mask);
            b.DrawOval(new ImpellerRect(x, y, sz, sz), p);
        }

        StressHelpers.DrawCountOverlay(e, "Blurred shapes", count);
    }
}

// ----------------------------------------------------------------------------
// [StressTest] 200 Shadowed Cards (DrawShadow)
// ----------------------------------------------------------------------------
internal sealed class StressTestShadowsScene : StressTestSceneBase
{
    public override string Name => "[StressTest] Shadows";
    public override string? Description => "N DrawShadow calls + filled cards — typical UI card grid at scale";
    public override string ItemLabel => "shadows";

    public StressTestShadowsScene() : base(initial: 200, step: 50, min: 0, max: 5000) { }

    public override void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x18, 0x1C, 0x24);

        var rng = StressHelpers.Seeded(9);
        var shadowColor = ImpellerColor.FromRgb(0x00, 0x00, 0x00);
        var radii = SceneHelpers.UniformRadii(8);
        int count = ItemCount;

        using var fill = ImpellerPaint.New()!;
        for (int i = 0; i < count; i++)
        {
            int x = rng.Next(e.PixelWidth - 80);
            int y = rng.Next(e.PixelHeight - 60);
            int w = 40 + rng.Next(60);
            int h = 30 + rng.Next(40);
            float elev = 2 + rng.Next(10);

            using (var pb = ImpellerPathBuilder.New()!)
            {
                pb.AddRoundedRect(new ImpellerRect(x, y, w, h), radii);
                using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
                b.DrawShadow(path, shadowColor, elev, 0, (float)e.DpiScale);
            }

            byte r = (byte)(160 + rng.Next(96));
            byte g = (byte)(160 + rng.Next(96));
            byte bb = (byte)(160 + rng.Next(96));
            fill.SetColor(ImpellerColor.FromRgb(r, g, bb));
            b.DrawRoundedRect(new ImpellerRect(x, y, w, h), radii, fill);
        }

        StressHelpers.DrawCountOverlay(e, "Shadowed cards", count);
    }
}

// ----------------------------------------------------------------------------
// [StressTest] 100 SaveLayers
// ----------------------------------------------------------------------------
internal sealed class StressTestSaveLayersScene : StressTestSceneBase
{
    public override string Name => "[StressTest] SaveLayers";
    public override string? Description => "N SaveLayer + child draws — offscreen allocation pressure";
    public override string ItemLabel => "layers";

    public StressTestSaveLayersScene() : base(initial: 100, step: 25, min: 0, max: 2000) { }

    public override void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x0E, 0x10, 0x14);

        var rng = StressHelpers.Seeded(10);
        float t = (float)e.TotalTime.TotalSeconds;
        int count = ItemCount;

        for (int i = 0; i < count; i++)
        {
            int x = rng.Next(e.PixelWidth - 120);
            int y = rng.Next(e.PixelHeight - 120);
            int sz = 60 + rng.Next(60);

            using var layerPaint = ImpellerPaint.New()!;
            layerPaint.SetColor(new ImpellerColor { Alpha = 0.6f, Red = 1, Green = 1, Blue = 1 });
            using var noBackdrop = ImpellerImageFilter.CreateBlurNew(0f, 0f, ImpellerTileMode.kImpellerTileModeClamp)!;
            b.SaveLayer(new ImpellerRect(x, y, sz, sz), layerPaint, noBackdrop);

            using var inner = ImpellerPaint.New()!;
            float hue = (i * 0.01f + t * 0.05f) % 1f;
            var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.8f, 1.0f);
            inner.SetColor(new ImpellerColor { Alpha = 1, Red = rr, Green = gg, Blue = bb });
            b.DrawOval(new ImpellerRect(x, y, sz, sz), inner);
            inner.SetColor(new ImpellerColor { Alpha = 1, Red = 1 - rr, Green = 1 - gg, Blue = 1 - bb });
            b.DrawOval(new ImpellerRect(x + sz / 3, y + sz / 3, sz / 2, sz / 2), inner);

            b.Restore();
        }

        StressHelpers.DrawCountOverlay(e, "SaveLayers", count);
    }
}

// ----------------------------------------------------------------------------
// [StressTest] Mixed Pipeline — rects + text + shadows + blurs all at once
// ----------------------------------------------------------------------------
internal sealed class StressTestMixedPipelineScene : StressTestSceneBase
{
    public override string Name => "[StressTest] Mixed Pipeline";
    public override string? Description => "Mixed: N×2 rects + N×0.2 paths + N×0.05 shadows + N×0.03 blurs + N×0.08 text labels";
    public override string ItemLabel => "× scale (base 1000)";

    public StressTestMixedPipelineScene() : base(initial: 1000, step: 250, min: 0, max: 20000) { }

    public override void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x10, 0x12, 0x18);

        var rng = StressHelpers.Seeded(11);
        float t = (float)e.TotalTime.TotalSeconds;
        var radii = SceneHelpers.UniformRadii(4);

        int n = ItemCount;
        int nRects   = n * 2;
        int nPaths   = n / 5;
        int nShadows = n / 20;
        int nBlurs   = Math.Max(1, n / 33);
        int nText    = n / 12;

        // Rects
        using (var p = ImpellerPaint.New()!)
        {
            for (int i = 0; i < nRects; i++)
            {
                int x = rng.Next(e.PixelWidth);
                int y = rng.Next(e.PixelHeight);
                int sz = 4 + rng.Next(8);
                float hue = (i * 0.001f + t * 0.05f) % 1f;
                var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.7f, 1.0f);
                p.SetColor(new ImpellerColor { Alpha = 0.6f, Red = rr, Green = gg, Blue = bb });
                b.DrawRect(new ImpellerRect(x, y, sz, sz), p);
            }
        }

        // Cubic paths
        using (var p = ImpellerPaint.New()!)
        {
            p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
            p.SetStrokeWidth(2f);
            for (int i = 0; i < nPaths; i++)
            {
                float cx = rng.Next(e.PixelWidth);
                float cy = rng.Next(e.PixelHeight);
                float r = 10 + rng.Next(20);
                using var pb = ImpellerPathBuilder.New()!;
                pb.MoveTo(new ImpellerPoint { X = cx - r, Y = cy });
                pb.QuadraticCurveTo(new ImpellerPoint { X = cx, Y = cy - r * 2 },
                                    new ImpellerPoint { X = cx + r, Y = cy });
                using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
                float hue = (i * 0.005f + t * 0.05f) % 1f;
                var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.8f, 1.0f);
                p.SetColor(new ImpellerColor { Alpha = 0.85f, Red = rr, Green = gg, Blue = bb });
                b.DrawPath(path, p);
            }
        }

        // Shadows
        var shadowColor = ImpellerColor.FromRgb(0x00, 0x00, 0x00);
        using (var fill = ImpellerPaint.New()!)
        {
            for (int i = 0; i < nShadows; i++)
            {
                int x = rng.Next(e.PixelWidth - 80);
                int y = rng.Next(e.PixelHeight - 60);
                int w = 50 + rng.Next(40);
                int h = 30 + rng.Next(30);
                using (var pb = ImpellerPathBuilder.New()!)
                {
                    pb.AddRoundedRect(new ImpellerRect(x, y, w, h), radii);
                    using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
                    b.DrawShadow(path, shadowColor, 6f, 0, (float)e.DpiScale);
                }
                fill.SetColor(ImpellerColor.FromRgb(0xE8, 0xE8, 0xF0));
                b.DrawRoundedRect(new ImpellerRect(x, y, w, h), radii, fill);
            }
        }

        // Blurred ovals
        for (int i = 0; i < nBlurs; i++)
        {
            int x = rng.Next(e.PixelWidth);
            int y = rng.Next(e.PixelHeight);
            int sz = 40 + rng.Next(50);
            using var mask = ImpellerMaskFilter.CreateBlurNew(ImpellerBlurStyle.kImpellerBlurStyleNormal, 8f)!;
            using var p = ImpellerPaint.New()!;
            float hue = (i * 0.03f + t * 0.05f) % 1f;
            var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.8f, 1.0f);
            p.SetColor(new ImpellerColor { Alpha = 0.4f, Red = rr, Green = gg, Blue = bb });
            p.SetMaskFilter(mask);
            b.DrawOval(new ImpellerRect(x, y, sz, sz), p);
        }

        // Text labels
        if (e.Typography != null)
        {
            for (int i = 0; i < nText; i++)
            {
                int x = rng.Next(e.PixelWidth - 80);
                int y = rng.Next(e.PixelHeight - 20);
                TextBasicsScene.DrawSimpleText(b, e.Typography, $"#{i:000}",
                    12, x, y, 80, ImpellerColor.FromRgb(0xFF, 0xFF, 0xFF));
            }
        }

        int total = nRects + nPaths + nShadows + nBlurs + nText;
        StressHelpers.DrawCountOverlay(e, $"Mixed ({nRects}r+{nPaths}p+{nShadows}s+{nBlurs}b+{nText}t)", total);
    }
}
