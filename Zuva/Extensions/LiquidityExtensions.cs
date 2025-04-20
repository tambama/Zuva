using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Internals;
using Zuva.Models;

namespace Zuva.Extensions;

public static class LiquidityExtensions
{
    public static void LiquidityCheck(this List<SwingPoint> externalLiquidity, Chart chart, MarketData marketData, Bars bars, Bar currentBar, bool showSessionLevels, bool showLiquidity)
        {
            var endOfDay = currentBar.OpenTime.Date.AddHours(16).AddMinutes(14);
            
            // check Asian Session high and low
            if (currentBar.OpenTime.TimeOfDay == new TimeSpan(0, 0, 0) &&
                currentBar.OpenTime.DayOfWeek != DayOfWeek.Sunday && showSessionLevels)
            {
                externalLiquidity.ResetPassCount();
                
                var asiaStart = currentBar.OpenTime.Date.AddDays(-1).AddHours(20);
                var asiaEnd = currentBar.OpenTime;
                var asianSession = bars.GetMinMax(asiaStart, asiaEnd);
                
                var highBar = bars[asianSession.maxIndex];
                var swingHigh = new SwingPoint(asianSession.maxIndex, asianSession.max, highBar.OpenTime, highBar, SwingType.H,
                    LiquidityType.PSH, Direction.Up, LiquidityName.AH);
                externalLiquidity.Add(swingHigh);
                if (showLiquidity)
                {
                    chart.DrawStraightLine($"lq-{swingHigh.Time}", swingHigh.Time, swingHigh.Price, endOfDay, swingHigh.Price, swingHigh.LiquidityName.ToString(), LineStyle.Solid, Color.Pink, true, false, false);
                }

                var lowBar = bars[asianSession.minIndex];
                var swingLow = new SwingPoint(asianSession.minIndex, asianSession.min, lowBar.OpenTime, lowBar, SwingType.L,
                    LiquidityType.PSL, Direction.Down, LiquidityName.AL);
                externalLiquidity.Add(swingLow);
                if (showLiquidity)
                {
                    chart.DrawStraightLine($"lq-{swingLow.Time}", swingLow.Time, swingLow.Price, endOfDay, swingLow.Price, swingLow.LiquidityName.ToString(), LineStyle.Solid, Color.Pink, true, false, false);
                }
            }

            // check London Session high and low
            if (currentBar.OpenTime.TimeOfDay == new TimeSpan(7, 0, 0) &&
                currentBar.OpenTime.DayOfWeek != DayOfWeek.Sunday && showSessionLevels)
            {
                externalLiquidity.ResetPassCount();

                // TODO: Remove previous London Session Highs/Lows
                var londonStart = currentBar.OpenTime.Date.AddHours(2).AddMinutes(30);
                var londonEnd = currentBar.OpenTime;
                var londonSession = bars.GetMinMax(londonStart, londonEnd);

                var highBar = bars[londonSession.maxIndex];
                var swingHigh = new SwingPoint(londonSession.maxIndex, londonSession.max, highBar.OpenTime, highBar, SwingType.H,
                    LiquidityType.PSH, Direction.Up, LiquidityName.LH);
                externalLiquidity.Add(swingHigh);
                if (showLiquidity)
                {
                    chart.DrawStraightLine($"lq-{swingHigh.Time}", swingHigh.Time, swingHigh.Price, endOfDay, swingHigh.Price, swingHigh.LiquidityName.ToString(), LineStyle.Solid, Color.Pink, true, false, false);
                }

                var lowBar = bars[londonSession.minIndex];
                var swingLow = new SwingPoint(londonSession.minIndex, londonSession.min, lowBar.OpenTime, lowBar, SwingType.L,
                    LiquidityType.PSL, Direction.Down, LiquidityName.LL);
                externalLiquidity.Add(swingLow);
                if (showLiquidity)
                {
                    chart.DrawStraightLine($"lq-{swingLow.Time}", swingLow.Time, swingLow.Price, endOfDay, swingLow.Price, swingLow.LiquidityName.ToString(), LineStyle.Solid, Color.Pink, true, false, false);
                }
            }

            // check New York Morning Session high and low
            if (currentBar.OpenTime.TimeOfDay == new TimeSpan(13, 0, 0) &&
                currentBar.OpenTime.DayOfWeek != DayOfWeek.Sunday && showSessionLevels)
            {
                externalLiquidity.ResetPassCount();

                var nyStart = currentBar.OpenTime.Date.AddHours(7);
                var nyEnd = currentBar.OpenTime;
                var nySession = bars.GetMinMax(nyStart, nyEnd);

                var highBar = bars[nySession.maxIndex];
                var swingHigh = new SwingPoint(nySession.maxIndex, nySession.max, highBar.OpenTime, highBar, SwingType.H,
                    LiquidityType.PSH, Direction.Up, LiquidityName.NAH);
                externalLiquidity.Add(swingHigh);
                if (showLiquidity)
                {
                    chart.DrawStraightLine($"lq-{swingHigh.Time}", swingHigh.Time, swingHigh.Price, endOfDay, swingHigh.Price, swingHigh.LiquidityName.ToString(), LineStyle.Solid, Color.Pink, true, false, false);
                }

                var lowBar = bars[nySession.minIndex];
                var swingLow = new SwingPoint(nySession.minIndex, nySession.min, lowBar.OpenTime, lowBar, SwingType.L,
                    LiquidityType.PSL, Direction.Down, LiquidityName.NAL);
                externalLiquidity.Add(swingLow);
                if (showLiquidity)
                {
                    chart.DrawStraightLine($"lq-{swingLow.Time}", swingLow.Time, swingLow.Price, endOfDay, swingLow.Price, swingLow.LiquidityName.ToString(), LineStyle.Solid, Color.Pink, true, false, false);
                }
            }

            // check New York PM Session high and low
            if (currentBar.OpenTime.TimeOfDay == new TimeSpan(20, 0, 0) &&
                currentBar.OpenTime.DayOfWeek != DayOfWeek.Sunday && showSessionLevels)
            {
                externalLiquidity.ResetPassCount();

                var nyStart = currentBar.OpenTime.Date.AddHours(13);
                var nyEnd = currentBar.OpenTime;
                var nySession = bars.GetMinMax(nyStart, nyEnd);

                var highBar = bars[nySession.maxIndex];
                var swingHigh = new SwingPoint(nySession.maxIndex, nySession.max, highBar.OpenTime, highBar, SwingType.H,
                    LiquidityType.PSH, Direction.Up, LiquidityName.NPH);
                externalLiquidity.Add(swingHigh);
                if (showLiquidity)
                {
                    chart.DrawStraightLine($"lq-{swingHigh.Time}", swingHigh.Time, swingHigh.Price, endOfDay, swingHigh.Price, swingHigh.LiquidityName.ToString(), LineStyle.Solid, Color.Pink, true, false, false);
                }

                var lowBar = bars[nySession.minIndex];
                var swingLow = new SwingPoint(nySession.minIndex, nySession.min, lowBar.OpenTime, lowBar, SwingType.L,
                    LiquidityType.PSL, Direction.Down, LiquidityName.NPL);
                externalLiquidity.Add(swingLow);
                if (showLiquidity)
                {
                    chart.DrawStraightLine($"lq-{swingLow.Time}", swingLow.Time, swingLow.Price, endOfDay, swingLow.Price, swingLow.LiquidityName.ToString(), LineStyle.Solid, Color.Pink, true, false, false);
                }
            }

            // check previous day high and low at today's open
            if (currentBar.OpenTime.TimeOfDay == new TimeSpan(0, 0, 0))
            {
                externalLiquidity.ResetPassCount();

                var dailySeries = marketData.GetBars(TimeFrame.Daily);
                if (dailySeries.Count > 1)
                {
                    var previousDate = currentBar.OpenTime.Date.AddDays(-1);
                    if (currentBar.OpenTime.DayOfWeek == DayOfWeek.Monday)
                    {
                        previousDate = currentBar.OpenTime.AddDays(-3);
                    }

                    var yesterday = bars.GetMinMax(previousDate, previousDate.EndOfDay());

                    var highBar = bars[yesterday.maxIndex];
                    var swingHigh = new SwingPoint(yesterday.maxIndex, yesterday.max, highBar.OpenTime, highBar, SwingType.H,
                        LiquidityType.PDH, Direction.Up, LiquidityName.PDH);
                    externalLiquidity.Add(swingHigh);
                    if (showLiquidity)
                    {
                        chart.DrawStraightLine($"lq-{swingHigh.Time}", swingHigh.Time, swingHigh.Price, endOfDay, swingHigh.Price, swingHigh.LiquidityName.ToString(), LineStyle.Solid, Color.Pink, true, false, false);
                    }

                    var lowBar = bars[yesterday.minIndex];
                    var swingLow = new SwingPoint(yesterday.minIndex, yesterday.min, lowBar.OpenTime, lowBar, SwingType.L,
                        LiquidityType.PDL, Direction.Down, LiquidityName.PDL);
                    externalLiquidity.Add(swingLow);
                    if (showLiquidity)
                    {
                        chart.DrawStraightLine($"lq-{swingLow.Time}", swingLow.Time, swingLow.Price, endOfDay, swingLow.Price, swingLow.LiquidityName.ToString(), LineStyle.Solid, Color.Pink, true, false, false);
                    }
                }
            }
        }
    
