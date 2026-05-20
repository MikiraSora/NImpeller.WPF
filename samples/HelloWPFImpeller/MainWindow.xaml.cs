using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

using HelloWPFImpeller.Interop;
using HelloWPFImpeller.Scenes;

using NImpeller;

using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace HelloWPFImpeller;

public partial class MainWindow : Window
{
    private const string WindowTitleBase = "HelloWPFImpeller";

    private readonly D3DResources _d3dResources = new();
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private TimeSpan _lastRenderTime = TimeSpan.MinValue;

    private ImpellerContext? _impellerContext;
    private ImpellerContextVulkanInfo? _impellerVulkanInfo;
    private Vk? _vk;
    private SharedVulkanImage? _sharedVkImage;
    private HiddenVulkanWindow? _hiddenWindow;
    private ImpellerVulkanSwapchain? _impellerSwapchain;
    private SurfaceKHR _vkSurface;
    private KhrSurface? _khrSurface;
    private ImpellerTypographyContext? _typography;

    // DPI / pixel scale
    private double _dpiScaleX = 1.0;
    private double _dpiScaleY = 1.0;
    private D3DImage? _d3dImage;

    // FPS tracking
    private int _frameCountThisSecond;
    private double _lastFpsUpdateSec;
    private float _displayFps;

    public MainWindow()
    {
        InitializeComponent();
        Title = WindowTitleBase;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // ---- DPI detection ----
        var src = PresentationSource.FromVisual(this);
        if (src?.CompositionTarget != null)
        {
            var m = src.CompositionTarget.TransformToDevice;
            _dpiScaleX = m.M11;
            _dpiScaleY = m.M22;
        }
        App.Log($"[DPI] scale = {_dpiScaleX:0.###} x {_dpiScaleY:0.###}");

        // ---- D3DImage with matching DPI (so PixelWidth=physical px maps 1:1) ----
        _d3dImage = new D3DImage(96.0 * _dpiScaleX, 96.0 * _dpiScaleY);
        OutputImage.Source = _d3dImage;

        // ---- D3D shared texture sized in PHYSICAL pixels ----
        var hwnd = new WindowInteropHelper(this).Handle;
        _d3dResources.Initialize(hwnd);
        App.Log($"[D3D] Adapter LUID = {_d3dResources.AdapterLuid.High:X8}:{_d3dResources.AdapterLuid.Low:X8}");

        // Use the actual rendered area of the Image control (which is the WPF client area),
        // not the Window's outer ActualWidth/Height which includes title bar + borders.
        double dipW = OutputImage.ActualWidth > 0 ? OutputImage.ActualWidth : ActualWidth;
        double dipH = OutputImage.ActualHeight > 0 ? OutputImage.ActualHeight : ActualHeight;
        var pxW = Math.Max(1, (uint)Math.Round(dipW * _dpiScaleX));
        var pxH = Math.Max(1, (uint)Math.Round(dipH * _dpiScaleY));
        App.Log($"[D3D] Render target = {pxW}x{pxH} physical px (OutputImage {dipW}x{dipH} DIP, Window {ActualWidth}x{ActualHeight} DIP)");
        _d3dResources.CreateOrResizeRenderTarget(pxW, pxH);

        _d3dImage.Lock();
        _d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _d3dResources.BackbufferSurfaceHandle);
        _d3dImage.Unlock();

        InitializeImpeller();

