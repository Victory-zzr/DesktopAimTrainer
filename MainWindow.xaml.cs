using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DesktopAimTrainer;

public partial class MainWindow : Window
{
    private TrainingSession? _currentSession;
    private TrainingConfig _lastConfig = new();
    private TrainingResult? _lastResult;
    
    public MainWindow()
    {
        InitializeComponent();
        LocalizationManager.Initialize();
        LocalizationManager.LanguageChanged += LocalizationManager_LanguageChanged;
        UpdateUI();
    }
    
    private void LocalizationManager_LanguageChanged(object? sender, EventArgs e)
    {
        UpdateUI();
    }
    
    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string langCode)
        {
            LocalizationManager.SetLanguage(langCode);
        }
    }
    
    private void UpdateUI()
    {
        if (LanguageLabel == null) return; // 确保控件已初始化
        
        // 窗口标题
        Title = LocalizationManager.GetString("WindowTitle");
        
        // 语言选择标签
        LanguageLabel.Text = LocalizationManager.GetString("Language");
        
        // 配置区 - 通过名称查找
        var configGroupBox = FindName("ConfigGroupBox") as System.Windows.Controls.GroupBox;
        if (configGroupBox != null)
        {
            configGroupBox.Header = LocalizationManager.GetString("TrainingConfig");
        }
        
        // 图标类型
        IconTypeLabel.Text = LocalizationManager.GetString("IconType");
        RecycleBinItem.Content = LocalizationManager.GetString("RecycleBin");
        NewFolderItem.Content = LocalizationManager.GetString("NewFolder");
        ExcelItem.Content = LocalizationManager.GetString("Excel");
        WordItem.Content = LocalizationManager.GetString("Word");
        TextDocumentItem.Content = LocalizationManager.GetString("TextDocument");
        
        // 训练模式
        TrainingModeLabel.Text = LocalizationManager.GetString("TrainingMode");
        CountModeItem.Content = LocalizationManager.GetString("CountMode");
        TimeModeItem.Content = LocalizationManager.GetString("TimeMode");
        
        // 参数标签
        TargetHitCountLabel.Text = LocalizationManager.GetString("TargetHitCount");
        TotalDurationLabel.Text = LocalizationManager.GetString("TotalDuration");
        TargetStayTimeLabel.Text = LocalizationManager.GetString("TargetStayTime");
        
        // 按钮
        StartButton.Content = LocalizationManager.GetString("StartTraining");
        
        // 统计区
        StatsGroupBox.Header = LocalizationManager.GetString("TrainingStats");
        NoDataTextBlock.Text = LocalizationManager.GetString("NoData");
        
        // 快捷键提示
        HotkeysTextBlock.Text = LocalizationManager.GetString("Hotkeys");
        
        // 如果有训练结果，更新统计显示
        if (_lastResult != null)
        {
            UpdateStatsDisplay(_lastResult);
        }
    }
    
    private void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 检查控件是否已初始化（避免在 InitializeComponent 期间访问未初始化的控件）
        if (CountModePanel == null || TimeModePanel == null || ModeComboBox == null)
            return;
            
        bool isCountMode = ModeComboBox.SelectedIndex == 0;
        CountModePanel.Visibility = isCountMode ? Visibility.Visible : Visibility.Collapsed;
        TimeModePanel.Visibility = isCountMode ? Visibility.Collapsed : Visibility.Visible;
    }
    
    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !IsTextAllowed(e.Text);
    }
    
    private static bool IsTextAllowed(string text)
    {
        return Array.TrueForAll(text.ToCharArray(), c => char.IsDigit(c));
    }
    
    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        var config = CreateConfigFromUI();
        if (config == null)
        {
            System.Windows.MessageBox.Show(
                LocalizationManager.GetString("InvalidInput"), 
                LocalizationManager.GetString("Error"), 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
            return;
        }
        
        _lastConfig = config;
        StartTraining(config);
    }
    
    private TrainingConfig? CreateConfigFromUI()
    {
        var config = new TrainingConfig
        {
            IconType = (TargetIconType)IconTypeComboBox.SelectedIndex,
            Mode = ModeComboBox.SelectedIndex == 0 ? TrainingMode.Count : TrainingMode.Time
        };
        
        if (config.Mode == TrainingMode.Count)
        {
            if (!int.TryParse(TargetHitCountTextBox.Text, out int count) || count <= 0)
            {
                return null;
            }
            config.TargetHitCount = count;
        }
        else
        {
            if (!int.TryParse(TotalDurationTextBox.Text, out int duration) || duration <= 0)
            {
                return null;
            }
            if (!int.TryParse(TargetStayTimeTextBox.Text, out int stayTime) || stayTime <= 0)
            {
                return null;
            }
            config.TotalDurationSeconds = duration;
            config.TargetStayTimeMs = stayTime;
        }
        
        return config;
    }
    
    public void StartTraining(TrainingConfig config)
    {
        // 停止当前训练（如果有）
        if (_currentSession != null && _currentSession.IsRunning)
        {
            _currentSession.Stop();
        }
        
        _currentSession = new TrainingSession(config);
        _currentSession.TrainingCompleted += Session_TrainingCompleted;
        
        // 最小化窗口到托盘
        WindowState = WindowState.Minimized;
        ShowInTaskbar = false;
        
        _currentSession.Start();
    }
    
    private void Session_TrainingCompleted(object? sender, TrainingResult result)
    {
        _lastResult = result;
        
        // 更新统计显示
        Dispatcher.Invoke(() =>
        {
            UpdateStatsDisplay(result);
        });
    }
    
    public void ShowLastResult()
    {
        if (_lastResult != null)
        {
            UpdateStatsDisplay(_lastResult);
        }
        else
        {
            StatsPanel.Children.Clear();
            StatsPanel.Children.Add(new TextBlock
            {
                Text = LocalizationManager.GetString("NoData"),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Foreground = System.Windows.Media.Brushes.Gray,
                FontSize = 14
            });
        }
        
        WindowState = WindowState.Normal;
        ShowInTaskbar = true;
        Activate();
    }
    
    private void UpdateStatsDisplay(TrainingResult result)
    {
        StatsPanel.Children.Clear();
        
        var stats = new StackPanel { Margin = new Thickness(0, 5, 0, 5) };
        var seconds = LocalizationManager.GetString("Seconds");
        
        if (_lastConfig.Mode == TrainingMode.Count)
        {
            stats.Children.Add(CreateStatTextBlock($"{LocalizationManager.GetString("Hits")} {result.Hits}"));
            stats.Children.Add(CreateStatTextBlock($"{LocalizationManager.GetString("TotalTime")} {result.TotalTimeSeconds:F2} {seconds}"));
            stats.Children.Add(CreateStatTextBlock($"{LocalizationManager.GetString("AverageTime")} {result.AverageTimePerTargetSeconds:F2} {seconds}"));
        }
        else
        {
            stats.Children.Add(CreateStatTextBlock($"{LocalizationManager.GetString("Hits")} {result.Hits}"));
            stats.Children.Add(CreateStatTextBlock($"{LocalizationManager.GetString("Misses")} {result.Misses}"));
            stats.Children.Add(CreateStatTextBlock($"{LocalizationManager.GetString("HitRate")} {result.HitRate:P2}"));
            stats.Children.Add(CreateStatTextBlock($"Hits/Minute: {result.HitsPerMinute:F2}"));
            stats.Children.Add(CreateStatTextBlock($"{LocalizationManager.GetString("TotalTime")} {result.TotalTimeSeconds:F2} {seconds}"));
        }
        
        StatsPanel.Children.Add(stats);
    }
    
    private TextBlock CreateStatTextBlock(string text)
    {
        return new TextBlock
        {
            Text = text,
            Margin = new Thickness(0, 5, 0, 5),
            FontSize = 13
        };
    }
    
    public void StopCurrentTraining()
    {
        if (_currentSession != null && _currentSession.IsRunning)
        {
            _currentSession.Stop();
        }
    }
    
    public void QuickStart()
    {
        if (_lastConfig.IsValid())
        {
            StartTraining(_lastConfig);
        }
    }
}