    public static bool CheckLiquiditySweep(this List<SwingPoint> externalLiquidity, Chart chart, Bar currentBar, int currentBarIndex, List<int> sweepers, bool showLiquiditySweep)
        {
            var swept = false;
            var orderedLiquidity = externalLiquidity.OrderBy(l => l.Time).ToList();
            var direction = currentBar.GetCandleDirection();

            // Daily Levels
            var liquidities = orderedLiquidity.Where(e =>
                e.LiquidityType is LiquidityType.PDH or LiquidityType.PDL && e.Price > currentBar.Low &&
                e.Price < currentBar.High).OrderByDescending(l => l.Index).ToList();

            foreach (var liquidity in liquidities)
            {
                if (liquidity.PassCount == 0)
                {
                    liquidity.PassCount++;
                }

                if (showLiquiditySweep)
                {
                    chart.DrawStraightLine($"lq-{liquidity.Time}", liquidity.Time, liquidity.Price, currentBar.OpenTime, liquidity.Price, liquidity.LiquidityName.ToString(), LineStyle.Solid, Color.Gray, true, true, false);
                }

                externalLiquidity.Remove(liquidity);
                
                sweepers.Add(currentBarIndex);
            }
            
            if (liquidities.Count > 0)
                swept = true;

            var yesterday = currentBar.OpenTime.Date.AddDays(-1);

            // Session Levels
            liquidities = orderedLiquidity.Where(e =>
                e.LiquidityType is LiquidityType.PSH or LiquidityType.PSL && e.Time >= yesterday &&
                e.Price > currentBar.Low && e.Price < currentBar.High).OrderBy(l => l.Index).ToList();

            foreach (var liquidity in liquidities)
            {
                if (liquidity.PassCount == 0)
                {
                    liquidity.PassCount++;
                }

                if (showLiquiditySweep)
                {
                    chart.DrawStraightLine($"lq-{liquidity.Time}", liquidity.Time, liquidity.Price, currentBar.OpenTime, liquidity.Price, liquidity.LiquidityName.ToString(), LineStyle.Solid, Color.Gray, true, true, false);
                }

                externalLiquidity.Remove(liquidity);
                
                sweepers.Add(currentBarIndex);
            }

            if (liquidities.Count > 0)
                swept = true;
            
            // Swing Points
            liquidities = orderedLiquidity.Where(e => e.Price > currentBar.Low && e.Price < currentBar.High).OrderBy(l => l.Index).ToList();

            foreach (var liquidity in liquidities)
            {
                if (liquidity.PassCount == 0)
                {
                    liquidity.PassCount++;
                }

                externalLiquidity.Remove(liquidity);
                
                sweepers.Add(currentBarIndex);
            }

            if (liquidities.Count > 0)
                swept = true;
            
            return swept;
        }
    
    public static void ResetPassCount(this List<SwingPoint> swingPoints)
    {
        foreach (var point in swingPoints)
        {
            point.PassCount = 0;
        }
    }
}