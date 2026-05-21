using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;

internal sealed class DrawTextureScene : IGalleryScene, IDisposable
{
    private TextureSceneImage? _image;

    public string Name => "DrawTexture";
    public string? Description => "Draws tex.png at explicit points with transforms and sampling modes";

    public void Render(ImpellerRenderEventArgs e)
    {
        TextureSceneHelpers.Clear(e.Builder);
        TextureSceneHelpers.DrawTitle(e, "DrawTexture", "Draw the full texture at points; transforms control placement, scale, and rotation.");

        if (_image == null && e.Context != null)
            _image = TextureSceneHelpers.LoadTexture(e.Context);

        if (_image == null)
        {
            TextureSceneHelpers.DrawMissingTexture(e);
            return;
        }

        using var paint = ImpellerPaint.New()!;
        paint.SetColor(ImpellerColor.FromArgb(255, 255, 255, 255));

        var scale = 160f / Math.Max(_image.Width, _image.Height);
        DrawAt(e, paint, 120, 135, scale, 0, ImpellerTextureSampling.kImpellerTextureSamplingNearestNeighbor);
        DrawAt(e, paint, 390, 135, scale, 0, ImpellerTextureSampling.kImpellerTextureSamplingLinear);
        DrawAt(e, paint, 660, 135, scale, (float)e.TotalTime.TotalSeconds * 24f, ImpellerTextureSampling.kImpellerTextureSamplingLinear);

        for (var i = 0; i < 6; i++)
        {
            var x = 110 + i * 120;
            var y = 415 + MathF.Sin((float)e.TotalTime.TotalSeconds * 2.0f + i) * 22;
            DrawAt(e, paint, x, y, scale * 0.55f, i * 9f, ImpellerTextureSampling.kImpellerTextureSamplingLinear);
        }
    }

    private void DrawAt(
        ImpellerRenderEventArgs e,
        ImpellerPaint paint,
        float x,
        float y,
        float scale,
        float rotation,
        ImpellerTextureSampling sampling)
    {
        if (_image == null) return;

        e.Builder.Save();
        e.Builder.Translate(x, y);
        e.Builder.Rotate(rotation);
        e.Builder.Scale(scale, scale);
        e.Builder.DrawTexture(
            _image.Texture,
            new ImpellerPoint { X = -_image.Width * 0.5f, Y = -_image.Height * 0.5f },
            sampling,
            paint);
        e.Builder.Restore();
    }

    public void Dispose()
    {
        _image?.Dispose();
        _image = null;
    }
}
