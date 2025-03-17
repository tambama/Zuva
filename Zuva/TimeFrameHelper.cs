using cAlgo.API;

namespace cAlgo
{
    /// <summary>
    /// Helper class for timeframe operations
    /// </summary>
    public static class TimeFrameHelper
    {
        /// <summary>
        /// Converts a string representation of a timeframe to a TimeFrame enum value
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
    }
}