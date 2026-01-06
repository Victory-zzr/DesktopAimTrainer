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
        
        try
        {
            // 创建主窗口
            _mainWindow = new MainWindow();
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
        // 取消关闭，改为隐藏
        e.Cancel = true;
        _mainWindow!.WindowState = WindowState.Minimized;
        _mainWindow.ShowInTaskbar = false;
    }
    
    private void TrayIcon_DoubleClick(object? sender, EventArgs e)
    {
        if (_mainWindow != null)
        {
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.ShowInTaskbar = true;
            _mainWindow.Activate();
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
        
        base.OnExit(e);
    }
}
