namespace DesktopAimTrainer;

/// <summary>
/// 训练模式枚举
/// </summary>
public enum TrainingMode
{
    Count,  // 按量训练
    Time    // 按时间训练
}

/// <summary>
/// TrainingConfig 扩展，用于检查是否有有效配置
/// </summary>
public static class TrainingConfigExtensions
{
    public static bool IsValid(this TrainingConfig config)
    {
        if (config.Mode == TrainingMode.Count)
        {
            return config.TargetHitCount > 0;
        }
        else if (config.Mode == TrainingMode.Time)
        {
            return config.TotalDurationSeconds > 0 && config.TargetStayTimeMs > 0;
        }
        return false;
    }
}

/// <summary>
/// 训练配置
/// </summary>
public class TrainingConfig
{
    public TargetIconType IconType { get; set; } = TargetIconType.NewFolder;
    public TrainingMode Mode { get; set; } = TrainingMode.Count;
    
    // Count Mode 参数
    public int TargetHitCount { get; set; } = 10;
    
    // Time Mode 参数
    public int TotalDurationSeconds { get; set; } = 60;
    public int TargetStayTimeMs { get; set; } = 2000;
}

/// <summary>
/// 训练统计结果
/// </summary>
public class TrainingResult
{
    public int Hits { get; set; }
    public int Misses { get; set; }
    public double TotalTimeSeconds { get; set; }
    public double AverageTimePerTargetSeconds { get; set; }
    public double HitRate => Hits + Misses > 0 ? (double)Hits / (Hits + Misses) : 0;
    public double HitsPerMinute => TotalTimeSeconds > 0 ? (Hits / TotalTimeSeconds) * 60 : 0;
}

