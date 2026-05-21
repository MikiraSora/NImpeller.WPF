using System;
using System.Windows;

namespace NImpeller.Wpf;

/// <summary>
/// Configuration for an <see cref="ImpellerView"/>. Pass to
/// <see cref="ImpellerView.InitializeRender(ImpellerViewSettings)"/>, or call
/// <see cref="ImpellerView.InitializeRender()"/> for defaults.
///
/// <para><b>Lifetime model</b></para>
/// Settings are consumed in two windows:
/// <list type="number">
///   <item><b>First initialization in the process</b>: fields that affect the
///     singleton <c>ImpellerContext</c> (currently <see cref="EnableValidation"/>)
///     are locked. Later views inherit these and cannot change them.</item>
///   <item><b>First initialization on this view</b>: every other field is read
///     once to build the per-view GPU resources (swapchain, shared texture,
///     ticker policy).</item>
/// </list>
/// Calling initialize more than once on the same view is invalid. Use
/// <see cref="ImpellerView.Start()"/> and <see cref="ImpellerView.Stop"/> to
/// control only the continuous render loop after initialization. To apply locked
/// or first-initialization-only changes, create a new <see cref="ImpellerView"/>
/// with the desired settings.
/// </summary>
public sealed class ImpellerViewSettings
{
    /// <summary>
    /// When true, initialization automatically starts the global
    /// <c>CompositionTarget.Rendering</c> loop for this view. When false, the
    /// view is initialized for on-demand rendering and only draws when
    /// <see cref="ImpellerView.InvalidateRender"/> is called, unless
    /// <see cref="ImpellerView.Start()"/> is called later.
    /// <para><b>Lifetime</b>: consumed by
    /// <see cref="ImpellerView.InitializeRender(ImpellerViewSettings)"/>. After
    /// initialization, use <see cref="ImpellerView.Start()"/> and
    /// <see cref="ImpellerView.Stop"/> to begin or pause the continuous loop.</para>
    /// </summary>
    public bool RenderContinuously { get; init; } = true;

    /// <summary>
    /// When true, the view's backing texture is allocated in physical pixels
    /// using the view's current DPI, so text and one-pixel strokes stay sharp on high-DPI
    /// displays. When false, the texture is allocated in DIPs (logical pixels),
    /// which is cheaper but blurry on displays above 100% DPI.
    /// <para><b>Lifetime</b>: read during the view's first initialization. Set it
    /// before calling <see cref="ImpellerView.InitializeRender(ImpellerViewSettings)"/>
    /// or <see cref="ImpellerView.Start()"/>. To change it later, create a new
    /// <see cref="ImpellerView"/> with the desired settings.</para>
    /// </summary>
    public bool UseDeviceDpi { get; init; } = true;

    /// <summary>
    /// Override the layout size returned by <c>MeasureOverride</c>. When null
    /// (default), the view fills the available size of its parent container.
    /// <para><b>Lifetime</b>: read whenever WPF measures the view.</para>
    /// </summary>
    public Size? LogicalSizeOverride { get; init; }

    /// <summary>
    /// Enable Vulkan validation layers.
    /// <para><b>Lifetime</b>: process-wide, locked at the very first
    /// initialization in the process because the underlying <c>ImpellerContext</c>
    /// / <c>VkInstance</c> is a singleton shared by every <see cref="ImpellerView"/>.
    /// Subsequent values are silently ignored. To toggle, restart the process before
    /// creating the first <see cref="ImpellerView"/>.</para>
    /// </summary>
    public bool EnableValidation { get; init; } = false;
}
