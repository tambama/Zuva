using System;
using cAlgo.API;
using Zuva.Models;

namespace Zuva.Extensions;

public static class PriceExtensions
{
    public static Direction GetCandleDirection(this Bar candle)
    {
        return candle.Close > candle.Open ? Direction.Up : Direction.Down;
    }
    
    /// <summary>
    /// Returns the minimum/maximum prices levels during an specific time period
    /// </summary>
    /// <param name="startTime">Start Time (Inclusive)</param>
    /// <param name="endTime">End Time (Inclusive)</param>
    /// <returns>Tuple<double, double> (Item1 will be minimum price and Item2 will be maximum price)</returns>
    public static (int minIndex, double min, int maxIndex, double max) GetMinMax(this Bars Bars, DateTime startTime, DateTime endTime)
    {
        var min = double.MaxValue;
        var minIndex = 0;
        var max = double.MinValue;
        var maxIndex = 0;

        for (var barIndex = 0; barIndex < Bars.Count; barIndex++)
        {
            var bar = Bars[barIndex];

            if (bar.OpenTime < startTime || bar.OpenTime > endTime)
            {
                if (bar.OpenTime > endTime) break;

                continue;
            }

            var newMin = Math.Min(min, bar.Low);
            var newMax = Math.Max(max, bar.High);

            if (newMin != min)
            {
                min = newMin;
                minIndex = barIndex;
            }

            if (newMax == max) continue;
            max = newMax;
            maxIndex = barIndex;
        }

        return (minIndex, min, maxIndex, max);
    }
}