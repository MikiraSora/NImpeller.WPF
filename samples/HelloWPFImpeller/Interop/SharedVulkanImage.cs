using System;

using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace HelloWPFImpeller.Interop;

/// <summary>
/// A VkImage backed by a Direct3D 11 shared texture, imported via VK_KHR_external_memory_win32.
///
/// The image is created on Impeller's internal VkDevice (obtained from
/// ImpellerContext.GetVulkanInfo) so that subsequent blits from Impeller's swapchain
/// image into this image can be submitted on Impeller's graphics queue.
///
/// The format must match the underlying D3D resource (D3D9 X8R8G8B8 / D3D11 BGRA8 ->
/// VK_FORMAT_B8G8R8A8_UNORM).
/// </summary>
internal sealed unsafe class SharedVulkanImage : IDisposable
{
    private readonly Vk _vk;
    private readonly Device _device;
    private readonly PhysicalDevice _physicalDevice;
    private readonly Format _format;
    private readonly ExternalMemoryHandleTypeFlags _handleType;

    private Image _image;
    private DeviceMemory _memory;
    private uint _width;
    private uint _height;
    private bool _disposed;

    private CommandPool _cmdPool;
    private CommandBuffer _cmdBuffer;
    private Fence _fence;
    private Queue _queue;
    private ImageLayout _currentLayout = ImageLayout.Undefined;

    public Image VkImage => _image;
    public DeviceMemory VkMemory => _memory;
    public Format Format => _format;
    public uint Width => _width;
    public uint Height => _height;

    public SharedVulkanImage(Vk vk, PhysicalDevice physicalDevice, Device device,
        Format format = Format.B8G8R8A8Unorm,
        ExternalMemoryHandleTypeFlags handleType = ExternalMemoryHandleTypeFlags.D3D11TextureKmtBit)
    {
        _vk = vk;
        _physicalDevice = physicalDevice;
        _device = device;
        _format = format;
        _handleType = handleType;
    }

    public void Import(nint sharedHandle, uint width, uint height)
    {
        DestroyResources();

        _width = width;
        _height = height;

        var externalMemoryImageInfo = new ExternalMemoryImageCreateInfo(handleTypes: _handleType);

        var imageInfo = new ImageCreateInfo(
            pNext: &externalMemoryImageInfo,
            // We will (a) blit from Impeller's swapchain image into this image
            // (TRANSFER_DST_BIT), and (b) keep it accessible as a color attachment
            // for any future direct rendering paths (ColorAttachmentBit).
            usage: ImageUsageFlags.TransferDstBit | ImageUsageFlags.ColorAttachmentBit,
            format: _format,
            imageType: ImageType.Type2D,
            mipLevels: 1u,
            arrayLayers: 1u,
            samples: SampleCountFlags.Count1Bit,
            tiling: ImageTiling.Optimal,
            initialLayout: ImageLayout.Undefined,
            sharingMode: SharingMode.Exclusive,
            extent: new Extent3D(width: width, height: height, depth: 1u));

        Check(_vk.CreateImage(_device, &imageInfo, null, out _image), "vkCreateImage(shared)");

        _vk.GetImageMemoryRequirements(_device, _image, out var requirements);

        var importInfo = new ImportMemoryWin32HandleInfoKHR(
            handleType: _handleType,
            handle: sharedHandle);

        var dedicatedAllocateInfo = new MemoryDedicatedAllocateInfo(image: _image);
        var features = GetImageFormatExternalMemoryFeatures(imageInfo, _handleType);
        if (features.HasFlag(ExternalMemoryFeatureFlags.DedicatedOnlyBit))
        {
            importInfo.PNext = &dedicatedAllocateInfo;
            App.Log("[SharedVulkanImage] driver requires dedicated allocation; chaining MemoryDedicatedAllocateInfo");
        }

        var memoryInfo = new MemoryAllocateInfo(
            pNext: &importInfo,
            allocationSize: requirements.Size,
            memoryTypeIndex: GetMemoryTypeIndex(requirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit));

        Check(_vk.AllocateMemory(_device, &memoryInfo, null, out _memory), "vkAllocateMemory(shared)");
        Check(_vk.BindImageMemory(_device, _image, _memory, 0ul), "vkBindImageMemory(shared)");

        App.Log($"[SharedVulkanImage] imported VkImage=0x{_image.Handle:X16} memory=0x{_memory.Handle:X16} size={requirements.Size} bytes for {_width}x{_height} {_format}");
        // Importing fresh storage resets the image's layout-as-far-as-Vulkan-knows back to Undefined.
        _currentLayout = ImageLayout.Undefined;
    }

