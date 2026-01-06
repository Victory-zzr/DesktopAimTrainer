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
        // 尝试加载自定义图标，如果失败则使用系统默认图标
        System.Drawing.Icon? customIcon = null;
        try
        {
            // 尝试多个可能的路径
            var paths = new[]
            {
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo.ico"),
                System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Resources", "logo.ico"),
                "Resources\\logo.ico"
            };
            
            foreach (var iconPath in paths)
            {
                if (System.IO.File.Exists(iconPath))
                {
                    customIcon = new System.Drawing.Icon(iconPath);
                    break;
                }
            }
        }
        catch
        {
            // 如果加载失败，使用系统默认图标
        }
        
        _notifyIcon = new NotifyIcon
        {
            Icon = customIcon ?? System.Drawing.SystemIcons.Application,
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

