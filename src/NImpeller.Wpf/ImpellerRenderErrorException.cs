using System;

namespace NImpeller.Wpf;

/// <summary>
/// Thrown when an <see cref="ImpellerView"/> fails to (re)build or render its
/// GPU resources. The <see cref="Exception.InnerException"/> identifies the
/// underlying cause (Vulkan, D3D, allocation, swapchain out-of-date, ...).
///
/// Currently raised from:
/// <list type="bullet">
///   <item>The dispatcher-tick resize path, when <c>RecreateForSize</c> fails
///         and leaves the view without a usable swapchain (surfaces the failure
///         to <c>Application.DispatcherUnhandledException</c>).</item>
///   <item><c>ImpellerSharedHost.AcquireAndStart</c>, when the Vulkan physical
///         device list does not contain a device matching the D3D adapter LUID
///         (shared-texture interop would silently corrupt).</item>
/// </list>
/// </summary>
public sealed class ImpellerRenderErrorException : Exception
{
    public ImpellerRenderErrorException(string message)
        : base(message)
    {
    }

    public ImpellerRenderErrorException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
