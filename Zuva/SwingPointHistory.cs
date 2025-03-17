using System;

namespace cAlgo
{
    /// <summary>
    /// Class to track historical swing points for persistence
    /// </summary>
    public class SwingPointHistory
    {
        /// <summary>
        /// The index in the current timeframe where this swing point was detected
        /// </summary>
        public int Index { get; set; }
        
        /// <summary>
        /// The price value of the swing point
        /// </summary>
        public double Value { get; set; }
        
        /// <summary>
        /// The higher timeframe index that corresponds to this swing point
        /// </summary>
        public int HigherTimeframeIndex { get; set; }
    }
}