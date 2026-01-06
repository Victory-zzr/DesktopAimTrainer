using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace DesktopAimTrainer;

/// <summary>
/// 系统图标获取辅助类
/// </summary>
public static class SystemIconHelper
{
    /// <summary>
    /// 根据图标类型获取系统图标
    /// </summary>
    public static BitmapSource? GetSystemIcon(TargetIconType iconType, int size = 48)
    {
        try
        {
            return iconType switch
            {
                TargetIconType.RecycleBin => GetStockIcon(Win32Api.SHSTOCKICONID.SIID_RECYCLER, size),
                TargetIconType.NewFolder => GetStockIcon(Win32Api.SHSTOCKICONID.SIID_FOLDER, size),
                TargetIconType.Excel => GetFileIcon(".xlsx", size),
                TargetIconType.Word => GetFileIcon(".docx", size),
                TargetIconType.TextDocument => GetFileIcon(".txt", size),
                _ => GetStockIcon(Win32Api.SHSTOCKICONID.SIID_FOLDER, size)
            };
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// 获取系统预定义图标（Stock Icon）
    /// </summary>
    private static BitmapSource? GetStockIcon(Win32Api.SHSTOCKICONID iconId, int size)
    {
        var info = new Win32Api.SHSTOCKICONINFO
        {
            cbSize = (uint)Marshal.SizeOf(typeof(Win32Api.SHSTOCKICONINFO))
        };
        
        Win32Api.SHGSI flags = Win32Api.SHGSI.SHGSI_ICON | (size > 32 ? Win32Api.SHGSI.SHGSI_LARGEICON : Win32Api.SHGSI.SHGSI_SMALLICON);
        
        int result = Win32Api.SHGetStockIconInfo(iconId, flags, ref info);
        if (result != 0 || info.hIcon == IntPtr.Zero)
        {
            return null;
        }
        
        try
        {
            return ConvertIconToBitmapSource(info.hIcon, size);
        }
        finally
        {
            Win32Api.DestroyIcon(info.hIcon);
        }
    }
    
    /// <summary>
    /// 根据文件扩展名获取文件图标
    /// </summary>
    private static BitmapSource? GetFileIcon(string extension, int size)
    {
        var shInfo = new Win32Api.SHFILEINFO();
        uint flags = Win32Api.SHGFI_ICON | Win32Api.SHGFI_USEFILEATTRIBUTES;
        
        if (size > 32)
        {
            flags |= Win32Api.SHGFI_LARGEICON;
        }
        else
        {
            flags |= Win32Api.SHGFI_SMALLICON;
        }
        
        IntPtr hIcon = Win32Api.SHGetFileInfo(extension, Win32Api.FILE_ATTRIBUTE_NORMAL, ref shInfo, 
            (uint)Marshal.SizeOf(shInfo), flags);
        
        if (hIcon == IntPtr.Zero || shInfo.hIcon == IntPtr.Zero)
        {
            return null;
        }
        
        try
        {
            return ConvertIconToBitmapSource(shInfo.hIcon, size);
        }
        finally
        {
            Win32Api.DestroyIcon(shInfo.hIcon);
        }
    }
    
    /// <summary>
    /// 将 Windows Icon 句柄转换为 WPF BitmapSource
    /// </summary>
    private static BitmapSource ConvertIconToBitmapSource(IntPtr hIcon, int size)
    {
        // 使用 System.Drawing.Icon 转换
        using (var icon = System.Drawing.Icon.FromHandle(hIcon))
        {
            // 创建指定大小的图标
            using (var bitmap = new Bitmap(size, size))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.DrawImage(icon.ToBitmap(), 0, 0, size, size);
                }
                
                // 转换为 BitmapSource
                return ConvertBitmapToBitmapSource(bitmap);
            }
        }
    }
    
    /// <summary>
    /// 将 System.Drawing.Bitmap 转换为 WPF BitmapSource
    /// </summary>
    private static BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
    {
        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);
        
        try
        {
            var bitmapSource = BitmapSource.Create(
                bitmapData.Width,
                bitmapData.Height,
                bitmap.HorizontalResolution,
                bitmap.VerticalResolution,
                System.Windows.Media.PixelFormats.Bgra32,
                null,
                bitmapData.Scan0,
                bitmapData.Stride * bitmapData.Height,
                bitmapData.Stride);
            
            bitmapSource.Freeze(); // 冻结以提高性能
            return bitmapSource;
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
    }
}

