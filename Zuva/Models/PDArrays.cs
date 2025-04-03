using System;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Internals;

namespace cAlgo
{
    /// <summary>
    /// Base class for Price Delivery Arrays
    /// </summary>
    public abstract class PDArray
    {
        public int StartIndex { get; set; }
        public double UpperBound { get; set; }
        public double LowerBound { get; set; }
        public DateTime StartTime { get; set; }
        public bool IsActive { get; set; } = true;
        public abstract PDArrayType Type { get; }
    }

    /// <summary>
    /// Types of Price Delivery Arrays
    /// </summary>
    public enum PDArrayType
    {
        FairValueGap,
        OrderBlock
    }

    /// <summary>
    /// Represents a Fair Value Gap - a gap in price action where no trading took place
    /// </summary>
    public class FairValueGap : PDArray
    {
        public override PDArrayType Type => PDArrayType.FairValueGap;
        public bool IsBullish { get; set; }

        public FairValueGap(int startIndex, double upper, double lower, DateTime startTime, bool isBullish)
        {
            StartIndex = startIndex;
            UpperBound = upper;
            LowerBound = lower;
            StartTime = startTime;
            IsBullish = isBullish;
        }
    }

    /// <summary>
    /// Represents an Order Block - a zone where significant orders were placed
    /// </summary>
    public class OrderBlock : PDArray
    {
        public override PDArrayType Type => PDArrayType.OrderBlock;
        public bool IsBullish { get; set; }

        public OrderBlock(int startIndex, double upper, double lower, DateTime startTime, bool isBullish)
        {
            StartIndex = startIndex;
            UpperBound = upper;
            LowerBound = lower;
            StartTime = startTime;
            IsBullish = isBullish;
        }
    }

    /// <summary>
    /// Detector for various Price Delivery Arrays
    /// </summary>
    public class PDArrayDetector
    {
        private readonly Bars _bars;
        private List<FairValueGap> _fairValueGaps = new List<FairValueGap>();
        private List<OrderBlock> _orderBlocks = new List<OrderBlock>();

        public PDArrayDetector(Bars bars)
        {
            _bars = bars;
        }

        public List<FairValueGap> GetFairValueGaps()
        {
            return _fairValueGaps;
        }

        public List<OrderBlock> GetOrderBlocks()
        {
            return _orderBlocks;
        }

        /// <summary>
        /// Detects Fair Value Gaps at the specified index
        /// </summary>
        public FairValueGap DetectFairValueGap(int index)
        {
            // Need at least 3 bars for FVG calculation
            if (index < 2 || index >= _bars.Count)
                return null;

            // Get the three bars needed for FVG detection
            double bar1High = _bars.HighPrices[index - 2];
            double bar1Low = _bars.LowPrices[index - 2];
            double bar2High = _bars.HighPrices[index - 1];
            double bar2Low = _bars.LowPrices[index - 1];
            double bar3High = _bars.HighPrices[index];
            double bar3Low = _bars.LowPrices[index];

            // Check for bullish FVG (low of 1st bar > high of 3rd bar)
            if (bar1Low > bar3High)
            {
                var fvg = new FairValueGap(
                    startIndex: index - 2,
                    upper: bar1Low,
                    lower: bar3High,
                    startTime: _bars.OpenTimes[index - 2],
                    isBullish: true
                );
                _fairValueGaps.Add(fvg);
                return fvg;
            }

            // Check for bearish FVG (high of 1st bar < low of 3rd bar)
            if (bar1High < bar3Low)
            {
                var fvg = new FairValueGap(
                    startIndex: index - 2,
                    upper: bar3Low,
                    lower: bar1High,
                    startTime: _bars.OpenTimes[index - 2],
                    isBullish: false
                );
                _fairValueGaps.Add(fvg);
                return fvg;
            }

            return null;
        }

