using System.Runtime.InteropServices;

namespace WukongBenchmarkTool.Native;

/// <summary>
/// Тонкая обёртка над user32.dll SendInput для симуляции кликов мыши и нажатий клавиш,
/// плюс чтение текущей позиции курсора и состояния кнопки мыши (для calibration-режима).
/// Координаты клика — абсолютные экранные пиксели.
/// </summary>
public static class InputSimulator
{
    private const int InputMouse = 0;
    private const int InputKeyboard = 1;
    private const uint MouseEventAbsolute = 0x8000;
    private const uint MouseEventMove = 0x0001;
    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp = 0x0004;
    private const uint KeyEventKeyUp = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public int Type;
        public InputUnion Union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MouseInput Mouse;
        [FieldOffset(0)] public KeyboardInput Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort Vk;
        public ushort Scan;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint numberOfInputs, Input[] inputs, int sizeOfInputStruct);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKeyCode);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    public static (int Width, int Height) GetPrimaryScreenSize() =>
        (GetSystemMetrics(SM_CXSCREEN), GetSystemMetrics(SM_CYSCREEN));

    public static (int X, int Y) GetCursorPosition()
    {
        GetCursorPos(out var p);
        return (p.X, p.Y);
    }

    public static bool IsLeftMouseButtonDown() => (GetAsyncKeyState(0x01) & 0x8000) != 0;

    public static bool IsKeyDown(ushort virtualKeyCode) => (GetAsyncKeyState(virtualKeyCode) & 0x8000) != 0;

    public static void ClickAt(int x, int y)
    {
        var (screenW, screenH) = GetPrimaryScreenSize();
        var absX = (int)(x * 65535.0 / Math.Max(1, screenW - 1));
        var absY = (int)(y * 65535.0 / Math.Max(1, screenH - 1));

        var move = new Input
        {
            Type = InputMouse,
            Union = new InputUnion
            {
                Mouse = new MouseInput { Dx = absX, Dy = absY, Flags = MouseEventAbsolute | MouseEventMove },
            },
        };
        var down = new Input
        {
            Type = InputMouse,
            Union = new InputUnion
            {
                Mouse = new MouseInput { Dx = absX, Dy = absY, Flags = MouseEventAbsolute | MouseEventLeftDown },
            },
        };
        var up = new Input
        {
            Type = InputMouse,
            Union = new InputUnion
            {
                Mouse = new MouseInput { Dx = absX, Dy = absY, Flags = MouseEventAbsolute | MouseEventLeftUp },
            },
        };

        SendInput(1, [move], Marshal.SizeOf<Input>());
        Thread.Sleep(50);
        SendInput(1, [down], Marshal.SizeOf<Input>());
        Thread.Sleep(50);
        SendInput(1, [up], Marshal.SizeOf<Input>());
    }

    public static void PressKey(ushort virtualKeyCode)
    {
        var down = new Input { Type = InputKeyboard, Union = new InputUnion { Keyboard = new KeyboardInput { Vk = virtualKeyCode } } };
        var up = new Input
        {
            Type = InputKeyboard,
            Union = new InputUnion { Keyboard = new KeyboardInput { Vk = virtualKeyCode, Flags = KeyEventKeyUp } },
        };

        SendInput(1, [down], Marshal.SizeOf<Input>());
        Thread.Sleep(50);
        SendInput(1, [up], Marshal.SizeOf<Input>());
    }
}
