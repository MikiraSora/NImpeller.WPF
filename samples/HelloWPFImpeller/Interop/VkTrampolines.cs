using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace HelloWPFImpeller.Interop;

/// <summary>
/// Static trampolines installed in place of selected Vulkan entry points.
///
/// Each trampoline calls the corresponding "Real..." function pointer after rewriting
/// its arguments. Real function pointers are populated lazily by VkProcInterceptor.GetProcAddress
/// the first time Impeller asks for them.
/// </summary>
internal static unsafe class VkTrampolines
{
    // --- Real function pointers (populated by VkProcInterceptor) ---
    public static delegate* unmanaged[Cdecl]<InstanceCreateInfo*, AllocationCallbacks*, Instance*, Result> RealCreateInstance;
    public static delegate* unmanaged[Cdecl]<PhysicalDevice, DeviceCreateInfo*, AllocationCallbacks*, Device*, Result> RealCreateDevice;
    public static delegate* unmanaged[Cdecl]<Instance, uint*, PhysicalDevice*, Result> RealEnumeratePhysicalDevices;
    public static delegate* unmanaged[Cdecl]<PhysicalDevice, PhysicalDeviceProperties2*, void> RealGetPhysicalDeviceProperties2;
    public static delegate* unmanaged[Cdecl]<PhysicalDevice, SurfaceKHR, SurfaceCapabilitiesKHR*, Result> RealGetPhysicalDeviceSurfaceCapabilitiesKHR;
    public static delegate* unmanaged[Cdecl]<Device, SwapchainCreateInfoKHR*, AllocationCallbacks*, SwapchainKHR*, Result> RealCreateSwapchainKHR;
    public static delegate* unmanaged[Cdecl]<Device, SwapchainKHR, uint*, Image*, Result> RealGetSwapchainImagesKHR;
    public static delegate* unmanaged[Cdecl]<Device, SwapchainKHR, ulong, Semaphore, Fence, uint*, Result> RealAcquireNextImageKHR;
    public static delegate* unmanaged[Cdecl]<Queue, PresentInfoKHR*, Result> RealQueuePresentKHR;

    // --- Configuration (set by interceptor host before context creation) ---
    public static ulong TargetAdapterLuid; // 64-bit LUID (Low + High << 32). 0 = no preference.

    // --- Swapchain state collected by hooks ---
    // Keyed by SwapchainKHR.Handle.
    public static readonly Dictionary<ulong, Image[]> SwapchainImages = new();
    public static readonly Dictionary<ulong, uint> CurrentAcquiredIndex = new();
    public static readonly Dictionary<ulong, Extent2D> SwapchainExtent = new();

    // --- Blit-on-present configuration (set by interceptor host after context creation) ---
    public static Vk? Vk;
    public static Device BlitDevice;
    public static Image BlitTargetImage;        // SharedVulkanImage's VkImage (the D3D-shared target)
    public static Extent2D BlitTargetExtent;
    public static CommandPool BlitCommandPool;
    public static CommandBuffer BlitCommandBuffer;
    public static Fence BlitFence;
    public static bool BlitEnabled;             // false until host calls InstallBlitResources

    /// <summary>
    /// Sequence number bumped every time vkQueuePresentKHR completes a blit; the WPF
    /// renderer reads this to know when a new frame is ready to be shown via D3DImage.
    /// </summary>
    public static long BlitFrameCounter;

    // --- Instance extensions appended in vkCreateInstance ---
    private static readonly string[] AppendInstanceExtensions =
    {
        "VK_KHR_external_memory_capabilities",
        "VK_KHR_get_physical_device_properties2",
        "VK_KHR_external_semaphore_capabilities",
    };

    // --- Device extensions appended in vkCreateDevice ---
    private static readonly string[] AppendDeviceExtensions =
    {
        "VK_KHR_external_memory",
        "VK_KHR_external_memory_win32",
        "VK_KHR_dedicated_allocation",
        "VK_KHR_get_memory_requirements2",
    };

    // --- Pinned UTF-8 byte buffers for the extension names (kept alive for the process lifetime) ---
    private static readonly byte*[] _appendInstanceExtPtrs = AllocPinnedAsciiArray(AppendInstanceExtensions);
    private static readonly byte*[] _appendDeviceExtPtrs   = AllocPinnedAsciiArray(AppendDeviceExtensions);