        /// <summary>
        /// Detects Order Blocks at the specified index
        /// </summary>
        public OrderBlock DetectOrderBlock(int index)
        {
            // Need at least 3 bars for Order Block calculation
            if (index < 2 || index >= _bars.Count)
                return null;

            // Get necessary bar data
            double bar1Open = _bars.OpenPrices[index - 2];
            double bar1Close = _bars.ClosePrices[index - 2];
            double bar1High = _bars.HighPrices[index - 2];
            double bar1Low = _bars.LowPrices[index - 2];
            
            double bar2Open = _bars.OpenPrices[index - 1];
            double bar2Close = _bars.ClosePrices[index - 1];
            double bar2High = _bars.HighPrices[index - 1];
            double bar2Low = _bars.LowPrices[index - 1];
            
            double bar3Open = _bars.OpenPrices[index];
            double bar3Close = _bars.ClosePrices[index];
            double bar3High = _bars.HighPrices[index];
            double bar3Low = _bars.LowPrices[index];

            // Check for bullish Order Block (middle bar is bearish and the next bar is strongly bullish)
            bool middleBarBearish = bar2Close < bar2Open;
            bool nextBarBullish = bar3Close > bar3Open && (bar3Close - bar3Open) > (bar2Open - bar2Close); 
            
            if (middleBarBearish && nextBarBullish && bar3Low <= bar2Low && bar3High > bar2High)
            {
                // The Order Block is the middle bar's body (or a portion of it)
                var orderBlock = new OrderBlock(
                    startIndex: index - 1,
                    upper: bar2Open,
                    lower: bar2Close,
                    startTime: _bars.OpenTimes[index - 1],
                    isBullish: true
                );
                _orderBlocks.Add(orderBlock);
                return orderBlock;
            }

            // Check for bearish Order Block (middle bar is bullish and the next bar is strongly bearish)
            bool middleBarBullish = bar2Close > bar2Open;
            bool nextBarBearish = bar3Close < bar3Open && (bar3Open - bar3Close) > (bar2Close - bar2Open);
            
            if (middleBarBullish && nextBarBearish && bar3High >= bar2High && bar3Low < bar2Low)
            {
                // The Order Block is the middle bar's body (or a portion of it)
                var orderBlock = new OrderBlock(
                    startIndex: index - 1,
                    upper: bar2Close,
                    lower: bar2Open,
                    startTime: _bars.OpenTimes[index - 1],
                    isBullish: false
                );
                _orderBlocks.Add(orderBlock);
                return orderBlock;
            }

            return null;
        }

        /// <summary>
        /// Updates active status of all PD Arrays
        /// </summary>
        public void UpdatePDArrayStatus(int currentIndex)
        {
            // Update fair value gaps
            for (int i = 0; i < _fairValueGaps.Count; i++)
            {
                var fvg = _fairValueGaps[i];
                if (!fvg.IsActive) continue;

                // Check if price has moved into the FVG's range
                for (int j = fvg.StartIndex + 1; j <= currentIndex; j++)
                {
                    if (fvg.IsBullish)
                    {
                        // Bullish FVG is mitigated if price trades down into it
                        if (_bars.LowPrices[j] <= fvg.UpperBound && _bars.HighPrices[j] >= fvg.LowerBound)
                        {
                            fvg.IsActive = false;
                            break;
                        }
                    }
                    else
                    {
                        // Bearish FVG is mitigated if price trades up into it
                        if (_bars.HighPrices[j] >= fvg.LowerBound && _bars.LowPrices[j] <= fvg.UpperBound)
                        {
                            fvg.IsActive = false;
                            break;
                        }
                    }
                }
            }

            // Update order blocks
            for (int i = 0; i < _orderBlocks.Count; i++)
            {
                var ob = _orderBlocks[i];
                if (!ob.IsActive) continue;

                // Check if price has moved beyond the order block's threshold
                for (int j = ob.StartIndex + 1; j <= currentIndex; j++)
                {
                    if (ob.IsBullish)
                    {
                        // Bullish OB is mitigated if price trades below the lower bound
                        if (_bars.LowPrices[j] < ob.LowerBound)
                        {
                            ob.IsActive = false;
                            break;
                        }
                    }
                    else
                    {
                        // Bearish OB is mitigated if price trades above the upper bound
                        if (_bars.HighPrices[j] > ob.UpperBound)
                        {
                            ob.IsActive = false;
                            break;
                        }
                    }
                }
            }
        }
    }
}