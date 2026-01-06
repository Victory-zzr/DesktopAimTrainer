using System;
using System.IO;
using System.Diagnostics;

namespace DesktopAimTrainer;

/// <summary>
/// 日志级别
/// </summary>
public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Error = 2
}

/// <summary>
/// 调试日志记录器，将调试信息写入文件
/// </summary>
public static class DebugLogger
{
    private static readonly string LogFilePath;
    private static readonly object LockObject = new object();
    
#if DEBUG
    // Debug 版本：记录所有日志
    private static readonly LogLevel CurrentLogLevel = LogLevel.Debug;
#else
    // Release 版本：只记录 Error 级别日志
    private static readonly LogLevel CurrentLogLevel = LogLevel.Error;
#endif
    
    static DebugLogger()
    {
        // 日志文件保存在程序目录下的 Debug.log
        string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        LogFilePath = Path.Combine(appDirectory, "Debug.log");
    }
    
    /// <summary>
    /// 写入调试信息（Debug 版本）
    /// </summary>
    public static void WriteLine(string message)
    {
        WriteLine(LogLevel.Debug, message);
    }
    
    /// <summary>
    /// 写入指定级别的日志
    /// </summary>
    public static void WriteLine(LogLevel level, string message)
    {
        // Release 版本只记录 Error 级别
        if (level < CurrentLogLevel)
        {
            return;
        }
        
        try
        {
            lock (LockObject)
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string levelStr = level == LogLevel.Error ? "ERROR" : level == LogLevel.Info ? "INFO" : "DEBUG";
                string logMessage = $"[{timestamp}] [{levelStr}] {message}";
                
                // Debug 版本输出到 Debug，Release 版本只输出 Error
#if DEBUG
                Debug.WriteLine(logMessage);
#else
                if (level == LogLevel.Error)
                {
                    Debug.WriteLine(logMessage);
                }
#endif
                
                // 追加到日志文件并立即刷新
                using (var writer = new StreamWriter(LogFilePath, true, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine(logMessage);
                    writer.Flush(); // 立即刷新，确保日志写入磁盘
                }
            }
        }
        catch (Exception ex)
        {
            // 如果写入日志失败，尝试输出到 Debug（避免影响程序运行）
            Debug.WriteLine($"日志写入失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 写入错误信息（始终记录）
    /// </summary>
    public static void WriteError(string message, Exception? ex = null)
    {
        string errorMessage = message;
        if (ex != null)
        {
            errorMessage += $"\n{ex.Message}\n{ex.StackTrace}";
        }
        WriteLine(LogLevel.Error, errorMessage);
    }
    
    /// <summary>
    /// 清空日志文件
    /// </summary>
    public static void Clear()
    {
        try
        {
            lock (LockObject)
            {
                if (File.Exists(LogFilePath))
                {
                    File.Delete(LogFilePath);
                }
            }
        }
        catch
        {
            // 忽略错误
        }
    }
}