    private static byte*[] AllocPinnedAsciiArray(string[] names)
    {
        var arr = new byte*[names.Length];
        for (int i = 0; i < names.Length; i++)
            arr[i] = (byte*)Marshal.StringToHGlobalAnsi(names[i]);
        return arr;
    }

    // ============================================================================
    // vkCreateInstance: append instance extensions before delegating to the real call
    // ============================================================================
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static Result CreateInstance(InstanceCreateInfo* pCreateInfo, AllocationCallbacks* pAllocator, Instance* pInstance)
    {
        if (pCreateInfo == null || RealCreateInstance == null)
            return RealCreateInstance == null ? Result.ErrorInitializationFailed : RealCreateInstance(pCreateInfo, pAllocator, pInstance);

        var augmented = AugmentExtensions(
            origCount: pCreateInfo->EnabledExtensionCount,
            origPtrs: pCreateInfo->PpEnabledExtensionNames,
            appendPtrs: _appendInstanceExtPtrs,
            out var newCount,
            out var newPtrs);

        var newCreateInfo = *pCreateInfo;
        newCreateInfo.EnabledExtensionCount = newCount;
        newCreateInfo.PpEnabledExtensionNames = newPtrs;

        App.Log($"[VkTrampolines] vkCreateInstance: orig {pCreateInfo->EnabledExtensionCount} -> {newCount} extensions");
        var r = RealCreateInstance(&newCreateInfo, pAllocator, pInstance);

        FreeAugmented(augmented, newPtrs);
        return r;
    }

    // ============================================================================
    // vkCreateDevice: append device extensions before delegating to the real call
    // ============================================================================
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static Result CreateDevice(PhysicalDevice physicalDevice, DeviceCreateInfo* pCreateInfo, AllocationCallbacks* pAllocator, Device* pDevice)
    {
        if (pCreateInfo == null || RealCreateDevice == null)
            return RealCreateDevice == null ? Result.ErrorInitializationFailed : RealCreateDevice(physicalDevice, pCreateInfo, pAllocator, pDevice);

        var augmented = AugmentExtensions(
            origCount: pCreateInfo->EnabledExtensionCount,
            origPtrs: pCreateInfo->PpEnabledExtensionNames,
            appendPtrs: _appendDeviceExtPtrs,
            out var newCount,
            out var newPtrs);

        var newCreateInfo = *pCreateInfo;
        newCreateInfo.EnabledExtensionCount = newCount;
        newCreateInfo.PpEnabledExtensionNames = newPtrs;

        App.Log($"[VkTrampolines] vkCreateDevice: orig {pCreateInfo->EnabledExtensionCount} -> {newCount} extensions");
        var r = RealCreateDevice(physicalDevice, &newCreateInfo, pAllocator, pDevice);

        FreeAugmented(augmented, newPtrs);
        return r;
    }

    // ============================================================================
    // vkEnumeratePhysicalDevices: if TargetAdapterLuid is set, move the matching device to index 0
    // ============================================================================
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static Result EnumeratePhysicalDevices(Instance instance, uint* pPhysicalDeviceCount, PhysicalDevice* pPhysicalDevices)
    {
        var r = RealEnumeratePhysicalDevices(instance, pPhysicalDeviceCount, pPhysicalDevices);
        if (r != Result.Success && r != Result.Incomplete) return r;
        if (pPhysicalDevices == null) return r;
        if (TargetAdapterLuid == 0 || RealGetPhysicalDeviceProperties2 == null) return r;

        uint count = *pPhysicalDeviceCount;
        if (count <= 1) return r;

        int matchIdx = -1;
        for (uint i = 0; i < count; i++)
        {
            if (TryGetDeviceLuid(pPhysicalDevices[i], out var luid) && luid == TargetAdapterLuid)
            {
                matchIdx = (int)i;
                break;
            }
        }

        if (matchIdx > 0)
        {
            App.Log($"[VkTrampolines] vkEnumeratePhysicalDevices: swapping LUID-matched device from index {matchIdx} to 0");
            (pPhysicalDevices[0], pPhysicalDevices[matchIdx]) = (pPhysicalDevices[matchIdx], pPhysicalDevices[0]);
        }
        else if (matchIdx < 0)
        {
            App.Log($"[VkTrampolines] vkEnumeratePhysicalDevices: no device matched target LUID 0x{TargetAdapterLuid:X16} (have {count} devices)");
        }
        return r;
    }

