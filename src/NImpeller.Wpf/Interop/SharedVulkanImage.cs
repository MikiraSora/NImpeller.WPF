using System;

using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace NImpeller.Wpf.Interop;

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
    private readonly KhrExternalMemoryWin32 _extMemWin32;
    private readonly Device _device;
    private readonly PhysicalDevice _physicalDevice;
    private readonly Format _format;
    private readonly ExternalMemoryHandleTypeFlags _handleType;

    private Image _image;
    private DeviceMemory _memory;
    private uint _width;
    private uint _height;
    private bool _disposed;

    public Image VkImage => _image;
    public DeviceMemory VkMemory => _memory;
    public Format Format => _format;
    public uint Width => _width;
    public uint Height => _height;

    public SharedVulkanImage(Vk vk, KhrExternalMemoryWin32 extMemWin32,
        PhysicalDevice physicalDevice, Device device,
        Format format = Format.B8G8R8A8Unorm,
        ExternalMemoryHandleTypeFlags handleType = ExternalMemoryHandleTypeFlags.D3D11TextureKmtBit)
    {
        _vk = vk;
        _extMemWin32 = extMemWin32;
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

        // Per Vulkan spec: memoryTypeIndex used to import external memory must satisfy
        // BOTH the image's memory requirements AND the external-handle's compatible
        // memory types reported by vkGetMemoryWin32HandlePropertiesKHR. Most drivers
        // happen to report the same set (device-local for D3D11 shared textures),
        // but using only the image's typeBits is a spec violation that has bitten
        // people on niche hardware (some Intel iGPU + WARP combos).
        var handleProps = new MemoryWin32HandlePropertiesKHR(StructureType.MemoryWin32HandlePropertiesKhr);
        Check(_extMemWin32.GetMemoryWin32HandleProperties(_device, _handleType, sharedHandle, &handleProps),
            "vkGetMemoryWin32HandlePropertiesKHR");
        uint compatibleTypeBits = requirements.MemoryTypeBits & handleProps.MemoryTypeBits;
        if (compatibleTypeBits == 0)
            throw new InvalidOperationException(
                $"No Vulkan memory type satisfies both image requirements (0x{requirements.MemoryTypeBits:X}) " +
                $"and external handle requirements (0x{handleProps.MemoryTypeBits:X}) for handle type {_handleType}.");

        var importInfo = new ImportMemoryWin32HandleInfoKHR(
            handleType: _handleType,
            handle: sharedHandle);

        var dedicatedAllocateInfo = new MemoryDedicatedAllocateInfo(image: _image);
        var features = GetImageFormatExternalMemoryFeatures(imageInfo, _handleType);
        if (features.HasFlag(ExternalMemoryFeatureFlags.DedicatedOnlyBit))
        {
            importInfo.PNext = &dedicatedAllocateInfo;
            TraceLog.Log("[SharedVulkanImage] driver requires dedicated allocation; chaining MemoryDedicatedAllocateInfo");
        }

        var memoryInfo = new MemoryAllocateInfo(
            pNext: &importInfo,
            allocationSize: requirements.Size,
            memoryTypeIndex: GetMemoryTypeIndex(compatibleTypeBits, MemoryPropertyFlags.DeviceLocalBit));

        Check(_vk.AllocateMemory(_device, &memoryInfo, null, out _memory), "vkAllocateMemory(shared)");
        Check(_vk.BindImageMemory(_device, _image, _memory, 0ul), "vkBindImageMemory(shared)");

        TraceLog.Log($"[SharedVulkanImage] imported VkImage=0x{_image.Handle:X16} memory=0x{_memory.Handle:X16} size={requirements.Size} bytes for {_width}x{_height} {_format}");
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
        if (_image.Handle != 0) { _vk.DestroyImage(_device, _image, null); _image = default; }
        if (_memory.Handle != 0) { _vk.FreeMemory(_device, _memory, null); _memory = default; }
        _width = _height = 0;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DestroyResources();
    }
}
