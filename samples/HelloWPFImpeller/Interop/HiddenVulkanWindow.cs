using System;
using System.Runtime.InteropServices;

namespace HelloWPFImpeller.Interop;

/// <summary>
/// A 1×1 invisible top-level window used purely to obtain a VkSurfaceKHR
/// (through VK_KHR_win32_surface). Impeller's Vulkan backend requires a
/// VkSurfaceKHR to create its swapchain; we never present to this window —
/// vkQueuePresentKHR is hooked and turned into a blit into the shared
/// D3D-Vulkan texture instead.
/// </summary>
internal sealed class HiddenVulkanWindow : IDisposable
{
    private const string ClassName = "HelloWPFImpellerHiddenVk";
    private const uint WS_POPUP = 0x80000000;
    private const uint WS_EX_TOOLWINDOW = 0x00000080;
    private const int  HWND_MESSAGE = -3;
    private const uint CS_OWNDC = 0x0020;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEXW
    {
        public uint cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public IntPtr lpszMenuName;
        public IntPtr lpszClassName;
        public IntPtr hIconSm;
    }

    [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassExW(ref WNDCLASSEXW lpwcx);

    [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowExW(
        uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32")]
    private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32")]
    private static extern IntPtr GetModuleHandleW(string? lpModuleName);

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private static readonly WndProcDelegate s_wndProc = DefWindowProcWrapped;
    private static IntPtr DefWindowProcWrapped(IntPtr h, uint m, IntPtr w, IntPtr l) => DefWindowProcW(h, m, w, l);

    private static bool s_classRegistered;
    private static readonly object s_lock = new();
    private IntPtr _hwnd;
    private IntPtr _hinstance;

    public IntPtr Hwnd => _hwnd;
    public IntPtr HInstance => _hinstance;

    public void Create(int width, int height)
    {
        if (_hwnd != IntPtr.Zero) return;
        _hinstance = GetModuleHandleW(null);

        lock (s_lock)
        {
            if (!s_classRegistered)
            {
                var wc = new WNDCLASSEXW
                {
                    cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                    style = CS_OWNDC,
                    lpfnWndProc = Marshal.GetFunctionPointerForDelegate(s_wndProc),
                    hInstance = _hinstance,
                    lpszClassName = Marshal.StringToHGlobalUni(ClassName),
                };
                if (RegisterClassExW(ref wc) == 0)
                {
                    int err = Marshal.GetLastWin32Error();
                    if (err != 0x582 /* ERROR_CLASS_ALREADY_EXISTS */)
                        throw new InvalidOperationException($"RegisterClassExW failed (err={err})");
                }
                s_classRegistered = true;
            }
        }

        _hwnd = CreateWindowExW(
            WS_EX_TOOLWINDOW, ClassName, "HelloWPFImpellerHiddenVk", WS_POPUP,
            -32000, -32000, Math.Max(1, width), Math.Max(1, height),
            IntPtr.Zero, IntPtr.Zero, _hinstance, IntPtr.Zero);

        if (_hwnd == IntPtr.Zero)
            throw new InvalidOperationException($"CreateWindowExW failed (err={Marshal.GetLastWin32Error()})");

        App.Log($"[HiddenVulkanWindow] HWND = 0x{(long)_hwnd:X16} size={width}x{height}");
    }

    public void Dispose()
    {
        if (_hwnd != IntPtr.Zero)
        {
            DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }
    }
}