    private static bool TryGetDeviceLuid(PhysicalDevice device, out ulong luid)
    {
        var idProps = new PhysicalDeviceIDProperties { SType = StructureType.PhysicalDeviceIDProperties };
        var props2 = new PhysicalDeviceProperties2 { SType = StructureType.PhysicalDeviceProperties2, PNext = &idProps };
        RealGetPhysicalDeviceProperties2(device, &props2);

        if (!idProps.DeviceLuidvalid) { luid = 0; return false; }

        // VkPhysicalDeviceIDProperties.deviceLUID is a uint8_t[VK_LUID_SIZE] (8 bytes), little-endian as ulong.
        ulong result = 0;
        for (int i = 0; i < 8; i++)
            result |= ((ulong)idProps.DeviceLuid[i]) << (i * 8);
        luid = result;
        return true;
    }

    // ============================================================================
    // Extension-array augmentation helper
    // ============================================================================
    // Strategy: allocate a new byte** array in unmanaged memory containing the original
    // pointers plus deduped appended pointers, then return the unmanaged pointer to be
    // freed via FreeAugmented after vkCreate*.
    private static IntPtr AugmentExtensions(uint origCount, byte** origPtrs, byte*[] appendPtrs,
        out uint newCount, out byte** newPtrs)
    {
        int maxCount = (int)origCount + appendPtrs.Length;
        IntPtr unmanaged = Marshal.AllocHGlobal(maxCount * sizeof(IntPtr));
        var combined = (byte**)unmanaged;

        uint outCount = 0;
        for (uint i = 0; i < origCount; i++)
            combined[outCount++] = origPtrs[i];

        for (int i = 0; i < appendPtrs.Length; i++)
        {
            bool already = false;
            for (uint j = 0; j < origCount; j++)
            {
                if (AsciiEquals(origPtrs[j], appendPtrs[i])) { already = true; break; }
            }
            if (!already)
                combined[outCount++] = appendPtrs[i];
        }

        newCount = outCount;
        newPtrs = combined;
        return unmanaged;
    }

    private static void FreeAugmented(IntPtr unmanaged, byte** _) => Marshal.FreeHGlobal(unmanaged);

    private static bool AsciiEquals(byte* a, byte* b)
    {
        if (a == b) return true;
        if (a == null || b == null) return false;
        while (true)
        {
            byte ca = *a, cb = *b;
            if (ca != cb) return false;
            if (ca == 0) return true;
            a++; b++;
        }
    }

    // ============================================================================
    // vkGetPhysicalDeviceSurfaceCapabilitiesKHR: force TRANSFER_SRC_BIT into
    // supportedUsageFlags so Impeller's swapchain creation accepts our augmented usage.
    // ============================================================================
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static Result GetPhysicalDeviceSurfaceCapabilitiesKHR(PhysicalDevice physicalDevice, SurfaceKHR surface, SurfaceCapabilitiesKHR* pSurfaceCapabilities)
    {
        var r = RealGetPhysicalDeviceSurfaceCapabilitiesKHR(physicalDevice, surface, pSurfaceCapabilities);
        if (r == Result.Success && pSurfaceCapabilities != null)
        {
            pSurfaceCapabilities->SupportedUsageFlags |= ImageUsageFlags.TransferSrcBit;
        }
        return r;
    }

    // ============================================================================
    // vkCreateSwapchainKHR: append TRANSFER_SRC_BIT to imageUsage so we can blit
    // the swapchain image into our shared target image during vkQueuePresentKHR.
    // ============================================================================
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static Result CreateSwapchainKHR(Device device, SwapchainCreateInfoKHR* pCreateInfo, AllocationCallbacks* pAllocator, SwapchainKHR* pSwapchain)
    {
        if (pCreateInfo == null) return RealCreateSwapchainKHR(device, pCreateInfo, pAllocator, pSwapchain);

        var augmented = *pCreateInfo;
        var origUsage = augmented.ImageUsage;
        augmented.ImageUsage = origUsage | ImageUsageFlags.TransferSrcBit;

        App.Log($"[VkTrampolines] vkCreateSwapchainKHR: usage 0x{(uint)origUsage:X} -> 0x{(uint)augmented.ImageUsage:X}, extent {augmented.ImageExtent.Width}x{augmented.ImageExtent.Height}, format {augmented.ImageFormat}, count {augmented.MinImageCount}");

        var r = RealCreateSwapchainKHR(device, &augmented, pAllocator, pSwapchain);
        if (r == Result.Success && pSwapchain != null)
        {
            ulong handle = pSwapchain->Handle;
            SwapchainExtent[handle] = augmented.ImageExtent;
            App.Log($"[VkTrampolines] vkCreateSwapchainKHR: created VkSwapchainKHR=0x{handle:X16}");
        }
        return r;
    }