    /// <summary>
    /// Sets up a command pool / buffer / fence on the given graphics queue for
    /// stage-5 verification (vkCmdClearColorImage) and the upcoming stage-6 blits.
    /// </summary>
    public void InitializeCommandResources(Queue queue, uint queueFamilyIndex)
    {
        _queue = queue;

        var poolInfo = new CommandPoolCreateInfo(
            flags: CommandPoolCreateFlags.ResetCommandBufferBit,
            queueFamilyIndex: queueFamilyIndex);
        Check(_vk.CreateCommandPool(_device, &poolInfo, null, out _cmdPool), "vkCreateCommandPool");

        var allocInfo = new CommandBufferAllocateInfo(
            commandPool: _cmdPool,
            level: CommandBufferLevel.Primary,
            commandBufferCount: 1u);
        Check(_vk.AllocateCommandBuffers(_device, &allocInfo, out _cmdBuffer), "vkAllocateCommandBuffers");

        var fenceInfo = new FenceCreateInfo(StructureType.FenceCreateInfo);
        Check(_vk.CreateFence(_device, &fenceInfo, null, out _fence), "vkCreateFence");

        App.Log($"[SharedVulkanImage] command resources ready (queue=0x{_queue.Handle:X16}, family={queueFamilyIndex})");
    }

    /// <summary>
    /// Stage-5 verification path: clear the shared image to a flat color using Vulkan,
    /// then block until the GPU has finished. Caller is expected to be inside
    /// D3DImage.Lock so that WPF is not concurrently sampling the back buffer.
    /// </summary>
    public void ClearViaVulkan(float r, float g, float b, float a)
    {
        if (_cmdBuffer.Handle == 0) return;

        Check(_vk.ResetCommandBuffer(_cmdBuffer, 0), "vkResetCommandBuffer");
        var beginInfo = new CommandBufferBeginInfo(flags: CommandBufferUsageFlags.OneTimeSubmitBit);
        Check(_vk.BeginCommandBuffer(_cmdBuffer, &beginInfo), "vkBeginCommandBuffer");

        var range = new ImageSubresourceRange(
            aspectMask: ImageAspectFlags.ColorBit,
            baseMipLevel: 0u, levelCount: 1u,
            baseArrayLayer: 0u, layerCount: 1u);

        // 1. Transition to TRANSFER_DST_OPTIMAL
        var toDst = new ImageMemoryBarrier(
            oldLayout: _currentLayout,
            newLayout: ImageLayout.TransferDstOptimal,
            srcAccessMask: AccessFlags.None,
            dstAccessMask: AccessFlags.TransferWriteBit,
            srcQueueFamilyIndex: Vk.QueueFamilyIgnored,
            dstQueueFamilyIndex: Vk.QueueFamilyIgnored,
            image: _image,
            subresourceRange: range);
        _vk.CmdPipelineBarrier(_cmdBuffer,
            srcStageMask: PipelineStageFlags.TopOfPipeBit,
            dstStageMask: PipelineStageFlags.TransferBit,
            dependencyFlags: 0u,
            memoryBarrierCount: 0u, pMemoryBarriers: null,
            bufferMemoryBarrierCount: 0u, pBufferMemoryBarriers: null,
            imageMemoryBarrierCount: 1u, pImageMemoryBarriers: &toDst);

        // 2. Clear color image
        var clearColor = new ClearColorValue(r, g, b, a);
        _vk.CmdClearColorImage(_cmdBuffer, _image, ImageLayout.TransferDstOptimal,
            &clearColor, 1u, &range);

        // 3. Transition back so D3D9 / D3DImage sees the latest contents in a stable layout.
        //    COLOR_ATTACHMENT_OPTIMAL is what the future blit-from-swapchain path will also
        //    leave the image in.
        var toColor = new ImageMemoryBarrier(
            oldLayout: ImageLayout.TransferDstOptimal,
            newLayout: ImageLayout.ColorAttachmentOptimal,
            srcAccessMask: AccessFlags.TransferWriteBit,
            dstAccessMask: AccessFlags.ColorAttachmentReadBit | AccessFlags.ColorAttachmentWriteBit,
            srcQueueFamilyIndex: Vk.QueueFamilyIgnored,
            dstQueueFamilyIndex: Vk.QueueFamilyIgnored,
            image: _image,
            subresourceRange: range);
        _vk.CmdPipelineBarrier(_cmdBuffer,
            srcStageMask: PipelineStageFlags.TransferBit,
            dstStageMask: PipelineStageFlags.ColorAttachmentOutputBit,
            dependencyFlags: 0u,
            memoryBarrierCount: 0u, pMemoryBarriers: null,
            bufferMemoryBarrierCount: 0u, pBufferMemoryBarriers: null,
            imageMemoryBarrierCount: 1u, pImageMemoryBarriers: &toColor);

        Check(_vk.EndCommandBuffer(_cmdBuffer), "vkEndCommandBuffer");
        _currentLayout = ImageLayout.ColorAttachmentOptimal;

        Check(_vk.ResetFences(_device, 1u, in _fence), "vkResetFences");
        var cmd = _cmdBuffer;
        var submitInfo = new SubmitInfo(
            commandBufferCount: 1u,
            pCommandBuffers: &cmd);
        Check(_vk.QueueSubmit(_queue, 1u, &submitInfo, _fence), "vkQueueSubmit");
        Check(_vk.WaitForFences(_device, 1u, in _fence, true, 1_000_000_000ul), "vkWaitForFences");
    }

