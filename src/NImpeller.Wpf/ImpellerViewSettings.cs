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
/// or first-initialization-only changes, detach the view, wait for
/// <c>Unloaded</c>, then re-attach with the new settings.
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
    /// using the system DPI, so text and one-pixel strokes stay sharp on high-DPI
    /// displays. When false, the texture is allocated in DIPs (logical pixels),
    /// which is cheaper but blurry on displays above 100% DPI.
    /// <para><b>Lifetime</b>: applied at first initialization. Later changes are
    /// stored but do not rebuild the existing texture on their own; the new value
    /// will be observed on the next resize or DPI change. To apply immediately,
    /// detach and re-attach the view.</para>
    /// </summary>
    public bool UseDeviceDpi { get; init; } = true;

    /// <summary>
    /// Override the layout size returned by <c>MeasureOverride</c>. When null
    /// (default), the view fills the available size of its parent container.
    /// <para><b>Lifetime</b>: read on every <c>MeasureOverride</c>. Call
    /// <c>InvalidateMeasure</c> on the view to apply a new value sooner.</para>
    /// </summary>
    public Size? LogicalSizeOverride { get; init; }

    /// <summary>
    /// Enable Vulkan validation layers.
    /// <para><b>Lifetime</b>: process-wide, locked at the very first
    /// initialization in the process because the underlying <c>ImpellerContext</c>
    /// / <c>VkInstance</c> is a singleton shared by every <see cref="ImpellerView"/>.
    /// Subsequent values are silently ignored. To toggle, the entire process must
    /// be restarted (or <c>ImpellerSharedHost.Shutdown</c> called before the next
    /// acquire, which disposes the GPU context and is rarely useful in production).</para>
    /// </summary>
    public bool EnableValidation { get; init; } = false;
}
