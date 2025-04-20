using System;
using System.Collections.Generic;
using cAlgo.API;

namespace Zuva.Helpers
{
    /// <summary>
    /// Helper class for timeframe-related operations
    /// </summary>
    public class TimeframeHelper
    {
        /// <summary>
        /// Converts a timeframe string to a TimeFrame enum value
        /// </summary>
        public static TimeFrame GetTimeFrameFromString(string timeframeStr)
        {
            switch (timeframeStr.ToUpper())
            {
                case "M1":
                    return TimeFrame.Minute;
                case "M5":
                    return TimeFrame.Minute5;
                case "M15":
                    return TimeFrame.Minute15;
                case "M30":
                    return TimeFrame.Minute30;
                case "H1":
                    return TimeFrame.Hour;
                case "H4":
                    return TimeFrame.Hour4;
                case "D1":
                    return TimeFrame.Daily;
                case "W1":
                    return TimeFrame.Weekly;
                case "MN1":
                    return TimeFrame.Monthly;
                default:
                    return TimeFrame.Hour4; // Default to H4 if input is invalid
            }
        }
        
        /// <summary>
        /// Maps higher timeframe bars to current timeframe indices
        /// </summary>
        public static Dictionary<long, int> MapHigherTimeframeToCurrent(Bars currentTFBars, Bars higherTFBars)
        {
            Dictionary<long, int> higherTFBarOpenTimes = new Dictionary<long, int>();
            
            // Create a mapping between higher timeframe bar open times and current timeframe indices
            for (int i = 0; i < higherTFBars.Count; i++)
            {
                DateTime higherTFOpenTime = higherTFBars.OpenTimes[i];
                long openTimeTicks = higherTFOpenTime.Ticks;
                
                for (int j = 0; j < currentTFBars.Count; j++)
                {
                    if (currentTFBars.OpenTimes[j] >= higherTFOpenTime)
                    {
                        higherTFBarOpenTimes[openTimeTicks] = j;
                        break;
                    }
                }
            }
            
            return higherTFBarOpenTimes;
        }
        
        /// <summary>
        /// Gets the current timeframe index that corresponds to a higher timeframe bar
        /// </summary>
        public static int FindCurrentTimeframeIndexForHigherTimeframe(
            int higherTFIndex, 
            Bars higherTFBars, 
            Dictionary<long, int> higherTFBarOpenTimes)
        {
            if (higherTFIndex < 0 || higherTFIndex >= higherTFBars.Count)
                return -1;
                
            DateTime higherTFOpenTime = higherTFBars.OpenTimes[higherTFIndex];
            long openTimeTicks = higherTFOpenTime.Ticks;
            
            if (higherTFBarOpenTimes.TryGetValue(openTimeTicks, out int currentTFIndex))
            {
                return currentTFIndex;
            }
            
            return -1;
        }
        
        /// <summary>
        /// Determines if the current bar is the last bar of a higher timeframe
        /// </summary>
        public static bool IsLastBarOfHigherTimeframe(
            int currentIndex, 
            Bars currentTFBars,
            int higherTFIndex,
            Bars higherTFBars)
        {
            if (currentIndex >= currentTFBars.Count - 1 || higherTFIndex >= higherTFBars.Count - 1)
                return false;
            
            DateTime nextBarOpenTime = currentTFBars.OpenTimes[currentIndex + 1];
            DateTime nextHigherTFOpenTime = higherTFBars.OpenTimes[higherTFIndex + 1];
            
            return nextBarOpenTime >= nextHigherTFOpenTime;
        }
    }
}