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
        new ManualInvalidateScene(),
        new AirspaceTestScene(),
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

internal static class StressHelpers
{
    public static Random Seeded(int seed) => new Random(seed);

    public static void DrawCountOverlay(ImpellerRenderEventArgs e, string label, int count)
    {
        if (e.Typography == null) return;
        TextBasicsScene.DrawSimpleText(e.Builder, e.Typography,
            $"{label}: {count:N0}  •  frame {e.FrameNumber}",
            16 * e.DpiScaleX, 12, 12, e.PixelWidth,
            ImpellerColor.FromRgb(0xFF, 0xFF, 0xFF),
            weight: ImpellerFontWeight.kImpellerFontWeight600);
    }
}

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
