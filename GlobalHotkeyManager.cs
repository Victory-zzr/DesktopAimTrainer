using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DesktopAimTrainer;

/// <summary>
/// 全局快捷键管理器
/// </summary>
public class GlobalHotkeyManager : IDisposable
{
    private IntPtr _hookId = IntPtr.Zero;
    private Win32Api.LowLevelKeyboardProc _proc;
    
    public event EventHandler<int>? KeyPressed;
    
    public GlobalHotkeyManager()
    {
        _proc = HookCallback;
        _hookId = SetHook(_proc);
    }
    
    private IntPtr SetHook(Win32Api.LowLevelKeyboardProc proc)
    {
        // 对于低级别钩子，应该传入 null 而不是模块句柄
        return Win32Api.SetWindowsHookEx(
            Win32Api.WH_KEYBOARD_LL,
            proc,
            IntPtr.Zero, // 低级别钩子使用 IntPtr.Zero
            0);
    }
    
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)Win32Api.WM_KEYDOWN)
        {
            var hookStruct = Marshal.PtrToStructure<Win32Api.KBDLLHOOKSTRUCT>(lParam);
            int vkCode = (int)hookStruct.vkCode;
            
            // ESC = 27, F6 = 117, F7 = 118
            if (vkCode == 27 || vkCode == 117 || vkCode == 118)
            {
                KeyPressed?.Invoke(this, vkCode);
            }
        }
        
        return Win32Api.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }
    
    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            Win32Api.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }
}

