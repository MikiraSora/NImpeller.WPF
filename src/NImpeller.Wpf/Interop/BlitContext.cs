using System;

using Silk.NET.Vulkan;

namespace NImpeller.Wpf.Interop;

/// <summary>
/// Per-swapchain GPU resources used by <see cref="VkTrampolines.QueuePresentKHR"/> to
/// blit Impeller's swapchain image into the D3D-shared target VkImage owned by an
/// <c>ImpellerView</c>.
///
/// One <c>BlitContext</c> exists per active <c>ImpellerView</c>. Lookup happens in
/// <c>VkTrampolines.BlitsBySwapchain[swapchainHandle]</c>.
/// </summary>
internal sealed unsafe class BlitContext : IDisposable
{
    public Vk Vk { get; }
    public Device Device { get; }
    public Queue Queue { get; }
    public uint QueueFamilyIndex { get; }
    public Image TargetImage { get; set; }
    public Extent2D TargetExtent { get; set; }
    public CommandPool CommandPool { get; private set; }
    public CommandBuffer CommandBuffer { get; private set; }
    public Fence Fence { get; private set; }
    /// <summary>
    /// Lock that serializes <c>vkQueueSubmit</c> across every BlitContext sharing
    /// the same <see cref="Queue"/>. <b>Provided by <c>ImpellerSharedHost</c> and
    /// shared process-wide</b> — this is intentionally the same object instance
    /// across all views so multi-view present paths cannot race on submit.
    /// Do not allocate a per-instance lock here.
    /// </summary>
    public object SharedQueueLock { get; }
    public long FrameCounter;

    private bool _disposed;

    public BlitContext(Vk vk, Device device, Queue queue, uint queueFamilyIndex,
        Image targetImage, Extent2D targetExtent, object sharedQueueLock)
    {
        Vk = vk;
        Device = device;
        Queue = queue;
        QueueFamilyIndex = queueFamilyIndex;
        TargetImage = targetImage;
        TargetExtent = targetExtent;
        SharedQueueLock = sharedQueueLock;

        var poolInfo = new CommandPoolCreateInfo(
            flags: CommandPoolCreateFlags.ResetCommandBufferBit,
            queueFamilyIndex: queueFamilyIndex);
        Check(vk.CreateCommandPool(device, &poolInfo, null, out var pool), "vkCreateCommandPool(blit)");
        CommandPool = pool;

        var allocInfo = new CommandBufferAllocateInfo(
            commandPool: pool,
            level: CommandBufferLevel.Primary,
            commandBufferCount: 1u);
        Check(vk.AllocateCommandBuffers(device, &allocInfo, out var cmd), "vkAllocateCommandBuffers(blit)");
        CommandBuffer = cmd;

        // Created signaled so the very first WaitForFences in DoBlit returns immediately.
        var fenceInfo = new FenceCreateInfo(flags: FenceCreateFlags.SignaledBit);
        Check(vk.CreateFence(device, &fenceInfo, null, out var fence), "vkCreateFence(blit)");
        Fence = fence;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (Fence.Handle != 0) { Vk.DestroyFence(Device, Fence, null); Fence = default; }
        if (CommandPool.Handle != 0) { Vk.DestroyCommandPool(Device, CommandPool, null); CommandPool = default; CommandBuffer = default; }
    }

    private static void Check(Result r, string what)
    {
        if (r != Result.Success)
            throw new InvalidOperationException($"{what} failed: {r}");
    }
}
