using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
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
