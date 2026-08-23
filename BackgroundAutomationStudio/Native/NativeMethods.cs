using System.Runtime.InteropServices;
using System.Text;

namespace BackgroundAutomationStudio.Native;

public static class NativeMethods
{
    public const int WhKeyboardLl = 13;
    public const int WhMouseLl = 14;
    public const int WmKeyDown = 0x0100;
    public const int WmKeyUp = 0x0101;
    public const int WmChar = 0x0102;
    public const int WmSysKeyDown = 0x0104;
    public const int WmSysKeyUp = 0x0105;
    public const int WmLButtonDown = 0x0201;
    public const int WmLButtonUp = 0x0202;
    public const int WmLButtonDblClk = 0x0203;
    public const int WmRButtonDown = 0x0204;
    public const int WmRButtonUp = 0x0205;
    public const int WmMouseMove = 0x0200;
    public const uint MkLButton = 0x0001;
    public const uint MkRButton = 0x0002;
    public const uint GaRoot = 2;
    public const uint MapvkVkToVsc = 0;
    public const uint SwpNoZOrder = 0x0004;
    public const uint SwpNoActivate = 0x0010;
    public const uint SwpNoMove = 0x0002;
    public const uint SwpNoSize = 0x0001;
    public const uint SwpFrameChanged = 0x0020;
    public const int SwRestore = 9;
    public const int SwShowNoActivate = 4;
    public const int GwlExStyle = -20;
    public const long WsExToolWindow = 0x00000080L;
    public const long WsExNoActivate = 0x08000000L;
    public const int SmCxDoubleclk = 36;
    public const int SmCyDoubleclk = 37;
    public const int SmCxDrag = 68;
    public const int SmCyDrag = 69;
    public const int SmXVirtualScreen = 76;
    public const int SmYVirtualScreen = 77;
    public const int SmCxVirtualScreen = 78;
    public const int SmCyVirtualScreen = 79;
    public const uint InputMouse = 0;
    public const uint InputKeyboard = 1;
    public const uint MouseeventfMove = 0x0001;
    public const uint MouseeventfLeftdown = 0x0002;
    public const uint MouseeventfLeftup = 0x0004;
    public const uint MouseeventfRightdown = 0x0008;
    public const uint MouseeventfRightup = 0x0010;
    public const uint MouseeventfAbsolute = 0x8000;
    public const uint MouseeventfVirtualdesk = 0x4000;
    public const uint KeyeventfKeyup = 0x0002;
    public const uint KeyeventfUnicode = 0x0004;

    public delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);
    public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int hookId, HookProc callback, IntPtr module, uint threadId);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr GetModuleHandle(string? moduleName);
    [DllImport("user32.dll")]
    public static extern IntPtr WindowFromPoint(POINT point);
    [DllImport("user32.dll")]
    public static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool PostMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hwnd, int command);
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
    [DllImport("user32.dll")]
    public static extern bool GetClientRect(IntPtr hwnd, out RECT rect);
    [DllImport("user32.dll")]
    public static extern bool ScreenToClient(IntPtr hwnd, ref POINT point);
    [DllImport("user32.dll")]
    public static extern bool ClientToScreen(IntPtr hwnd, ref POINT point);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int maxCount);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetClassName(IntPtr hwnd, StringBuilder className, int maxCount);
    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
    [DllImport("user32.dll")]
    public static extern bool GetGUIThreadInfo(uint threadId, ref GUITHREADINFO info);
    [DllImport("user32.dll")]
    public static extern uint MapVirtualKey(uint code, uint mapType);
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint virtualKey);
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern bool EnumChildWindows(IntPtr parent, EnumWindowsProc callback, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr newValue);
    [DllImport("user32.dll")]
    public static extern uint GetDoubleClickTime();
    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")]
    public static extern bool GetKeyboardState(byte[] keyState);
    [DllImport("user32.dll")]
    public static extern IntPtr GetKeyboardLayout(uint threadId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int ToUnicodeEx(uint virtualKey, uint scanCode, byte[] keyState, StringBuilder buffer, int bufferSize, uint flags, IntPtr keyboardLayout);
    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int virtualKey);
    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint count, INPUT[] inputs, int size);
}
