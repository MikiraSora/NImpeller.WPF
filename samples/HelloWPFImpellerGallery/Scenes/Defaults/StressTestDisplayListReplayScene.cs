using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;

internal sealed class StressTestDisplayListReplayScene : StressTestSceneBase, IDisposable
{
    private const int ReplayCount = 1;

    private ImpellerDisplayList? _cachedList;
    private int _cachedItemCount;
    private int _cachedWidth;
    private int _cachedHeight;

    public override string Name => "[StressTest] DisplayList Replay";
    public override string? Description => "Build one huge random-square display list, then replay it many times per frame";
    public override string ItemLabel => "cached squares";

    public StressTestDisplayListReplayScene() : base(initial: 20000, step: 5000, min: 0, max: 50000) { }

    public override void Render(ImpellerRenderEventArgs e)
    {
        SceneHelpers.ClearBg(e.Builder, 0x0B, 0x0D, 0x12);

        var list = GetOrCreateDisplayList(e);
        if (list == null)
        {
            StressHelpers.DrawCountOverlay(e, "DisplayList replay unavailable", ItemCount);
            return;
        }

        var t = (float)e.TotalTime.TotalSeconds;
        var width = Math.Max(1, e.PixelWidth);
        var height = Math.Max(1, e.PixelHeight);

        for (var i = 0; i < ReplayCount; i++)
        {
            var phase = i / (float)ReplayCount;
            var dx = MathF.Sin(t * 0.73f + i * 0.41f) * 18f;
            var dy = MathF.Cos(t * 0.61f + i * 0.37f) * 18f;
            var scale = 0.96f + phase * 0.08f;

            e.Builder.Save();
            e.Builder.Translate(width * 0.5f, height * 0.5f);
            e.Builder.Rotate(t * 4f + i * 360f / ReplayCount);
            e.Builder.Scale(scale, scale);
            e.Builder.Translate(-width * 0.5f + dx, -height * 0.5f + dy);
            e.Builder.DrawDisplayList(list, 1.0f);
            e.Builder.Restore();
        }

        StressHelpers.DrawCountOverlay(e, $"DisplayList replay x{ReplayCount}", ItemCount * ReplayCount);
    }

    public void Dispose()
    {
        _cachedList?.Dispose();
        _cachedList = null;
        _cachedItemCount = 0;
        _cachedWidth = 0;
        _cachedHeight = 0;
    }

    private ImpellerDisplayList? GetOrCreateDisplayList(ImpellerRenderEventArgs e)
    {
        if (_cachedList != null &&
            _cachedItemCount == ItemCount &&
            _cachedWidth == e.PixelWidth &&
            _cachedHeight == e.PixelHeight)
        {
            return _cachedList;
        }

        _cachedList?.Dispose();
        _cachedList = null;
        _cachedItemCount = ItemCount;
        _cachedWidth = e.PixelWidth;
        _cachedHeight = e.PixelHeight;

        using var builder = ImpellerDisplayListBuilder.New(new ImpellerRect(0, 0, e.PixelWidth, e.PixelHeight));
        if (builder == null) return null;

        DrawRandomSquares(builder, e.PixelWidth, e.PixelHeight, ItemCount);
        _cachedList = builder.CreateDisplayListNew();
        return _cachedList;
    }

    private static void DrawRandomSquares(ImpellerDisplayListBuilder builder, int width, int height, int count)
    {
        if (count <= 0) return;

        var rng = StressHelpers.Seeded(29);
        var maxX = Math.Max(1, width);
        var maxY = Math.Max(1, height);

        using var paint = ImpellerPaint.New()!;
        for (var i = 0; i < count; i++)
        {
            var size = 2 + rng.Next(10);
            var x = rng.Next(maxX);
            var y = rng.Next(maxY);
            var hue = (i * 0.00037f + rng.NextSingle() * 0.12f) % 1f;
            var (r, g, b) = SceneHelpers.HsvToRgb(hue, 0.85f, 1.0f);

            paint.SetColor(new ImpellerColor
            {
                Alpha = 0.82f,
                Red = r,
                Green = g,
                Blue = b,
            });
            builder.DrawRect(new ImpellerRect(x, y, size, size), paint);
        }
    }
}
