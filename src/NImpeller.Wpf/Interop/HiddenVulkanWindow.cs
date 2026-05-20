using System;
using System.Runtime.InteropServices;

namespace NImpeller.Wpf.Interop;

/// <summary>
/// A 1×1 invisible top-level window used purely to obtain a VkSurfaceKHR
/// (through VK_KHR_win32_surface). Impeller's Vulkan backend requires a
/// VkSurfaceKHR to create its swapchain; we never present to this window —
/// vkQueuePresentKHR is hooked and turned into a blit into the shared
/// D3D-Vulkan texture instead.
///
/// Multiple <c>VkSurfaceKHR</c> instances may be created from the same HWND,
/// which is why the library keeps a single shared instance of this window in
/// <see cref="ImpellerSharedHost"/> rather than one per ImpellerView.
/// </summary>
internal sealed class HiddenVulkanWindow : IDisposable
{
    private const string ClassName = "NImpellerWpfHiddenVk";
    private const uint WS_POPUP = 0x80000000;
    private const uint WS_EX_TOOLWINDOW = 0x00000080;
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

    [DllImport("user32")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_NOREDRAW = 0x0008;

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
                // Allocate the class-name string just for the duration of RegisterClassExW;
                // Windows copies it into its internal class table, so we can free immediately
                // after registration and avoid a permanent process-level leak.
                var classNamePtr = Marshal.StringToHGlobalUni(ClassName);
                try
                {
                    var wc = new WNDCLASSEXW
                    {
                        cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                        style = CS_OWNDC,
                        lpfnWndProc = Marshal.GetFunctionPointerForDelegate(s_wndProc),
                        hInstance = _hinstance,
                        lpszClassName = classNamePtr,
                    };
                    if (RegisterClassExW(ref wc) == 0)
                    {
                        int err = Marshal.GetLastWin32Error();
                        if (err != 0x582 /* ERROR_CLASS_ALREADY_EXISTS */)
                            throw new InvalidOperationException($"RegisterClassExW failed (err={err})");
                    }
                    s_classRegistered = true;
                }
                finally
                {
                    Marshal.FreeHGlobal(classNamePtr);
                }
            }
        }

        _hwnd = CreateWindowExW(
            WS_EX_TOOLWINDOW, ClassName, "NImpellerWpfHiddenVk", WS_POPUP,
            -32000, -32000, Math.Max(1, width), Math.Max(1, height),
            IntPtr.Zero, IntPtr.Zero, _hinstance, IntPtr.Zero);

        if (_hwnd == IntPtr.Zero)
            throw new InvalidOperationException($"CreateWindowExW failed (err={Marshal.GetLastWin32Error()})");

        TraceLog.Log($"[HiddenVulkanWindow] HWND = 0x{(long)_hwnd:X16} size={width}x{height}");
    }

    public void Dispose()
    {
        if (_hwnd != IntPtr.Zero)
        {
            DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }
    }

    /// <summary>Resize the underlying HWND so vkGetPhysicalDeviceSurfaceCapabilitiesKHR reports the new currentExtent.</summary>
    public void Resize(int width, int height)
    {
        if (_hwnd == IntPtr.Zero) return;
        SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, Math.Max(1, width), Math.Max(1, height),
            SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_NOREDRAW);
    }
}
