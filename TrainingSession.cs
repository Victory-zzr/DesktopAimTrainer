using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace DesktopAimTrainer;

using WpfSize = System.Windows.Size;

/// <summary>
/// 训练会话管理器
/// </summary>
public class TrainingSession
{
    private readonly TrainingConfig _config;
    private readonly TargetGenerator _generator;
    private readonly List<TargetWindow> _activeTargets = new();
    private readonly Stopwatch _stopwatch = new Stopwatch();
    private readonly DispatcherTimer? _timeModeTimer;
    
    private TrainingResult _result = new();
    private TargetWindow? _currentTarget;
    private bool _isRunning;
    
    public event EventHandler<TrainingResult>? TrainingCompleted;
    public event EventHandler<TargetWindow>? TargetCreated;
    
    public bool IsRunning => _isRunning;
    public TrainingResult Result => _result;
    
    public TrainingSession(TrainingConfig config)
    {
        _config = config;
        _generator = new TargetGenerator();
        
        // Time Mode 需要定时器
        if (_config.Mode == TrainingMode.Time)
        {
            _timeModeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_config.TargetStayTimeMs)
            };
            _timeModeTimer.Tick += TimeModeTimer_Tick;
        }
    }
    
    public void Start()
    {
        if (_isRunning) return;
        
        _isRunning = true;
        _generator.Reset();
        _result = new TrainingResult();
        _stopwatch.Restart();
        
        // 生成第一个靶子
        CreateNextTarget();
        
        // 启动 Time Mode 定时器
        if (_config.Mode == TrainingMode.Time)
        {
            _timeModeTimer?.Start();
        }
    }
    
    public void Stop()
    {
        if (!_isRunning) return;
        
        _isRunning = false;
        _stopwatch.Stop();
        _timeModeTimer?.Stop();
        
        // 清理所有靶子
        foreach (var target in _activeTargets)
        {
            target.Close();
        }
        _activeTargets.Clear();
        _currentTarget = null;
    }
    
    private void CreateNextTarget()
    {
        if (!_isRunning) return;
        
        // 检查训练是否应该结束
        if (_config.Mode == TrainingMode.Count)
        {
            if (_result.Hits >= _config.TargetHitCount)
            {
                CompleteTraining();
                return;
            }
        }
        else if (_config.Mode == TrainingMode.Time)
        {
            if (_stopwatch.Elapsed.TotalSeconds >= _config.TotalDurationSeconds)
            {
                CompleteTraining();
                return;
            }
        }
        
        // 生成新靶子
        var screenSize = new WpfSize(
            SystemParameters.PrimaryScreenWidth,
            SystemParameters.PrimaryScreenHeight
        );
        var targetSize = Win32Api.GetSystemIconSize();
        var position = _generator.GenerateNextPosition(screenSize, targetSize);
        
        var target = new TargetWindow(_config.IconType);
        target.Left = position.X;
        target.Top = position.Y;
        target.TargetHit += Target_TargetHit;
        target.Show();
        
        _currentTarget = target;
        _activeTargets.Add(target);
        TargetCreated?.Invoke(this, target);
        
        // 重置 Time Mode 定时器
        if (_config.Mode == TrainingMode.Time)
        {
            _timeModeTimer?.Stop();
            _timeModeTimer?.Start();
        }
    }
    
    private void Target_TargetHit(object? sender, EventArgs e)
    {
        if (!_isRunning || sender is not TargetWindow target) return;
        
        _result.Hits++;
        
        // 计算平均时间（Count Mode）
        if (_config.Mode == TrainingMode.Count)
        {
            _result.AverageTimePerTargetSeconds = _stopwatch.Elapsed.TotalSeconds / _result.Hits;
        }
        
        // 移除当前靶子
        target.Close();
        _activeTargets.Remove(target);
        _currentTarget = null;
        
        // 生成下一个靶子
        CreateNextTarget();
    }
    
    private void TimeModeTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isRunning || _currentTarget == null) return;
        
        // 超时未命中
        if (!_currentTarget.IsHit)
        {
            _result.Misses++;
            _currentTarget.Close();
            _activeTargets.Remove(_currentTarget);
            _currentTarget = null;
        }
        
        // 生成下一个靶子
        CreateNextTarget();
    }
    
    private void CompleteTraining()
    {
        _isRunning = false;
        _stopwatch.Stop();
        _timeModeTimer?.Stop();
        
        _result.TotalTimeSeconds = _stopwatch.Elapsed.TotalSeconds;
        
        // 清理所有靶子
        foreach (var target in _activeTargets)
        {
            target.Close();
        }
        _activeTargets.Clear();
        _currentTarget = null;
        
        TrainingCompleted?.Invoke(this, _result);
    }
}

