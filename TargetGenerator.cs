using System;
using System.Windows;

namespace DesktopAimTrainer;

using WpfPoint = System.Windows.Point;
using WpfSize = System.Windows.Size;

/// <summary>
/// 靶子生成器，负责生成符合规则的位置
/// </summary>
public class TargetGenerator
{
    private readonly Random _random = new Random();
    private WpfPoint? _lastTargetPosition;
    private WpfPoint? _lastMousePosition;
    
    private const double VERTICAL_CENTER_RATIO_MIN = 0.30;
    private const double VERTICAL_CENTER_RATIO_MAX = 0.40;
    private const double MIN_DISTANCE_FROM_LAST = 100; // 像素
    private const double MIN_DISTANCE_FROM_MOUSE = 80; // 像素
    private const double CORE_ZONE_WEIGHT = 0.7; // 核心区权重70%
    
    /// <summary>
    /// 生成下一个靶子位置
    /// </summary>
    public WpfPoint GenerateNextPosition(WpfSize screenSize, WpfSize targetSize)
    {
        // 更新鼠标位置
        _lastMousePosition = Win32Api.GetMousePosition();
        
        // 计算纵向中央区域
        double verticalCenterHeight = screenSize.Height * (VERTICAL_CENTER_RATIO_MIN + 
            (VERTICAL_CENTER_RATIO_MAX - VERTICAL_CENTER_RATIO_MIN) * _random.NextDouble());
        double verticalCenterTop = (screenSize.Height - verticalCenterHeight) / 2;
        double verticalCenterBottom = verticalCenterTop + verticalCenterHeight;
        
        // 计算横向区域（核心区和余光区）
        double screenWidth = screenSize.Width;
        double coreZoneLeft = screenWidth * 0.25;
        double coreZoneRight = screenWidth * 0.75;
        double coreZoneWidth = coreZoneRight - coreZoneLeft;
        
        WpfPoint candidate;
        int attempts = 0;
        const int maxAttempts = 50;
        
        do
        {
            // 决定生成在核心区还是余光区
            bool useCoreZone = _random.NextDouble() < CORE_ZONE_WEIGHT;
            
            double x;
            if (useCoreZone)
            {
                // 核心区：25% - 75% 之间
                x = coreZoneLeft + _random.NextDouble() * coreZoneWidth;
            }
            else
            {
                // 余光区：随机选择左侧或右侧
                if (_random.NextDouble() < 0.5)
                {
                    // 左侧余光区：0 - 25%
                    x = _random.NextDouble() * coreZoneLeft;
                }
                else
                {
                    // 右侧余光区：75% - 100%
                    x = coreZoneRight + _random.NextDouble() * (screenWidth - coreZoneRight);
                }
            }
            
            // Y坐标在纵向中央区域内随机
            double y = verticalCenterTop + _random.NextDouble() * (verticalCenterBottom - verticalCenterTop - targetSize.Height);
            
            candidate = new WpfPoint(x, y);
            attempts++;
            
        } while (!IsValidPosition(candidate, targetSize, screenSize) && attempts < maxAttempts);
        
        _lastTargetPosition = candidate;
        return candidate;
    }
    
    /// <summary>
    /// 检查位置是否有效（满足距离约束）
    /// </summary>
    private bool IsValidPosition(WpfPoint position, WpfSize targetSize, WpfSize screenSize)
    {
        // 检查是否超出屏幕范围
        if (position.X < 0 || position.Y < 0 ||
            position.X + targetSize.Width > screenSize.Width ||
            position.Y + targetSize.Height > screenSize.Height)
        {
            return false;
        }
        
        // 检查与上一个靶子的距离
        if (_lastTargetPosition.HasValue)
        {
            double distanceFromLast = Math.Abs(position.X - _lastTargetPosition.Value.X);
            if (distanceFromLast < MIN_DISTANCE_FROM_LAST)
            {
                return false;
            }
        }
        
        // 检查与鼠标的距离
        if (_lastMousePosition.HasValue)
        {
            double dx = position.X - _lastMousePosition.Value.X;
            double dy = position.Y - _lastMousePosition.Value.Y;
            double distanceFromMouse = Math.Sqrt(dx * dx + dy * dy);
            
            if (distanceFromMouse < MIN_DISTANCE_FROM_MOUSE)
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 重置生成器状态（开始新训练时调用）
    /// </summary>
    public void Reset()
    {
        _lastTargetPosition = null;
        _lastMousePosition = null;
    }
}

