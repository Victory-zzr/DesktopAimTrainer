using System;
using System.Globalization;
using System.Resources;
using System.Windows;

namespace DesktopAimTrainer;

/// <summary>
/// 本地化管理器
/// </summary>
public static class LocalizationManager
{
    private static ResourceManager? _resourceManager;
    private static CultureInfo _currentCulture = new CultureInfo("zh-CN");
    
    public static event EventHandler? LanguageChanged;
    
    public static CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (_currentCulture.Name != value.Name)
            {
                _currentCulture = value;
                LanguageChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }
    
    public static void Initialize()
    {
        _resourceManager = new ResourceManager("DesktopAimTrainer.Resources.Strings", typeof(LocalizationManager).Assembly);
    }
    
    public static string GetString(string key)
    {
        if (_resourceManager == null)
        {
            Initialize();
        }
        
        try
        {
            var value = _resourceManager?.GetString(key, _currentCulture);
            return value ?? key;
        }
        catch
        {
            return key;
        }
    }
    
    public static void SetLanguage(string languageCode)
    {
        CurrentCulture = new CultureInfo(languageCode);
    }
}