    // ============================================================================
    // vkGetSwapchainImagesKHR: cache the VkImage[] for each swapchain so we know
    // what to blit from in vkQueuePresentKHR.
    // ============================================================================
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static Result GetSwapchainImagesKHR(Device device, SwapchainKHR swapchain, uint* pSwapchainImageCount, Image* pSwapchainImages)
    {
        var r = RealGetSwapchainImagesKHR(device, swapchain, pSwapchainImageCount, pSwapchainImages);
        if (r == Result.Success && pSwapchainImages != null && pSwapchainImageCount != null)
        {
            uint count = *pSwapchainImageCount;
            var arr = new Image[count];
            for (uint i = 0; i < count; i++) arr[i] = pSwapchainImages[i];
            SwapchainImages[swapchain.Handle] = arr;
            App.Log($"[VkTrampolines] vkGetSwapchainImagesKHR: cached {count} images for swapchain 0x{swapchain.Handle:X16}");
            for (int i = 0; i < count; i++)
                App.Log($"    image[{i}] = 0x{arr[i].Handle:X16}");
        }
        return r;
    }

    // ============================================================================
    // vkAcquireNextImageKHR: remember the image index for the upcoming present.
    // ============================================================================
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static Result AcquireNextImageKHR(Device device, SwapchainKHR swapchain, ulong timeout, Semaphore semaphore, Fence fence, uint* pImageIndex)
    {
        var r = RealAcquireNextImageKHR(device, swapchain, timeout, semaphore, fence, pImageIndex);
        if ((r == Result.Success || r == Result.SuboptimalKhr) && pImageIndex != null)
        {
            CurrentAcquiredIndex[swapchain.Handle] = *pImageIndex;
        }
        return r;
    }

    // ============================================================================
    // vkQueuePresentKHR: instead of presenting to our hidden window, blit the freshly
    // rendered swapchain image into the D3D-shared target image. WPF/D3DImage then
    // picks up the result via the D3D9 surface that aliases the same memory.
    // ============================================================================
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static Result QueuePresentKHR(Queue queue, PresentInfoKHR* pPresentInfo)
    {
        if (!BlitEnabled || Vk is null || pPresentInfo == null || pPresentInfo->SwapchainCount == 0)
            return RealQueuePresentKHR(queue, pPresentInfo);

        var swapchainPtr = pPresentInfo->PSwapchains;
        var indexPtr = pPresentInfo->PImageIndices;
        ulong swapchainHandle = swapchainPtr[0].Handle;
        uint imageIndex = indexPtr[0];

        if (!SwapchainImages.TryGetValue(swapchainHandle, out var imageArr) || imageIndex >= imageArr.Length)
        {
            App.Log($"[VkTrampolines] vkQueuePresentKHR: swapchain 0x{swapchainHandle:X16} or index {imageIndex} not cached; falling back to passthrough");
            return RealQueuePresentKHR(queue, pPresentInfo);
        }

        var srcImage = imageArr[imageIndex];
        if (!SwapchainExtent.TryGetValue(swapchainHandle, out var srcExtent))
            srcExtent = BlitTargetExtent;

        try
        {
            DoBlit(queue, srcImage, srcExtent, pPresentInfo->PWaitSemaphores, pPresentInfo->WaitSemaphoreCount);
            System.Threading.Interlocked.Increment(ref BlitFrameCounter);
        }
        catch (Exception ex)
        {
            App.Log($"[VkTrampolines] vkQueuePresentKHR blit failed: {ex.Message}");
        }

        // Skip the real present — the swapchain image is owned by us at this point,
        // but ANV/driver expects the image back in PRESENT_SRC_KHR (we leave it that way
        // at the end of DoBlit) and won't actually display anything since the window is hidden.
        // We still need to "ack" so the next vkAcquireNextImageKHR will succeed; the simplest
        // way is to call the real present too. The wait semaphores have already been consumed
        // by our DoBlit submit, so we must NOT pass them again — make a copy with 0 semaphores.
        var copy = *pPresentInfo;
        copy.WaitSemaphoreCount = 0;
        copy.PWaitSemaphores = null;
        return RealQueuePresentKHR(queue, &copy);
    }

