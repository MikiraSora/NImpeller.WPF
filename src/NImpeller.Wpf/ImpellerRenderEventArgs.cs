using System;

using NImpeller;

namespace NImpeller.Wpf;

/// <summary>
/// Event args delivered to <see cref="ImpellerView.Render"/> handlers. Provides the
/// Impeller drawing primitives + timing/size information for the current frame.
///
/// The <see cref="Builder"/> is pre-created and will be turned into a display list and
/// drawn onto the view's surface after the handler returns; do not store or dispose it.
/// </summary>
public sealed class ImpellerRenderEventArgs : EventArgs
{
    public ImpellerRenderEventArgs(
        ImpellerView source,
        ImpellerDisplayListBuilder builder,
        ImpellerTypographyContext? typography,
        int pixelWidth,
        int pixelHeight,
        float dpiScale,
        TimeSpan deltaTime,
        TimeSpan totalTime,
        long frameNumber)
    {
        Source = source;
        Builder = builder;
        Typography = typography;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        DpiScale = dpiScale;
        DeltaTime = deltaTime;
        TotalTime = totalTime;
        FrameNumber = frameNumber;
    }

    /// <summary>The view that is being rendered.</summary>
    public ImpellerView Source { get; }

    /// <summary>The active display-list builder. Issue all <c>Draw*</c> calls on this.</summary>
    public ImpellerDisplayListBuilder Builder { get; }

    /// <summary>Shared typography context (null if Impeller failed to create one).</summary>
    public ImpellerTypographyContext? Typography { get; }

    /// <summary>Physical pixel width of the render target.</summary>
    public int PixelWidth { get; }

    /// <summary>Physical pixel height of the render target.</summary>
    public int PixelHeight { get; }

    /// <summary>System DPI scale (1.0 at 96 DPI, 1.5 at 144 DPI, etc.).</summary>
    public float DpiScale { get; }

    /// <summary>Time since the previous frame on this view.</summary>
    public TimeSpan DeltaTime { get; }

    /// <summary>Total time since the view started rendering.</summary>
    public TimeSpan TotalTime { get; }

    /// <summary>Monotonically increasing frame counter (starts at 1 for the first frame).</summary>
    public long FrameNumber { get; }
}
