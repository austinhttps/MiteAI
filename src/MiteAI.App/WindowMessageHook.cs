using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;

namespace MiteAI.App;

public class WindowMessageHook : IDisposable
{
    public delegate IntPtr SubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, uint dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    public static extern bool SetWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, IntPtr uIdSubclass, uint dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    public static extern bool RemoveWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, IntPtr uIdSubclass);

    [DllImport("comctl32.dll", SetLastError = true)]
    public static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    private SubclassProc _proc;
    private IntPtr _hWnd;
    public event EventHandler<uint>? HotKeyPressed;

    public WindowMessageHook(IntPtr hWnd)
    {
        _hWnd = hWnd;
        _proc = WindowProc;
        SetWindowSubclass(hWnd, _proc, IntPtr.Zero, 0);
    }

    private IntPtr WindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, uint dwRefData)
    {
        const uint WM_HOTKEY = 0x0312;
        if (uMsg == WM_HOTKEY)
        {
            HotKeyPressed?.Invoke(this, (uint)wParam);
        }
        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hWnd != IntPtr.Zero)
        {
            RemoveWindowSubclass(_hWnd, _proc, IntPtr.Zero);
            _hWnd = IntPtr.Zero;
        }
    }
}