    private static void DoBlit(Queue queue, Image srcImage, Extent2D srcExtent, Semaphore* pWaitSemaphores, uint waitSemaphoreCount)
    {
        var vk = Vk!;
        var device = BlitDevice;
        var cmd = BlitCommandBuffer;
        var fence = BlitFence;

        // Wait for the previous blit to finish before recycling cmd/fence.
        var fenceLocal = fence;
        Check(vk.WaitForFences(device, 1u, in fenceLocal, true, 1_000_000_000ul), "vkWaitForFences(blit prev)");
        Check(vk.ResetFences(device, 1u, in fenceLocal), "vkResetFences(blit)");

        Check(vk.ResetCommandBuffer(cmd, 0), "vkResetCommandBuffer(blit)");
        var beginInfo = new CommandBufferBeginInfo(flags: CommandBufferUsageFlags.OneTimeSubmitBit);
        Check(vk.BeginCommandBuffer(cmd, &beginInfo), "vkBeginCommandBuffer(blit)");

        var range = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0u, 1u, 0u, 1u);

        // 1. Transition src (swapchain image): PRESENT_SRC_KHR -> TRANSFER_SRC_OPTIMAL
        var bSrcToTransferSrc = new ImageMemoryBarrier(
            oldLayout: ImageLayout.PresentSrcKhr,
            newLayout: ImageLayout.TransferSrcOptimal,
            srcAccessMask: AccessFlags.None,
            dstAccessMask: AccessFlags.TransferReadBit,
            srcQueueFamilyIndex: Vk.QueueFamilyIgnored,
            dstQueueFamilyIndex: Vk.QueueFamilyIgnored,
            image: srcImage,
            subresourceRange: range);

        // 2. Transition dst (target shared image): COLOR_ATTACHMENT_OPTIMAL/UNDEFINED -> TRANSFER_DST_OPTIMAL
        //    We use UNDEFINED as oldLayout because we don't strictly need to preserve the previous frame's
        //    contents (we overwrite the full image). This is faster and works regardless of how the
        //    image was last left.
        var bDstToTransferDst = new ImageMemoryBarrier(
            oldLayout: ImageLayout.Undefined,
            newLayout: ImageLayout.TransferDstOptimal,
            srcAccessMask: AccessFlags.None,
            dstAccessMask: AccessFlags.TransferWriteBit,
            srcQueueFamilyIndex: Vk.QueueFamilyIgnored,
            dstQueueFamilyIndex: Vk.QueueFamilyIgnored,
            image: BlitTargetImage,
            subresourceRange: range);

        var barriers0 = stackalloc ImageMemoryBarrier[2] { bSrcToTransferSrc, bDstToTransferDst };
        vk.CmdPipelineBarrier(cmd,
            srcStageMask: PipelineStageFlags.TopOfPipeBit,
            dstStageMask: PipelineStageFlags.TransferBit,
            dependencyFlags: 0u,
            memoryBarrierCount: 0u, pMemoryBarriers: null,
            bufferMemoryBarrierCount: 0u, pBufferMemoryBarriers: null,
            imageMemoryBarrierCount: 2u, pImageMemoryBarriers: barriers0);

