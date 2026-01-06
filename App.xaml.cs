using System;
using System.Windows;
using System.Windows.Forms;
using WinFormsApp = System.Windows.Forms.Application;

namespace DesktopAimTrainer;

public partial class App : System.Windows.Application
{
    private GlobalHotkeyManager? _hotkeyManager;
    private TrayIconManager? _trayIconManager;
    private MainWindow? _mainWindow;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        DebugLogger.WriteLine($"=== 程序启动 === 进程ID: {System.Diagnostics.Process.GetCurrentProcess().Id}");
        
        // 检查是否已有实例运行（必须在创建窗口之前检查）
        DebugLogger.WriteLine("检查是否已有实例运行...");
        if (SingleInstanceManager.IsAlreadyRunning())
        {
            DebugLogger.WriteLine("检测到已有实例运行，准备退出当前进程");
            // 等待足够的时间确保日志写入完成
            System.Threading.Thread.Sleep(500);
            // 已有实例运行，立即退出当前实例，不创建任何窗口
            // 使用 Environment.Exit 确保立即退出，不等待消息循环
            Environment.Exit(0);
            return;
        }
        
        DebugLogger.WriteLine("未检测到已有实例，继续启动新实例");
        
        try
        {
            // 创建主窗口
            _mainWindow = new MainWindow();
            // 确保窗口初始状态不显示在任务栏（除非需要显示时）
            _mainWindow.ShowInTaskbar = true; // 首次启动时显示在任务栏
            _mainWindow.Show();
            _mainWindow.Activate(); // 确保窗口激活并显示在前台
            
            // 初始化托盘图标
            _trayIconManager = new TrayIconManager(_mainWindow);
            _trayIconManager.DoubleClick += TrayIcon_DoubleClick;
            
            // 初始化全局快捷键（如果失败不影响程序运行）
            try
            {
                _hotkeyManager = new GlobalHotkeyManager();
                if (_hotkeyManager != null)
                {
                    _hotkeyManager.KeyPressed += HotkeyManager_KeyPressed;
                }
            }
            catch (Exception ex)
            {
                // 快捷键初始化失败不影响程序运行，只记录错误
                System.Diagnostics.Debug.WriteLine($"全局快捷键初始化失败: {ex.Message}");
            }
            
            // 处理窗口关闭事件（最小化到托盘而不是关闭）
            _mainWindow.Closing += MainWindow_Closing;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"程序启动失败: {ex.Message}\n\n{ex.StackTrace}", 
                "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }
    
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // 取消关闭，改为完全隐藏到托盘
        e.Cancel = true;
        _mainWindow!.Hide(); // 完全隐藏窗口，不在桌面显示
        _mainWindow.ShowInTaskbar = false; // 不在任务栏显示
    }
    
    private void TrayIcon_DoubleClick(object? sender, EventArgs e)
    {
        ShowMainWindow();
    }
    
    /// <summary>
    /// 显示主窗口（供外部调用，如单实例激活）
    /// </summary>
    public void ShowMainWindow()
    {
        if (_mainWindow != null)
        {
            _mainWindow.Dispatcher.Invoke(() =>
            {
                _mainWindow.Show(); // 先显示窗口（如果被隐藏）
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.ShowInTaskbar = true;
                _mainWindow.Activate();
                _mainWindow.Focus();
            });
        }
    }
    
    private void HotkeyManager_KeyPressed(object? sender, int vkCode)
    {
        if (_mainWindow == null) return;
        
        // ESC = 27, F6 = 117, F7 = 118
        switch (vkCode)
        {
            case 27: // ESC - 终止训练
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    _mainWindow.StopCurrentTraining();
                });
                break;
                
            case 117: // F6 - 查看结果
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    _mainWindow.ShowLastResult();
                });
                break;
                
            case 118: // F7 - 快速开始
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    _mainWindow.QuickStart();
                });
                break;
        }
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        // 清理资源
        _hotkeyManager?.Dispose();
        _trayIconManager?.Dispose();
        SingleInstanceManager.Release();
        
        base.OnExit(e);
    }
}
