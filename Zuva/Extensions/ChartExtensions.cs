using cAlgo.API;
using Zuva.Models;

namespace Mwenje.Extensions;

public static class ChartExtensions
{
    public static void DrawStraightLine(this Chart chart, string id, DateTime startTime, double startPrice, DateTime endTime, double endPrice, string label = null, LineStyle lineStyle = LineStyle.Solid, Color color = null, bool hasLabel = false, bool removeExisting = false, bool extended = false)
    {
        color ??= Color.Wheat;

        if (removeExisting)
        {
            chart.RemoveObject(id);
        }
        
        var line = chart.DrawTrendLine(id, startTime, startPrice, endTime, endPrice, color, 1, lineStyle);
        
        if (extended)
        {
            line.ExtendToInfinity = true;
        }

        if (!hasLabel) return;
        
        chart.RemoveObject($"{id}-label");
        var text = chart.DrawText($"{id}-label", label, startTime, startPrice, color);
        text.HorizontalAlignment = HorizontalAlignment.Right;
    }
    
    public static void DrawCycle(this Chart chart, List<TimeRange> cycles, DateTime time)
    {
        var cycle = cycles.FirstOrDefault(c => c.StartTime == time.TimeOfDay);
        if (cycle != null)
        {
            chart.DrawVerticalLine("cycle-" + time, time, Color.Gray, 1, LineStyle.DotsRare);
        }
    }
    
    public static void DrawTrendLine(this Chart chart, string id, SwingPoint startPoint, SwingPoint endPoint, LineType lineType)
    {
        var lineStyle = lineType.GetLineStyle();
            
        chart.DrawTrendLine($"{id}-{lineStyle}-{startPoint.Time}", startPoint.Time, startPoint.Price, endPoint.Time, startPoint.Price, lineStyle.color, 1, lineStyle.style);
    }
    
    public static void DrawTrendLine(this Chart chart, string id, Level level, LineType lineType)
    {
        var lineStyle = lineType.GetLineStyle();
        
        var startTime = level.Direction == Direction.Up ? level.LowTime : level.HighTime;
        var startPrice = level.Entry ?? (level.Direction == Direction.Up ? level.Low : level.High);
        var endTime = level.StretchTo ?? (level.Direction == Direction.Up ? level.HighTime : level.LowTime);
        var endPrice = level.Direction == Direction.Up ? level.Low : level.High;
            
        chart.DrawTrendLine($"{id}-{lineStyle}-{startTime}", startTime, startPrice, endTime, startPrice, lineStyle.color, 1, lineStyle.style);
    }
    
    public static void DrawRectangle(this Chart chart, SwingPoint startPoint, SwingPoint endPoint, int opacity = 10)
    {
        var rectangle = chart.DrawRectangle($"rect-{startPoint.Time}", startPoint.Time, startPoint.Bar.Low, endPoint.Time, startPoint.Bar.High, Color.Pink);
        rectangle.IsFilled = true;
        rectangle.Color = Color.FromArgb(opacity, rectangle.Color);
    }

    public static void DrawRectangle(this Chart chart, Level level, string id, bool drawMidPoint = false, int opacity = 10)
    {
        var endTime = level.StretchTo ?? level.LowTime.AddMinutes(5);
        var startTime = level.LowTime < level.HighTime ? level.LowTime : level.HighTime;
        var startPrice = level.LowTime < level.HighTime ? level.Low : level.High;
        var endPrice = level.LowTime < level.HighTime ? level.High : level.Low;
        
        var color = level.Direction == Direction.Up ? Color.Green : Color.Pink;
        
        var rectangle = chart.DrawRectangle($"rect-{id}-{startTime}", startTime, startPrice, endTime, endPrice, color);
        rectangle.IsFilled = true;
        rectangle.Color = Color.FromArgb(opacity, rectangle.Color);

        if (drawMidPoint)
        {
            var line = chart.DrawTrendLine($"rect-{id}-ce-{startTime}", level.LowTime, level.Mid, endTime, level.Mid, Color.Wheat, 1, LineStyle.Dots);
        }
    }
    
