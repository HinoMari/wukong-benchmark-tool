using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WukongBenchmarkTool.Native;

public static class WindowHelper
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    public static async Task<IntPtr> WaitForMainWindowAsync(Process process, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            process.Refresh();
            if (process.HasExited)
            {
                return IntPtr.Zero;
            }

            if (process.MainWindowHandle != IntPtr.Zero)
            {
                return process.MainWindowHandle;
            }

            await Task.Delay(500);
        }

        return IntPtr.Zero;
    }

    public static (int X, int Y, int Width, int Height)? GetWindowBounds(IntPtr windowHandle)
    {
        if (!GetWindowRect(windowHandle, out var rect))
        {
            return null;
        }

        return (rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    public static void Focus(IntPtr windowHandle) => SetForegroundWindow(windowHandle);
}
