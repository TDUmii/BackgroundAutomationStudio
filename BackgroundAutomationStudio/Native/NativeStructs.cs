using System.Runtime.InteropServices;

namespace BackgroundAutomationStudio.Native;

[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;
    public POINT(int x, int y) { X = x; Y = y; }
}

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
    public int Width => Right - Left;
    public int Height => Bottom - Top;
}

[StructLayout(LayoutKind.Sequential)]
public struct GUITHREADINFO
{
    public uint Size;
    public uint Flags;
    public IntPtr Active;
    public IntPtr Focus;
    public IntPtr Capture;
    public IntPtr MenuOwner;
    public IntPtr MoveSize;
    public IntPtr Caret;
    public RECT CaretRect;
}

[StructLayout(LayoutKind.Sequential)]
public struct MSLLHOOKSTRUCT
{
    public POINT Point;
    public uint MouseData;
    public uint Flags;
    public uint Time;
    public UIntPtr ExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
public struct KBDLLHOOKSTRUCT
{
    public uint VkCode;
    public uint ScanCode;
    public uint Flags;
    public uint Time;
    public UIntPtr ExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
public struct INPUT
{
    public uint Type;
    public INPUTUNION Data;
}

[StructLayout(LayoutKind.Explicit)]
public struct INPUTUNION
{
    [FieldOffset(0)] public MOUSEINPUT Mouse;
    [FieldOffset(0)] public KEYBDINPUT Keyboard;
}

[StructLayout(LayoutKind.Sequential)]
public struct MOUSEINPUT
{
    public int X;
    public int Y;
    public uint MouseData;
    public uint Flags;
    public uint Time;
    public UIntPtr ExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
public struct KEYBDINPUT
{
    public ushort VirtualKey;
    public ushort ScanCode;
    public uint Flags;
    public uint Time;
    public UIntPtr ExtraInfo;
}
