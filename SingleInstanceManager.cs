using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Collections.Generic;

namespace DesktopAimTrainer;

/// <summary>
/// 单实例应用程序管理器
/// </summary>
public class SingleInstanceManager
{
    private const string MutexName = "DesktopAimTrainer_SingleInstance_Mutex";
    private static Mutex? _mutex;
    
    /// <summary>
    /// 检查是否已有实例运行
    /// </summary>
    public static bool IsAlreadyRunning()
    {
        DebugLogger.WriteLine($"尝试创建 Mutex: {MutexName}");
        bool createdNew;
        _mutex = new Mutex(true, MutexName, out createdNew);
        
        if (!createdNew)
        {
            DebugLogger.WriteLine("Mutex 已存在，说明已有实例运行");
            // 已有实例运行，释放刚创建的 Mutex（因为我们不会使用它）
            try
            {
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
            catch (Exception ex)
            {
                DebugLogger.WriteError("释放 Mutex 失败", ex);
            }
            _mutex = null;
            
            // 激活现有实例的窗口
            DebugLogger.WriteLine("准备调用 ActivateExistingInstance...");
            try
            {
                ActivateExistingInstance();
                DebugLogger.WriteLine("ActivateExistingInstance 调用完成");
            }
            catch (Exception ex)
            {
                DebugLogger.WriteError("ActivateExistingInstance 调用失败", ex);
            }
            DebugLogger.WriteLine("IsAlreadyRunning 返回 true");
            return true;
        }
        
        DebugLogger.WriteLine("Mutex 创建成功，这是第一个实例");
        return false;
    }
    
    /// <summary>
    /// 激活已存在的实例窗口
    /// </summary>
    private static void ActivateExistingInstance()
    {
        try
        {
            // 查找当前进程名（排除当前进程）
            string currentProcessName = Process.GetCurrentProcess().ProcessName;
            var currentProcessId = Process.GetCurrentProcess().Id;
            
            DebugLogger.WriteLine($"开始查找现有实例窗口 - 当前进程名: {currentProcessName}, 当前进程ID: {currentProcessId}");
            
            IntPtr foundWindow = IntPtr.Zero;
            int targetProcessId = 0;
            var allWindows = new List<(IntPtr hWnd, int processId, string title, string className, bool isTopLevel)>();
            
            // 枚举所有窗口，查找同名的其他进程的窗口（包括隐藏的窗口）
            DebugLogger.WriteLine("开始枚举所有窗口...");
            Win32Api.EnumWindows((hWnd, lParam) =>
            {
                Win32Api.GetWindowThreadProcessId(hWnd, out int windowProcessId);
                
                // 查找同名进程但不是当前进程的窗口
                if (windowProcessId != currentProcessId)
                {
                    try
                    {
                        var process = Process.GetProcessById(windowProcessId);
                        if (process.ProcessName == currentProcessName)
                        {
                            // 获取窗口标题
                            var titleSb = new System.Text.StringBuilder(256);
                            Win32Api.GetWindowText(hWnd, titleSb, titleSb.Capacity);
                            string windowTitle = titleSb.ToString();
                            
                            // 获取窗口类名
                            var classSb = new System.Text.StringBuilder(256);
                            Win32Api.GetClassName(hWnd, classSb, classSb.Capacity);
                            string className = classSb.ToString();
                            
                            // 检查是否是顶级窗口（没有父窗口）
                            IntPtr parent = Win32Api.GetParent(hWnd);
                            bool isTopLevel = (parent == IntPtr.Zero);
                            
                            // 收集所有匹配的窗口
                            allWindows.Add((hWnd, windowProcessId, windowTitle, className, isTopLevel));
                            
                            DebugLogger.WriteLine($"找到窗口 - 句柄: {hWnd}, 标题: '{windowTitle}', 类名: '{className}', 顶级窗口: {isTopLevel}");
                        }
                    }
                    catch
                    {
                        // 进程可能已退出，忽略
                    }
                }
                return true; // 继续枚举
            }, IntPtr.Zero);
            
            DebugLogger.WriteLine($"窗口枚举完成，找到 {allWindows.Count} 个匹配的窗口");
            
            // 优先选择：1) 标题为 "FPS 瞄准训练器" 的窗口（主窗口）
            //           2) 有标题的顶级窗口（排除 "Hidden Window" 和其他系统窗口）
            //           3) WPF 窗口类名（HwndWrapper 或类似）且有标题
            //           4) 任何顶级窗口
            //           5) 第一个窗口
            
            // 系统窗口标题列表（需要排除）
            var systemWindowTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Hidden Window",
                ".NET-BroadcastEventWindow",
                "GDI+ Window",
                "MediaContextNotificationWindow",
                "SystemResourceNotifyWindow",
                "CiceroUIWndFrame",
                "MSCTFIME UI",
                "Default IME"
            };
            
            // 首先查找主窗口（标题为 "FPS 瞄准训练器" 或包含 "瞄准训练器"）
            foreach (var (hWnd, processId, title, className, isTopLevel) in allWindows)
            {
                if (isTopLevel && !string.IsNullOrEmpty(title) && 
                    (title == "FPS 瞄准训练器" || title.Contains("瞄准训练器")))
                {
                    foundWindow = hWnd;
                    targetProcessId = processId;
                    DebugLogger.WriteLine($"选择窗口（主窗口）- 句柄: {hWnd}, 标题: '{title}'");
                    break;
                }
            }
            
            // 如果没有找到主窗口，查找有标题的顶级窗口（排除系统窗口）
            if (foundWindow == IntPtr.Zero)
            {
                foreach (var (hWnd, processId, title, className, isTopLevel) in allWindows)
                {
                    if (isTopLevel && !string.IsNullOrEmpty(title) && 
                        !systemWindowTitles.Contains(title) &&
                        !title.StartsWith(".NET-BroadcastEventWindow", StringComparison.OrdinalIgnoreCase))
                    {
                        foundWindow = hWnd;
                        targetProcessId = processId;
                        DebugLogger.WriteLine($"选择窗口（有标题的顶级窗口，排除系统窗口）- 句柄: {hWnd}, 标题: '{title}'");
                        break;
                    }
                }
            }
            
            // 如果没有找到，选择 WPF 窗口类名且有标题的顶级窗口
            if (foundWindow == IntPtr.Zero)
            {
                foreach (var (hWnd, processId, title, className, isTopLevel) in allWindows)
                {
                    if (isTopLevel && className.Contains("HwndWrapper") && 
                        !string.IsNullOrEmpty(title) && 
                        !systemWindowTitles.Contains(title))
                    {
                        foundWindow = hWnd;
                        targetProcessId = processId;
                        DebugLogger.WriteLine($"选择窗口（WPF窗口类名且有标题）- 句柄: {hWnd}, 类名: '{className}', 标题: '{title}'");
                        break;
                    }
                }
            }
            
            // 如果还没有找到，选择任何顶级窗口
            if (foundWindow == IntPtr.Zero)
            {
                foreach (var (hWnd, processId, title, className, isTopLevel) in allWindows)
                {
                    if (isTopLevel)
                    {
                        foundWindow = hWnd;
                        targetProcessId = processId;
                        DebugLogger.WriteLine($"选择窗口（顶级窗口）- 句柄: {hWnd}");
                        break;
                    }
                }
            }
            
            // 最后，如果还是没有找到，使用第一个窗口
            if (foundWindow == IntPtr.Zero && allWindows.Count > 0)
            {
                foundWindow = allWindows[0].hWnd;
                targetProcessId = allWindows[0].processId;
                DebugLogger.WriteLine($"选择窗口（第一个窗口）- 句柄: {foundWindow}");
            }
            
            // 如果找到窗口，激活它
            if (foundWindow != IntPtr.Zero && targetProcessId > 0)
            {
                DebugLogger.WriteLine($"找到现有实例窗口 - 窗口句柄: {foundWindow}, 进程ID: {targetProcessId}, 找到的窗口总数: {allWindows.Count}");
                
                // 注册自定义消息（用于通知现有实例显示窗口）
                uint showWindowMsg = Win32Api.RegisterWindowMessage("DesktopAimTrainer_ShowWindow");
                DebugLogger.WriteLine($"注册窗口消息，消息ID: {showWindowMsg}");
                
                // 允许目标进程设置前台窗口
                bool allowForeground = Win32Api.AllowSetForegroundWindow(targetProcessId);
                DebugLogger.WriteLine($"AllowSetForegroundWindow 结果: {allowForeground}");
                
                // 先发送消息通知现有实例显示窗口（使用 PostMessage 避免阻塞）
                bool messageSent = Win32Api.PostMessage(foundWindow, showWindowMsg, IntPtr.Zero, IntPtr.Zero);
                DebugLogger.WriteLine($"PostMessage 结果: {messageSent}, 消息ID: {showWindowMsg}, 窗口句柄: {foundWindow}");
                
                // 等待足够的时间让消息被处理（WPF 需要更多时间处理消息）
                System.Threading.Thread.Sleep(500);
                
                // 注意：不要直接调用 ShowWindow，让 WndProc 处理窗口显示
                // 只负责将窗口置于前台（如果消息处理成功的话）
                bool foreground = Win32Api.SetForegroundWindow(foundWindow);
                bool bringToTop = Win32Api.BringWindowToTop(foundWindow);
                DebugLogger.WriteLine($"SetForegroundWindow: {foreground}, BringWindowToTop: {bringToTop}");
            }
            else
            {
                DebugLogger.WriteError($"未找到现有实例的窗口 (进程名: {currentProcessName}, 当前进程ID: {currentProcessId}, 找到窗口数: {allWindows.Count})");
            }
        }
        catch (Exception ex)
        {
            // 记录错误以便调试
            DebugLogger.WriteError("激活现有实例失败", ex);
        }
    }
    
    /// <summary>
    /// 通过窗口标题查找窗口
    /// </summary>
    private static IntPtr FindWindowByTitle(string title)
    {
        return Win32Api.FindWindow(null, title);
    }
    
    /// <summary>
    /// 通过进程ID查找窗口
    /// </summary>
    private static IntPtr FindWindowByProcessId(int processId)
    {
        IntPtr foundWindow = IntPtr.Zero;
        var windows = new List<IntPtr>();
        
        Win32Api.EnumWindows((hWnd, lParam) =>
        {
            Win32Api.GetWindowThreadProcessId(hWnd, out int windowProcessId);
            if (windowProcessId == processId)
            {
                windows.Add(hWnd);
            }
            return true;
        }, IntPtr.Zero);
        
        // 返回第一个找到的窗口（通常是主窗口）
        return windows.Count > 0 ? windows[0] : IntPtr.Zero;
    }
    
    /// <summary>
    /// 释放 Mutex
    /// </summary>
    public static void Release()
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        _mutex = null;
    }
}

