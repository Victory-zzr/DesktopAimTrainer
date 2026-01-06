using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace DesktopAimTrainer;

public partial class MainWindow : Window
{
    private TrainingSession? _currentSession;
    private TrainingConfig _lastConfig = new();
    private TrainingResult? _lastResult;
    
    private HwndSource? _hwndSource;
    private uint _showWindowMessage;
    
    public MainWindow()
    {
        InitializeComponent();
        
        // 设置窗口图标
        SetWindowIcon();
        
        LocalizationManager.Initialize();
        LocalizationManager.LanguageChanged += LocalizationManager_LanguageChanged;
        UpdateUI();
        
        // 注册窗口消息处理
        SourceInitialized += MainWindow_SourceInitialized;
    }
    
    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        // 获取窗口句柄并注册消息处理
        var helper = new WindowInteropHelper(this);
        helper.EnsureHandle();
        IntPtr hwnd = helper.Handle;
        _hwndSource = HwndSource.FromHwnd(hwnd);
        
        if (_hwndSource != null)
        {
            // 注册自定义消息（必须在两个进程中注册相同的消息名称才能匹配）
            _showWindowMessage = Win32Api.RegisterWindowMessage("DesktopAimTrainer_ShowWindow");
            _hwndSource.AddHook(WndProc);
            DebugLogger.WriteLine($"窗口消息处理已注册，消息ID: {_showWindowMessage}, 窗口句柄: {hwnd}, 进程ID: {System.Diagnostics.Process.GetCurrentProcess().Id}");
        }
        else
        {
            DebugLogger.WriteError($"无法获取 HwndSource，窗口句柄: {hwnd}");
        }
    }
    
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // 处理显示窗口消息
#if DEBUG
        // Debug 版本记录所有消息
        DebugLogger.WriteLine($"WndProc 收到消息: msg={msg}, _showWindowMessage={_showWindowMessage}, hwnd={hwnd}");
#endif
        
        if (_showWindowMessage != 0 && msg == _showWindowMessage)
        {
            DebugLogger.WriteLine($"收到显示窗口消息，当前窗口状态 - IsVisible: {IsVisible}, ShowInTaskbar: {ShowInTaskbar}, WindowState: {WindowState}");
            
            try
            {
                // 检查是否在 UI 线程上
                if (Dispatcher.CheckAccess())
                {
                    // 已经在 UI 线程上，直接执行
                    DebugLogger.WriteLine("在 UI 线程上，直接调用 ShowWindowInternal");
                    ShowWindowInternal();
                }
                else
                {
                    // 不在 UI 线程上，使用 BeginInvoke 异步执行
                    DebugLogger.WriteLine("不在 UI 线程上，使用 BeginInvoke");
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ShowWindowInternal();
                    }), System.Windows.Threading.DispatcherPriority.Normal);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.WriteError("处理显示窗口消息失败", ex);
            }
            
            handled = true;
            return new IntPtr(1); // 返回非零值表示消息已处理
        }
        
        return IntPtr.Zero;
    }
    
    private void ShowWindowInternal()
    {
        try
        {
            DebugLogger.WriteLine($"开始显示窗口... 当前状态 - IsVisible: {IsVisible}, ShowInTaskbar: {ShowInTaskbar}, WindowState: {WindowState}, Width: {Width}, Height: {Height}");
            
            // 确保窗口大小正确（防止显示为巨大窗口）
            if (Width <= 0 || Height <= 0 || Width > 2000 || Height > 2000)
            {
                DebugLogger.WriteLine($"窗口大小异常，重置为默认大小 - Width: {Width}, Height: {Height}");
                Width = 400;
                Height = 500;
            }
            
            // 确保窗口完全显示
            if (!IsVisible)
            {
                Show();
            }
            
            WindowState = WindowState.Normal;
            ShowInTaskbar = true;
            
            // 确保窗口位置合理（如果窗口位置异常，居中显示）
            if (Left < -1000 || Top < -1000 || Left > SystemParameters.VirtualScreenWidth || Top > SystemParameters.VirtualScreenHeight)
            {
                DebugLogger.WriteLine($"窗口位置异常，重置为居中 - Left: {Left}, Top: {Top}");
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            
            Activate();
            Focus();
            BringIntoView();
            
            DebugLogger.WriteLine($"窗口显示完成 - IsVisible: {IsVisible}, ShowInTaskbar: {ShowInTaskbar}, WindowState: {WindowState}, Width: {Width}, Height: {Height}, Left: {Left}, Top: {Top}");
        }
        catch (Exception ex)
        {
            DebugLogger.WriteError("显示窗口失败", ex);
        }
    }
    
    private void SetWindowIcon()
    {
        try
        {
            // 尝试加载自定义图标
            var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo.ico");
            if (!System.IO.File.Exists(iconPath))
            {
                iconPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Resources", "logo.ico");
            }
            
            if (System.IO.File.Exists(iconPath))
            {
                using (var stream = System.IO.File.OpenRead(iconPath))
                {
                    var icon = new System.Drawing.Icon(stream);
                    var bitmap = System.Drawing.Icon.FromHandle(icon.Handle).ToBitmap();
                    var hBitmap = bitmap.GetHbitmap();
                    try
                    {
                        var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap,
                            IntPtr.Zero,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        bitmapSource.Freeze();
                        Icon = bitmapSource;
                    }
                    finally
                    {
                        Win32Api.DeleteObject(hBitmap);
                        bitmap.Dispose();
                        icon.Dispose();
                    }
                }
            }
        }
        catch
        {
            // 如果加载失败，使用默认图标（不设置，使用系统默认）
        }
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
        
        // 完全隐藏窗口到托盘
        Hide();
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
        
        Show(); // 先显示窗口（如果被隐藏）
        WindowState = WindowState.Normal;
        ShowInTaskbar = true;
        Activate();
        Focus();
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