        CompositionTarget.Rendering += OnRendering;
    }

    private void InitializeImpeller()
    {
        try
        {
            VkProcInterceptor.Initialize();
        }
        catch (Exception ex)
        {
            App.Log($"[Impeller] Vulkan loader initialization failed: {ex.Message}");
            return;
        }

        VkTrampolines.TargetAdapterLuid = ((ulong)(uint)_d3dResources.AdapterLuid.High << 32)
                                          | (ulong)(uint)_d3dResources.AdapterLuid.Low;
        App.Log($"[Impeller] Target adapter LUID = 0x{VkTrampolines.TargetAdapterLuid:X16}");

        _impellerContext = ImpellerContext.CreateVulkanNew(
            VkProcInterceptor.GetProcAddress,
            enableValidation: false);

        if (_impellerContext == null)
        {
            App.Log("[Impeller] CreateVulkanNew returned null. Cannot continue.");
            return;
        }

        var info = _impellerContext.GetVulkanInfo();
        if (info == null)
        {
            App.Log("[Impeller] GetVulkanInfo returned null.");
            return;
        }

        _impellerVulkanInfo = info;
        App.Log("[Impeller] Vulkan context created:");
        App.Log($"  VkInstance       = 0x{(long)info.Value.Vk_instance:X16}");
        App.Log($"  VkPhysicalDevice = 0x{(long)info.Value.Vk_physical_device:X16}");
        App.Log($"  VkDevice         = 0x{(long)info.Value.Vk_logical_device:X16}");
        App.Log($"  QueueFamilyIndex = {info.Value.Graphics_queue_family_index}");
        App.Log($"  QueueIndex       = {info.Value.Graphics_queue_index}");

        InitializeSharedVulkanImage(info.Value);
    }

    private void InitializeSharedVulkanImage(ImpellerContextVulkanInfo info)
    {
        try
        {
            _vk = Vk.GetApi();
            var physical = new PhysicalDevice(info.Vk_physical_device);
            var device = new Device(info.Vk_logical_device);
            _sharedVkImage = new SharedVulkanImage(_vk, physical, device);
            _sharedVkImage.Import(_d3dResources.VulkanSharedHandle, _d3dResources.Width, _d3dResources.Height);

            _vk.GetDeviceQueue(device, info.Graphics_queue_family_index, info.Graphics_queue_index, out var queue);
            App.Log($"[Impeller] VkQueue = 0x{queue.Handle:X16}");
            _sharedVkImage.InitializeCommandResources(queue, info.Graphics_queue_family_index);

            InitializeImpellerSwapchain(info);
        }
        catch (Exception ex)
        {
            App.Log($"[SharedVulkanImage] import failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private unsafe void InitializeImpellerSwapchain(ImpellerContextVulkanInfo info)
    {
        _hiddenWindow = new HiddenVulkanWindow();
        _hiddenWindow.Create((int)_d3dResources.Width, (int)_d3dResources.Height);

        var instance = new Instance(info.Vk_instance);
        if (!_vk!.TryGetInstanceExtension<KhrWin32Surface>(instance, out var khrWin32))
        {
            App.Log("[Impeller] VK_KHR_win32_surface not available on the instance");
            return;
        }
        if (!_vk.TryGetInstanceExtension<KhrSurface>(instance, out _khrSurface))
        {
            App.Log("[Impeller] VK_KHR_surface not available on the instance");
            return;
        }

        var surfaceInfo = new Win32SurfaceCreateInfoKHR(
            sType: StructureType.Win32SurfaceCreateInfoKhr,
            hwnd: _hiddenWindow.Hwnd,
            hinstance: _hiddenWindow.HInstance);

        var r = khrWin32.CreateWin32Surface(instance, &surfaceInfo, null, out _vkSurface);
        if (r != Result.Success)
        {
            App.Log($"[Impeller] vkCreateWin32SurfaceKHR failed: {r}");
            return;
        }
        App.Log($"[Impeller] VkSurfaceKHR = 0x{_vkSurface.Handle:X16}");

        _impellerSwapchain = _impellerContext!.VulkanSwapchainCreateNew(new IntPtr((long)_vkSurface.Handle));
        if (_impellerSwapchain == null)
        {
            App.Log("[Impeller] ImpellerVulkanSwapchainCreateNew returned null");
            return;
        }
        App.Log("[Impeller] Vulkan swapchain created successfully.");

        _typography = ImpellerTypographyContext.New();
        if (_typography == null)
            App.Log("[Impeller] WARNING: TypographyContext.New returned null; text overlay disabled.");

        var device = new Device(info.Vk_logical_device);
        _vk!.GetDeviceQueue(device, info.Graphics_queue_family_index, info.Graphics_queue_index, out var queue);
        VkTrampolines.InstallBlitResources(
            _vk, device, queue, info.Graphics_queue_family_index,
            _sharedVkImage!.VkImage,
            new Extent2D(_d3dResources.Width, _d3dResources.Height));
    }

    private bool _firstImpellerFrameLogged;
    private long _frameNumber;

    private void OnRendering(object? sender, EventArgs e)
    {
        if (_d3dImage == null || !_d3dImage.IsFrontBufferAvailable) return;

        var args = (RenderingEventArgs)e;
        if (_lastRenderTime == args.RenderingTime) return;
        _lastRenderTime = args.RenderingTime;

        _d3dImage.Lock();
        try
        {
            if (_impellerSwapchain != null)
            {
                RenderImpellerFrame();
            }
            else if (_sharedVkImage != null)
            {
                // Fallback (Impeller didn't init): plain Vulkan clear so something still moves.
                var t = (float)_stopwatch.Elapsed.TotalSeconds;
                _sharedVkImage.ClearViaVulkan(
                    0.5f + 0.5f * MathF.Sin(t * 1.2f),
                    0.5f + 0.5f * MathF.Sin(t * 0.9f + 1.0f),
                    0.5f + 0.5f * MathF.Sin(t * 1.5f + 2.0f),
                    1.0f);
            }
            else
            {
                _d3dResources.ClearForDebug(0x40, 0x40, 0x40);
            }
            _d3dImage.AddDirtyRect(new Int32Rect(0, 0, _d3dImage.PixelWidth, _d3dImage.PixelHeight));
        }
        catch (Exception ex)
        {
            if (!_firstImpellerFrameLogged)
            {
                _firstImpellerFrameLogged = true;
                App.Log($"[Render] First-frame Impeller render failed: {ex.GetType().Name}: {ex.Message}");
            }
        }
        finally
        {
            _d3dImage.Unlock();
        }

        UpdateFps();
    }

    private void UpdateFps()
    {
        _frameCountThisSecond++;
        double now = _stopwatch.Elapsed.TotalSeconds;
        if (now - _lastFpsUpdateSec >= 1.0)
        {
            _displayFps = MathF.Round(_frameCountThisSecond / (float)(now - _lastFpsUpdateSec));
            _frameCountThisSecond = 0;
            _lastFpsUpdateSec = now;
            Title = $"{WindowTitleBase} — {_displayFps:F2} fps @ {_d3dResources.Width}x{_d3dResources.Height} (DPI {_dpiScaleX:0.##}x)";
        }
    }

    private void RenderImpellerFrame()
    {
        int w = (int)_d3dResources.Width;
        int h = (int)_d3dResources.Height;
        var rect = new ImpellerRect(0, 0, w, h);

        using var builder = ImpellerDisplayListBuilder.New(rect)
                            ?? throw new InvalidOperationException("ImpellerDisplayListBuilder.New returned null");

        var t = (float)_stopwatch.Elapsed.TotalSeconds;
        // Scene receives PHYSICAL pixel dimensions and the DPI scale so font sizes and
        // stroke widths can be multiplied to keep visual size constant across DPIs.
        HelloDemoScene.Render(builder, _typography, t, w, h, ++_frameNumber, (float)_dpiScaleX);

        using var displayList = builder.CreateDisplayListNew()
                                ?? throw new InvalidOperationException("CreateDisplayListNew returned null");

        using var surface = _impellerSwapchain!.AcquireNextSurfaceNew()
                            ?? throw new InvalidOperationException("AcquireNextSurfaceNew returned null");
        surface.DrawDisplayList(displayList);
        surface.Present();

        if (!_firstImpellerFrameLogged)
        {
            _firstImpellerFrameLogged = true;
            App.Log($"[Render] First Impeller frame rendered + blitted. BlitFrameCounter={VkTrampolines.BlitFrameCounter}");
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        CompositionTarget.Rendering -= OnRendering;

        if (_vk != null && _impellerVulkanInfo.HasValue)
        {
            try { _vk.DeviceWaitIdle(new Device(_impellerVulkanInfo.Value.Vk_logical_device)); }
            catch (Exception ex) { App.Log($"[Closed] vkDeviceWaitIdle threw: {ex.Message}"); }
        }

        _typography?.Dispose();
        _typography = null;
        _impellerSwapchain?.Dispose();
        _impellerSwapchain = null;
        _sharedVkImage?.Dispose();
        _sharedVkImage = null;
        _impellerContext?.Dispose();
        _impellerContext = null;
        _hiddenWindow?.Dispose();
        _hiddenWindow = null;
        _d3dResources.Dispose();

        App.Log("--- HelloWPFImpeller shut down cleanly ---");
    }
}
