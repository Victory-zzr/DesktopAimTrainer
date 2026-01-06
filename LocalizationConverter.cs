using System;
using System.Globalization;
using System.Windows.Data;

namespace DesktopAimTrainer;

/// <summary>
/// 本地化转换器（用于 XAML 绑定）
/// </summary>
public class LocalizationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is string key)
        {
            return LocalizationManager.GetString(key);
        }
        return value?.ToString() ?? string.Empty;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