    public static void DrawLevel(this Chart chart, Level level, bool drawMidPoint = false)
    {
        var lowEndTime = level.StretchTo ?? level.LowTime.AddMinutes(1);
        var highEndTime = level.StretchTo ?? level.HighTime.AddMinutes(1);
        var midEndTime = level.StretchTo ?? level.MidTime.AddMinutes(1);
        var midTime = level.LowTime > level.HighTime ? level.HighTime.AddMinutes(1) : level.LowTime.AddMinutes(1);
        
        var highLine = chart.DrawTrendLine($"hi-{level.HighTime}", level.HighTime, level.High, highEndTime, level.High, Color.Green, 1, LineStyle.Solid);

        var lowLine = chart.DrawTrendLine($"lo-{level.LowTime}", level.LowTime, level.Low, lowEndTime, level.Low, Color.Green, 1, LineStyle.Solid);

        if (drawMidPoint)
        {
            var midLine = chart.DrawTrendLine($"ce-{midTime}", midTime, level.Mid, midEndTime, level.Mid, Color.Green, 1, LineStyle.Dots);
        }
    }

    public static void DrawStandardDeviation(this Chart chart, StandardDeviation standardDeviation)
    {
        var time = standardDeviation.OneTime;
        var timePlusOne = time.AddMinutes(5);
        if (standardDeviation.MinusTwo > 0)
        {
            var two = chart.DrawTrendLine($"{standardDeviation.OneTime}-two", time,standardDeviation.MinusTwo, timePlusOne, standardDeviation.MinusTwo, Color.Green);
        }

        if (standardDeviation.MinusFour > 0)
        {
            var four = chart.DrawTrendLine($"{standardDeviation.OneTime}-four", time,standardDeviation.MinusFour, timePlusOne, standardDeviation.MinusFour, Color.Red);
        }
    }
    
    // Add this method to the ChartExtensions class

    /// <summary>
    /// Draws a rectangle showing activation of a level with a midpoint line
    /// </summary>
    public static void DrawActivationRectangle(this Chart chart, Level level, SwingPoint activatingPoint, string prefix, int opacity = 15)
    {
        // Create a unique ID for this activation
        string id = $"{prefix}-{level.LowTime.Ticks}-{activatingPoint.Time.Ticks}";
    
        // Define the start and end times correctly
        DateTime startTime = level.LowTime < level.HighTime ? level.LowTime : level.HighTime;
        DateTime endTime = activatingPoint.Time;
    
        // Set color based on direction
        Color color = level.Direction == Direction.Up ? Color.Green : Color.Pink;
    
        // Draw the activation rectangle
        var rectangle = chart.DrawRectangle(
            $"{id}-rect", 
            startTime, 
            level.Low, 
            endTime, 
            level.High, 
            color);
        
        rectangle.IsFilled = true;
        rectangle.Color = Color.FromArgb(opacity, rectangle.Color); // Semi-transparent
    
        // Draw a dotted line for the midpoint
        chart.DrawTrendLine(
            $"{id}-midline", 
            startTime, 
            level.Mid, 
            endTime, 
            level.Mid, 
            Color.Wheat, 
            1, 
            LineStyle.Dots);
        
        return;
        
        // Add a label to identify what kind of activation this is
        var levelType = level.LevelType.ToString();
        chart.DrawText(
            $"{id}-label",
            $"{levelType} Activation", 
            startTime, 
            level.Mid, 
            color);
    }
    
    /// <summary>
    /// Updates the market bias display on the chart
    /// </summary>
    public static void UpdateBias(this Chart chart, Direction bias)
    {
        chart.RemoveObject("bias");
        var text = bias == Direction.Down ? "Bearish" : "Bullish";
        chart.DrawStaticText("bias", text, VerticalAlignment.Bottom, HorizontalAlignment.Left, Color.Wheat);
    }
}