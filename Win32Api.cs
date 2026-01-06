using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DesktopAimTrainer;

using WpfPoint = System.Windows.Point;
using WpfSize = System.Windows.Size;

/// <summary>
/// Win32 API 封装，用于全局快捷键、Click-through窗口、鼠标位置获取
/// </summary>
public static class Win32Api
{
    #region Constants
    
    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int WS_EX_LAYERED = 0x00080000;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_TOPMOST = 0x00000008;
    public const int GWL_EXSTYLE = -20;
    
    public const int WH_KEYBOARD_LL = 13;
    public const int WM_KEYDOWN = 0x0100;
    public const int WM_KEYUP = 0x0101;
    
    public const int MOD_NONE = 0x0000;
    public const int MOD_ALT = 0x0001;
    public const int MOD_CONTROL = 0x0002;
    public const int MOD_SHIFT = 0x0004;
    public const int MOD_WIN = 0x0008;
    
    #endregion
    
    #region Structs
    
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
    
    #endregion
    
    #region P/Invoke Declarations
    
    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);
    
    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    
    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    
    [DllImport("user32.dll")]
    public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
    
    [DllImport("user32.dll")]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
    
    [DllImport("user32.dll")]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);
    
    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    
    [DllImport("kernel32.dll")]
    public static extern IntPtr GetModuleHandle(string lpModuleName);
    
    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);
    
    public const int SM_CXICON = 11;
    public const int SM_CYICON = 12;
    
    // Shell API for getting system icons
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);
    
    [DllImport("shell32.dll")]
    public static extern int SHGetStockIconInfo(SHSTOCKICONID siid, SHGSI uFlags, ref SHSTOCKICONINFO psii);
    
    public const uint SHGFI_ICON = 0x000000100;
    public const uint SHGFI_LARGEICON = 0x000000000;
    public const uint SHGFI_SMALLICON = 0x000000001;
    public const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
    public const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
    
    public const uint SHGSI_ICON = 0x000000100;
    public const uint SHGSI_LARGEICON = 0x000000000;
    public const uint SHGSI_SMALLICON = 0x000000001;
    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }
    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct SHSTOCKICONINFO
    {
        public uint cbSize;
        public IntPtr hIcon;
        public int iSysImageIndex;
        public int iIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szPath;
    }
    
    public enum SHSTOCKICONID : uint
    {
        SIID_RECYCLER = 31,
        SIID_FOLDER = 3,
        SIID_DOCNOASSOC = 0,
        SIID_DOCASSOC = 1,
    }
    
    [Flags]
    public enum SHGSI : uint
    {
        SHGSI_ICONLOCATION = 0,
        SHGSI_ICON = 0x000000100,
        SHGSI_SYSICONINDEX = 0x000004000,
        SHGSI_LINKOVERLAY = 0x000008000,
        SHGSI_SELECTED = 0x000010000,
        SHGSI_LARGEICON = 0x000000000,
        SHGSI_SMALLICON = 0x000000001,
        SHGSI_SHELLICONSIZE = 0x000000004
    }
    
    [DllImport("user32.dll")]
    public static extern bool DestroyIcon(IntPtr hIcon);
    
    #endregion
    
    #region Delegates
    
    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// 获取当前鼠标位置（屏幕坐标）
    /// </summary>
    public static WpfPoint GetMousePosition()
    {
        GetCursorPos(out POINT point);
        return new WpfPoint(point.X, point.Y);
    }
    
    /// <summary>
    /// 设置窗口为 Click-through（鼠标穿透）
    /// </summary>
    public static void SetClickThrough(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            window.SourceInitialized += (s, e) =>
            {
                hwnd = new WindowInteropHelper(window).Handle;
                SetClickThroughInternal(hwnd);
            };
        }
        else
        {
            SetClickThroughInternal(hwnd);
        }
    }
    
    private static void SetClickThroughInternal(IntPtr hwnd)
    {
        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_TOPMOST;
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
    }
    
    /// <summary>
    /// 获取系统标准图标大小
    /// </summary>
    public static WpfSize GetSystemIconSize()
    {
        int width = GetSystemMetrics(SM_CXICON);
        int height = GetSystemMetrics(SM_CYICON);
        return new WpfSize(width, height);
    }
    
    #endregion
}

