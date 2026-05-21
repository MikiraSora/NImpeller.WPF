using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;

internal sealed class DrawTextureRectScene : IGalleryScene, IDisposable
{
    private TextureSceneImage? _image;

    public string Name => "DrawTextureRect";
    public string? Description => "Samples tex.png into destination rectangles, including cropped source regions";

    public void Render(ImpellerRenderEventArgs e)
    {
        TextureSceneHelpers.Clear(e.Builder);
        TextureSceneHelpers.DrawTitle(e, "DrawTextureRect", "Use src/dst rectangles to scale, crop, and letterbox Resources/tex.png.");

        if (_image == null && e.Context != null)
            _image = TextureSceneHelpers.LoadTexture(e.Context);

        if (_image == null)
        {
            TextureSceneHelpers.DrawMissingTexture(e);
            return;
        }

        using var paint = ImpellerPaint.New()!;
        paint.SetColor(ImpellerColor.FromArgb(255, 255, 255, 255));

        var fullSrc = new ImpellerRect(0, 0, _image.Width, _image.Height);
        var leftDst = new ImpellerRect(60, 120, 300, 300);
        e.Builder.DrawTextureRect(_image.Texture, fullSrc, leftDst, ImpellerTextureSampling.kImpellerTextureSamplingLinear, paint);
        TextureSceneHelpers.DrawFrame(e.Builder, leftDst, ImpellerColor.FromRgb(0x8B, 0xD6, 0xF7));

        var cropSize = Math.Min(_image.Width, _image.Height);
        var cropSrc = new ImpellerRect((_image.Width - cropSize) / 2, (_image.Height - cropSize) / 2, cropSize, cropSize);
        var cropDst = new ImpellerRect(410, 120, 240, 240);
        e.Builder.DrawTextureRect(_image.Texture, cropSrc, cropDst, ImpellerTextureSampling.kImpellerTextureSamplingNearestNeighbor, paint);
        TextureSceneHelpers.DrawFrame(e.Builder, cropDst, ImpellerColor.FromRgb(0xF4, 0xC4, 0x67));

        var stripSrc = new ImpellerRect(0, _image.Height / 3, _image.Width, _image.Height / 3);
        var stripDst = new ImpellerRect(60, 470, Math.Min(680, e.PixelWidth - 120), 120);
        e.Builder.DrawTextureRect(_image.Texture, stripSrc, stripDst, ImpellerTextureSampling.kImpellerTextureSamplingLinear, paint);
        TextureSceneHelpers.DrawFrame(e.Builder, stripDst, ImpellerColor.FromRgb(0x93, 0xE6, 0xAC));
    }

    public void Dispose()
    {
        _image?.Dispose();
        _image = null;
    }
}