    private ExternalMemoryFeatureFlags GetImageFormatExternalMemoryFeatures(
        ImageCreateInfo imageInfo, ExternalMemoryHandleTypeFlags handleType)
    {
        var externalFormatInfo = new PhysicalDeviceExternalImageFormatInfo(handleType: handleType);

        var formatInfo = new PhysicalDeviceImageFormatInfo2(
            pNext: &externalFormatInfo,
            format: imageInfo.Format,
            usage: imageInfo.Usage,
            type: imageInfo.ImageType,
            tiling: imageInfo.Tiling);

        var externalFormatProperties = new ExternalImageFormatProperties(StructureType.ExternalImageFormatProperties);
        var formatProperties = new ImageFormatProperties2(pNext: &externalFormatProperties);

        Check(_vk.GetPhysicalDeviceImageFormatProperties2(_physicalDevice, &formatInfo, &formatProperties),
            "vkGetPhysicalDeviceImageFormatProperties2");

        return externalFormatProperties.ExternalMemoryProperties.ExternalMemoryFeatures;
    }

    private uint GetMemoryTypeIndex(uint typeBits, MemoryPropertyFlags required)
    {
        _vk.GetPhysicalDeviceMemoryProperties(_physicalDevice, out var memProps);
        for (int i = 0; i < memProps.MemoryTypeCount; i++)
        {
            if ((typeBits & (1u << i)) != 0
                && (memProps.MemoryTypes[i].PropertyFlags & required) == required)
                return (uint)i;
        }
        throw new InvalidOperationException(
            $"No Vulkan memory type satisfies typeBits=0x{typeBits:X} required={required}");
    }

    private static void Check(Result r, string what)
    {
        if (r != Result.Success)
            throw new InvalidOperationException($"{what} failed: {r}");
    }

    private void DestroyResources()
    {
        if (_fence.Handle != 0) { _vk.DestroyFence(_device, _fence, null); _fence = default; }
        if (_cmdPool.Handle != 0) { _vk.DestroyCommandPool(_device, _cmdPool, null); _cmdPool = default; _cmdBuffer = default; }
        if (_image.Handle != 0) { _vk.DestroyImage(_device, _image, null); _image = default; }
        if (_memory.Handle != 0) { _vk.FreeMemory(_device, _memory, null); _memory = default; }
        _width = _height = 0;
        _currentLayout = ImageLayout.Undefined;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DestroyResources();
    }
}
