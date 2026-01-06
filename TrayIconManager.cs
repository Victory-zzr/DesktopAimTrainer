using System;
using System.Windows;
using System.Windows.Forms;

namespace DesktopAimTrainer;

/// <summary>
/// 系统托盘图标管理器
/// </summary>
public class TrayIconManager : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private MainWindow? _mainWindow;
    
    public event EventHandler? DoubleClick;
    
    public TrayIconManager(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        InitializeTrayIcon();
    }
    
    private void InitializeTrayIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "FPS 瞄准训练器",
            Visible = true
        };
        
        _notifyIcon.DoubleClick += (sender, e) =>
        {
            DoubleClick?.Invoke(this, EventArgs.Empty);
        };
        
        // 创建上下文菜单
        var contextMenu = new ContextMenuStrip();
        var showMenuItem = new ToolStripMenuItem("显示主窗口");
        showMenuItem.Click += (sender, e) =>
        {
            DoubleClick?.Invoke(this, EventArgs.Empty);
        };
        contextMenu.Items.Add(showMenuItem);
        
        var exitMenuItem = new ToolStripMenuItem("退出");
        exitMenuItem.Click += (sender, e) =>
        {
            System.Windows.Application.Current.Shutdown();
        };
        contextMenu.Items.Add(exitMenuItem);
        
        _notifyIcon.ContextMenuStrip = contextMenu;
    }
    
    public void Dispose()
    {
        _notifyIcon?.Dispose();
        _notifyIcon = null;
    }
}

