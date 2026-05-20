using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;

/// <summary>
/// A renderable demo scene for the gallery. Each scene focuses on a particular
/// aspect of <c>ImpellerDisplayListBuilder</c> / <c>ImpellerPaint</c> / etc.
/// </summary>
public interface IGalleryScene
{
    /// <summary>Display name in the gallery list.</summary>
    string Name { get; }

    /// <summary>Optional one-line description shown beneath the name.</summary>
    string? Description { get; }

    /// <summary>
    /// Render one frame. The scene should treat the entire <c>e.PixelWidth</c> x
    /// <c>e.PixelHeight</c> area as its canvas, and may use <c>e.TotalTime</c> /
    /// <c>e.FrameNumber</c> for animation.
    /// </summary>
    void Render(ImpellerRenderEventArgs e);
}

/// <summary>
/// Optional capability: a scene that exposes a single numeric "how many items to draw"
/// knob. The main window surfaces this as a +/- button pair while the scene is selected.
/// </summary>
public interface IConfigurableScene : IGalleryScene
{
    /// <summary>Current number of items the scene will draw next frame.</summary>
    int ItemCount { get; set; }

    /// <summary>Increment applied by the +/- buttons (clamped to Min/Max).</summary>
    int ItemStep { get; }

    /// <summary>Lower bound for <see cref="ItemCount"/>.</summary>
    int ItemMin { get; }

    /// <summary>Upper bound for <see cref="ItemCount"/>.</summary>
    int ItemMax { get; }

    /// <summary>Optional unit suffix shown next to the count, e.g. "rects", "paths".</summary>
    string ItemLabel { get; }
}