        // 3. vkCmdBlitImage (driver handles RGBA<->BGRA channel swizzle for unorm formats)
        var blit = new ImageBlit();
        blit.SrcSubresource = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0u, 0u, 1u);
        blit.SrcOffsets.Element0 = default;
        blit.SrcOffsets.Element1 = new Offset3D((int)srcExtent.Width, (int)srcExtent.Height, 1);
        blit.DstSubresource = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0u, 0u, 1u);
        blit.DstOffsets.Element0 = default;
        blit.DstOffsets.Element1 = new Offset3D((int)BlitTargetExtent.Width, (int)BlitTargetExtent.Height, 1);

        vk.CmdBlitImage(cmd,
            srcImage, ImageLayout.TransferSrcOptimal,
            BlitTargetImage, ImageLayout.TransferDstOptimal,
            1u, &blit, Filter.Linear);

        // 4. Transition back: src -> PRESENT_SRC_KHR (so passthrough vkQueuePresentKHR is happy),
        //    dst -> COLOR_ATTACHMENT_OPTIMAL (stable terminal state for D3D side to read)
        var bSrcBack = new ImageMemoryBarrier(
            oldLayout: ImageLayout.TransferSrcOptimal,
            newLayout: ImageLayout.PresentSrcKhr,
            srcAccessMask: AccessFlags.TransferReadBit,
            dstAccessMask: AccessFlags.None,
            srcQueueFamilyIndex: Vk.QueueFamilyIgnored,
            dstQueueFamilyIndex: Vk.QueueFamilyIgnored,
            image: srcImage,
            subresourceRange: range);

        var bDstBack = new ImageMemoryBarrier(
            oldLayout: ImageLayout.TransferDstOptimal,
            newLayout: ImageLayout.ColorAttachmentOptimal,
            srcAccessMask: AccessFlags.TransferWriteBit,
            dstAccessMask: AccessFlags.ColorAttachmentReadBit | AccessFlags.ColorAttachmentWriteBit,
            srcQueueFamilyIndex: Vk.QueueFamilyIgnored,
            dstQueueFamilyIndex: Vk.QueueFamilyIgnored,
            image: BlitTargetImage,
            subresourceRange: range);

        var barriers1 = stackalloc ImageMemoryBarrier[2] { bSrcBack, bDstBack };
        vk.CmdPipelineBarrier(cmd,
            srcStageMask: PipelineStageFlags.TransferBit,
            dstStageMask: PipelineStageFlags.BottomOfPipeBit,
            dependencyFlags: 0u,
            memoryBarrierCount: 0u, pMemoryBarriers: null,
            bufferMemoryBarrierCount: 0u, pBufferMemoryBarriers: null,
            imageMemoryBarrierCount: 2u, pImageMemoryBarriers: barriers1);

        Check(vk.EndCommandBuffer(cmd), "vkEndCommandBuffer(blit)");

        // Submit: wait on Impeller's render-done semaphore (carried in pPresentInfo.pWaitSemaphores),
        // signal nothing (the real vkQueuePresentKHR we call afterwards has no wait semaphores).
        var cmdLocal = cmd;
        var waitStages = stackalloc PipelineStageFlags[(int)Math.Max(1u, waitSemaphoreCount)];
        for (int i = 0; i < waitSemaphoreCount; i++)
            waitStages[i] = PipelineStageFlags.TransferBit;

        var submitInfo = new SubmitInfo(
            waitSemaphoreCount: waitSemaphoreCount,
            pWaitSemaphores: pWaitSemaphores,
            pWaitDstStageMask: waitStages,
            commandBufferCount: 1u,
            pCommandBuffers: &cmdLocal);

        Check(vk.QueueSubmit(queue, 1u, &submitInfo, fence), "vkQueueSubmit(blit)");
        // CPU-side wait so the D3D side can read the result safely in the next D3DImage.Lock cycle.
        Check(vk.WaitForFences(device, 1u, in fenceLocal, true, 1_000_000_000ul), "vkWaitForFences(blit)");
    }

    public static void InstallBlitResources(Vk vk, Device device, Queue queue, uint queueFamilyIndex,
        Image targetImage, Extent2D targetExtent)
    {
        Vk = vk;
        BlitDevice = device;
        BlitTargetImage = targetImage;
        BlitTargetExtent = targetExtent;

        var poolInfo = new CommandPoolCreateInfo(
            flags: CommandPoolCreateFlags.ResetCommandBufferBit,
            queueFamilyIndex: queueFamilyIndex);
        Check(vk.CreateCommandPool(device, &poolInfo, null, out BlitCommandPool), "vkCreateCommandPool(blit)");

        var allocInfo = new CommandBufferAllocateInfo(
            commandPool: BlitCommandPool,
            level: CommandBufferLevel.Primary,
            commandBufferCount: 1u);
        Check(vk.AllocateCommandBuffers(device, &allocInfo, out BlitCommandBuffer), "vkAllocateCommandBuffers(blit)");

        // Fence created signaled so the first WaitForFences in DoBlit returns immediately.
        var fenceInfo = new FenceCreateInfo(flags: FenceCreateFlags.SignaledBit);
        Check(vk.CreateFence(device, &fenceInfo, null, out BlitFence), "vkCreateFence(blit)");

        BlitEnabled = true;
        App.Log($"[VkTrampolines] blit resources installed (target image=0x{targetImage.Handle:X16}, extent={targetExtent.Width}x{targetExtent.Height})");
    }

    private static void Check(Result r, string what)
    {
        if (r != Result.Success)
            throw new InvalidOperationException($"{what} failed: {r}");
    }
}
