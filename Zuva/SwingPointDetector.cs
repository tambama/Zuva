using System;
using cAlgo.API;
using cAlgo.API.Internals;

namespace cAlgo
{
    /// <summary>
    /// Detects swing points in price data
    /// </summary>
    public class SwingPointDetector
    {
        private readonly Bars _bars;
        
        // Swing point tracking
        private int _lastSwingHighIndex = -1;
        private int _lastSwingLowIndex = -1;
        private double _lastSwingHighValue = double.MinValue;
        private double _lastSwingLowValue = double.MaxValue;
        private bool _lastSwingWasHigh = false;
        private bool _lastSwingWasLow = false;

        public SwingPointDetector(Bars bars)
        {
            _bars = bars;
        }

        /// <summary>
        /// Detects swing points for the specified bar index
        /// </summary>
        public SwingPoint DetectSwingPoint(int index)
        {
            if (index <= 0 || index >= _bars.Count)
            {
                return new SwingPoint();
            }

            SwingPoint result = new SwingPoint
            {
                PreviousSwingHighIndex = _lastSwingHighIndex,
                PreviousSwingLowIndex = _lastSwingLowIndex
            };

            // Current candle properties
            double currentHigh = _bars.HighPrices[index];
            double currentLow = _bars.LowPrices[index];
            double currentOpen = _bars.OpenPrices[index];
            double currentClose = _bars.ClosePrices[index];
            bool isDownCandle = currentClose < currentOpen;
            bool isUpCandle = currentClose > currentOpen;

            // Handle the special case where previous candle has a swing low
            if (_lastSwingWasLow && _lastSwingLowIndex >= 0)
            {
                double prevSwingLowValue = _bars.LowPrices[_lastSwingLowIndex];
                double prevSwingLowCandleHigh = _bars.HighPrices[_lastSwingLowIndex];

                if (currentLow < prevSwingLowValue && currentHigh > prevSwingLowCandleHigh)
                {
                    if (isDownCandle)
                    {
                        // Set current high as swing high first
                        result.HasSwingHigh = true;
                        result.SwingHighValue = currentHigh;
                        _lastSwingHighIndex = index;
                        _lastSwingHighValue = currentHigh;
                        _lastSwingWasHigh = true;
                        _lastSwingWasLow = false;

                        // Then set current low as swing low
                        result.HasSwingLow = true;
                        result.SwingLowValue = currentLow;
                        _lastSwingLowIndex = index;
                        _lastSwingLowValue = currentLow;
                        _lastSwingWasLow = true;
                        _lastSwingWasHigh = false;
                        
                        return result;
                    }
                    else if (isUpCandle)
                    {
                        // Move the swing low to current candle
                        result.HasSwingLow = true;
                        result.SwingLowValue = currentLow;
                        result.ClearPreviousLow = true;
                        _lastSwingLowIndex = index;
                        _lastSwingLowValue = currentLow;

                        // Then set current high as swing high
                        result.HasSwingHigh = true;
                        result.SwingHighValue = currentHigh;
                        _lastSwingHighIndex = index;
                        _lastSwingHighValue = currentHigh;
                        _lastSwingWasHigh = true;
                        _lastSwingWasLow = false;
                        
                        return result;
                    }
                }
            }

            // Handle the special case where previous candle has a swing high
            if (_lastSwingWasHigh && _lastSwingHighIndex >= 0)
            {
                double prevSwingHighValue = _bars.HighPrices[_lastSwingHighIndex];
                double prevSwingHighCandleLow = _bars.LowPrices[_lastSwingHighIndex];

                if (currentHigh > prevSwingHighValue && currentLow < prevSwingHighCandleLow)
                {
                    if (isUpCandle)
                    {
                        // Set current low as swing low first
                        result.HasSwingLow = true;
                        result.SwingLowValue = currentLow;
                        _lastSwingLowIndex = index;
                        _lastSwingLowValue = currentLow;
                        _lastSwingWasLow = true;
                        _lastSwingWasHigh = false;

                        // Then set current high as swing high
                        result.HasSwingHigh = true;
                        result.SwingHighValue = currentHigh;
                        _lastSwingHighIndex = index;
                        _lastSwingHighValue = currentHigh;
                        _lastSwingWasHigh = true;
                        _lastSwingWasLow = false;
                        
                        return result;
                    }
                    else if (isDownCandle)
                    {
                        // Move the swing high to current candle
                        result.HasSwingHigh = true;
                        result.SwingHighValue = currentHigh;
                        result.ClearPreviousHigh = true;
                        _lastSwingHighIndex = index;
                        _lastSwingHighValue = currentHigh;

                        // Then set current low as swing low
                        result.HasSwingLow = true;
                        result.SwingLowValue = currentLow;
                        _lastSwingLowIndex = index;
                        _lastSwingLowValue = currentLow;
                        _lastSwingWasLow = true;
                        _lastSwingWasHigh = false;
                        
                        return result;
                    }
                }
            }

            // Normal swing high detection logic
            if (_lastSwingWasLow || (!_lastSwingWasHigh && !_lastSwingWasLow))
            {
                // If last swing was a low or no swing yet
                if (currentHigh > _lastSwingHighValue)
                {
                    // New swing high
                    result.HasSwingHigh = true;
                    result.SwingHighValue = currentHigh;
                    _lastSwingHighIndex = index;
                    _lastSwingHighValue = currentHigh;
                    _lastSwingWasHigh = true;
                    _lastSwingWasLow = false;
                    return result;
                }
            }
            else if (_lastSwingWasHigh && _lastSwingHighIndex >= 0)
            {
                // If last swing was a high
                if (currentHigh > _lastSwingHighValue)
                {
                    // Move swing high to current candle
                    result.HasSwingHigh = true;
                    result.SwingHighValue = currentHigh;
                    result.ClearPreviousHigh = true;
                    _lastSwingHighIndex = index;
                    _lastSwingHighValue = currentHigh;
                    return result;
                }
                
                // Check if we should create a new swing low
                if (currentLow < _bars.LowPrices[_lastSwingHighIndex])
                {
                    // New swing low after a swing high
                    result.HasSwingLow = true;
                    result.SwingLowValue = currentLow;
                    _lastSwingLowIndex = index;
                    _lastSwingLowValue = currentLow;
                    _lastSwingWasLow = true;
                    _lastSwingWasHigh = false;
                    return result;
                }
            }

            // Normal swing low detection logic
            if (_lastSwingWasHigh || (!_lastSwingWasHigh && !_lastSwingWasLow))
            {
                // If last swing was a high or no swing yet
                if (currentLow < _lastSwingLowValue)
                {
                    // New swing low
                    result.HasSwingLow = true;
                    result.SwingLowValue = currentLow;
                    _lastSwingLowIndex = index;
                    _lastSwingLowValue = currentLow;
                    _lastSwingWasLow = true;
                    _lastSwingWasHigh = false;
                    return result;
                }
            }
            else if (_lastSwingWasLow && _lastSwingLowIndex >= 0)
            {
                // If last swing was a low
                if (currentLow < _lastSwingLowValue)
                {
                    // Move swing low to current candle
                    result.HasSwingLow = true;
                    result.SwingLowValue = currentLow;
                    result.ClearPreviousLow = true;
                    _lastSwingLowIndex = index;
                    _lastSwingLowValue = currentLow;
                    return result;
                }
                
                // Check if we should create a new swing high
                if (currentHigh > _bars.HighPrices[_lastSwingLowIndex])
                {
                    // New swing high after a swing low
                    result.HasSwingHigh = true;
                    result.SwingHighValue = currentHigh;
                    _lastSwingHighIndex = index;
                    _lastSwingHighValue = currentHigh;
                    _lastSwingWasHigh = true;
                    _lastSwingWasLow = false;
                    return result;
                }
            }

            return result;
        }
    }
}