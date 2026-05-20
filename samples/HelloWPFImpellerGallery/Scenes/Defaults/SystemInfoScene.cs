using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class SystemInfoScene : IGalleryScene
{
    public string Name => "System Info";
    public string? Description => "Impeller version, Vulkan API, selected GPU, memory heaps, current view metrics";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x18, 0x1C, 0x24);
        if (e.Typography == null) return;

        var info = ImpellerSystemInfo.GpuInfo;

        float x = 40 * e.DpiScaleX;
        float y = 30 * e.DpiScaleY;
        float lineH = 24 * e.DpiScaleY;

        TextBasicsScene.DrawSimpleText(b, e.Typography, "Impeller × Vulkan × GPU", 30 * e.DpiScaleX,
            x, y, e.PixelWidth, ImpellerColor.FromRgb(0xFF, 0xFF, 0xFF),
            weight: ImpellerFontWeight.kImpellerFontWeight700);
        y += 50 * e.DpiScaleY;

        if (info == null)
        {
            TextBasicsScene.DrawSimpleText(b, e.Typography, "ImpellerSystemInfo.GpuInfo is null (host not initialized yet).",
                14 * e.DpiScaleX, x, y, e.PixelWidth - (int)x,
                ImpellerColor.FromRgb(0xE8, 0x70, 0x70));
            return;
        }

        var groupHeader = ImpellerColor.FromRgb(0x6F, 0xC2, 0xE8);
        var labelColor = ImpellerColor.FromRgb(0xA0, 0xA8, 0xB2);
        var valueColor = ImpellerColor.FromRgb(0xE8, 0xE8, 0xE8);

        void DrawHeader(string text)
        {
            TextBasicsScene.DrawSimpleText(b, e.Typography!, text, 16 * e.DpiScaleX,
                x, y, e.PixelWidth - (int)x, groupHeader,
                weight: ImpellerFontWeight.kImpellerFontWeight700);
            y += lineH + 4 * e.DpiScaleY;
        }
        void DrawRow(string label, string value)
        {
            TextBasicsScene.DrawSimpleText(b, e.Typography!, label, 14 * e.DpiScaleX,
                x + 18 * e.DpiScaleX, y, 200, labelColor);
            TextBasicsScene.DrawSimpleText(b, e.Typography!, value, 14 * e.DpiScaleX,
                x + 230 * e.DpiScaleX, y, e.PixelWidth - 280, valueColor,
                weight: ImpellerFontWeight.kImpellerFontWeight500);
            y += lineH;
        }

        // === Impeller ===
        DrawHeader("Impeller");
        DrawRow("API version",     $"{info.ImpellerApiVersion}  (raw 0x{info.ImpellerApiVersionRaw:X8})");
        DrawRow("Backend",          "Vulkan");
        y += 8 * e.DpiScaleY;

        // === Vulkan ===
        DrawHeader("Vulkan");
        DrawRow("API version",      $"{info.VulkanApiVersion}  (raw 0x{info.VulkanApiVersionRaw:X8})");
        DrawRow("Driver version",   $"0x{info.DriverVersionRaw:X8}");
        DrawRow("Instance",         $"0x{(long)info.VkInstance:X16}");
        DrawRow("Physical device",  $"0x{(long)info.VkPhysicalDevice:X16}");
        DrawRow("Logical device",   $"0x{(long)info.VkDevice:X16}");
        DrawRow("Queue",            $"0x{(long)info.VkQueue:X16}  family {info.QueueFamilyIndex}  index {info.QueueIndex}");
        y += 8 * e.DpiScaleY;

        // === GPU ===
        DrawHeader("GPU");
        DrawRow("Vendor",           $"{info.VendorName}  (id 0x{info.VendorId:X4})");
        DrawRow("Device name",      info.DeviceName);
        DrawRow("Device ID",        $"0x{info.DeviceId:X4}");
        DrawRow("Device type",      info.DeviceType);
        DrawRow("D3D adapter LUID", $"0x{info.AdapterLuid:X16}");
        DrawRow("DeviceLocal mem",  FormatBytes(info.DeviceLocalMemoryBytes));
        DrawRow("HostVisible mem",  FormatBytes(info.HostVisibleMemoryBytes));
        DrawRow("Max 2D image",     $"{info.MaxImageDimension2D} × {info.MaxImageDimension2D}");
        DrawRow("Max framebuffer",  $"{info.MaxFramebufferWidth} × {info.MaxFramebufferHeight}");
        y += 8 * e.DpiScaleY;

        // === Current ImpellerView ===
        DrawHeader("Current ImpellerView");
        DrawRow("Pixel size",       $"{e.PixelWidth} × {e.PixelHeight}");
        DrawRow("DPI scale",        $"X:{e.DpiScaleX:0.###}× Y:{e.DpiScaleY:0.###}x");
        DrawRow("Frame number",     e.FrameNumber.ToString("N0"));
        DrawRow("Frame delta",      $"{e.DeltaTime.TotalMilliseconds:0.00} ms");
        DrawRow("Total time",       $"{e.TotalTime.TotalSeconds:0.0} s");
    }

    private static string FormatBytes(ulong bytes)
    {
        if (bytes == 0) return "—";
        if (bytes >= 1L << 30) return $"{bytes / (double)(1L << 30):0.00} GiB";
        if (bytes >= 1L << 20) return $"{bytes / (double)(1L << 20):0.0} MiB";
        if (bytes >= 1L << 10) return $"{bytes / (double)(1L << 10):0.0} KiB";
        return $"{bytes} B";
    }
}
