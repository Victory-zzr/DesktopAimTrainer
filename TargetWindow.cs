using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DesktopAimTrainer;

using WpfImage = System.Windows.Controls.Image;
using WpfPoint = System.Windows.Point;
using WpfBitmapSource = System.Windows.Media.Imaging.BitmapSource;

/// <summary>
/// 悬浮靶子窗口
/// </summary>
public class TargetWindow : Window
{
    private readonly WpfImage _iconImage;
    private readonly DispatcherTimer _hitCheckTimer;
    private bool _isHit;
    
    public event EventHandler? TargetHit;
    
    public TargetIconType IconType { get; }
    public bool IsHit => _isHit;
    
    public TargetWindow(TargetIconType iconType)
    {
        IconType = iconType;
        
        // 窗口设置
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = System.Windows.Media.Brushes.Transparent;
        ShowInTaskbar = false;
        Topmost = true;
        ResizeMode = ResizeMode.NoResize;
        
        // 设置图标大小（系统标准大小）
        var iconSize = Win32Api.GetSystemIconSize();
        Width = iconSize.Width;
        Height = iconSize.Height;
        
        // 创建图标图像
        _iconImage = new WpfImage
        {
            Stretch = Stretch.Uniform,
            Source = LoadIcon(iconType)
        };
        
        Content = _iconImage;
        
        // 设置 Click-through
        Win32Api.SetClickThrough(this);
        
        // 创建命中检测定时器（每16ms检查一次，约60fps）
        _hitCheckTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _hitCheckTimer.Tick += HitCheckTimer_Tick;
        _hitCheckTimer.Start();
    }
    
    private void HitCheckTimer_Tick(object? sender, EventArgs e)
    {
        if (_isHit) return;
        
        var mousePos = Win32Api.GetMousePosition();
        
        // 检查鼠标是否在窗口区域内
        var windowRect = new Rect(
            Left,
            Top,
            Width,
            Height
        );
        
        if (windowRect.Contains(mousePos))
        {
            _isHit = true;
            _hitCheckTimer.Stop();
            TargetHit?.Invoke(this, EventArgs.Empty);
        }
    }
    
    private BitmapSource LoadIcon(TargetIconType iconType)
    {
        // 首先尝试从资源文件加载
        string resourceName = iconType switch
        {
            TargetIconType.RecycleBin => "recycle_bin.png",
            TargetIconType.NewFolder => "new_folder.png",
            TargetIconType.Excel => "excel.png",
            TargetIconType.Word => "word.png",
            TargetIconType.TextDocument => "text_document.png",
            _ => "new_folder.png"
        };
        
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri($"pack://application:,,,/Resources/{resourceName}", UriKind.Absolute);
            bitmap.EndInit();
            return bitmap;
        }
        catch
        {
            // 资源文件不存在，尝试从系统获取图标
            var iconSize = Win32Api.GetSystemIconSize();
            int size = (int)Math.Max(iconSize.Width, iconSize.Height);
            
            var systemIcon = SystemIconHelper.GetSystemIcon(iconType, size);
            if (systemIcon != null)
            {
                return systemIcon;
            }
            
            // 如果系统图标也获取失败，使用占位符
            return CreatePlaceholderIcon();
        }
    }
    
    private WpfBitmapSource CreatePlaceholderIcon()
    {
        // 创建一个简单的彩色方块作为占位符
        var iconSize = Win32Api.GetSystemIconSize();
        int size = (int)Math.Max(iconSize.Width, iconSize.Height);
        
        var drawingVisual = new DrawingVisual();
        using (var drawingContext = drawingVisual.RenderOpen())
        {
            var brush = new SolidColorBrush(
                IconType switch
                {
                    TargetIconType.RecycleBin => Colors.Red,
                    TargetIconType.NewFolder => Colors.Blue,
                    TargetIconType.Excel => Colors.Green,
                    TargetIconType.Word => Colors.DarkBlue,
                    TargetIconType.TextDocument => Colors.Orange,
                    _ => Colors.Gray
                }
            );
            drawingContext.DrawRectangle(brush, null, new Rect(0, 0, size, size));
        }
        
        var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(drawingVisual);
        
        var bitmap = new BitmapImage();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        
        using (var stream = new System.IO.MemoryStream())
        {
            encoder.Save(stream);
            stream.Position = 0;
            bitmap.BeginInit();
            bitmap.StreamSource = stream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
        }
        
        return bitmap;
    }
    
    protected override void OnClosed(EventArgs e)
    {
        _hitCheckTimer?.Stop();
        base.OnClosed(e);
    }
}

/// <summary>
/// 靶子图标类型
/// </summary>
public enum TargetIconType
{
    RecycleBin,
    NewFolder,
    Excel,
    Word,
    TextDocument
}

