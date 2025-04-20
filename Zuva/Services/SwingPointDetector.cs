using cAlgo.API;
using Zuva.Models;

namespace Zuva.Services
{
    /// <summary>
    /// Handles the detection of swing points based on ICT methodology
    /// </summary>
    public class SwingPointDetector
    {
        private int _lastSwingHighIndex = -1;
        private int _lastSwingLowIndex = -1;
        private double _lastSwingHighValue = double.MinValue;
        private double _lastSwingLowValue = double.MaxValue;
        private bool _lastSwingWasHigh = false;
        private bool _lastSwingWasLow = false;
        
        // Store the high and low values of the swing point candles
        private double _lastSwingHighCandleLow = double.MaxValue;
        private double _lastSwingLowCandleHigh = double.MinValue;
        
        private readonly IndicatorDataSeries _swingHighs;
        private readonly IndicatorDataSeries _swingLows;
        
        public SwingPointDetector(IndicatorDataSeries swingHighs, IndicatorDataSeries swingLows)
        {
            _swingHighs = swingHighs;
            _swingLows = swingLows;
        }
        
        public void ProcessBar(int index, double currentHigh, double currentLow, double currentOpen, double currentClose)
        {
            // Need at least 1 bar to calculate
            if (index <= 0)
                return;

            bool isDownCandle = currentClose < currentOpen;
            bool isUpCandle = currentClose > currentOpen;

            // Handle the special case where previous candle has a swing low
            if (_lastSwingWasLow && _lastSwingLowIndex >= 0)
            {
                double prevSwingLowValue = _lastSwingLowValue;
                double prevSwingLowCandleHigh = _lastSwingLowCandleHigh;

                if (currentLow < prevSwingLowValue && currentHigh > prevSwingLowCandleHigh)
                {
                    if (isDownCandle)
                    {
                        // Set current high as swing high first
                        _swingHighs[index] = currentHigh;
                        _lastSwingHighIndex = index;
                        _lastSwingHighValue = currentHigh;
                        _lastSwingHighCandleLow = currentLow;
                        _lastSwingWasHigh = true;
                        _lastSwingWasLow = false;

                        // Then set current low as swing low
                        _swingLows[index] = currentLow;
                        _lastSwingLowIndex = index;
                        _lastSwingLowValue = currentLow;
                        _lastSwingLowCandleHigh = currentHigh;
                        _lastSwingWasLow = true;
                        _lastSwingWasHigh = false;
                        
                        return; // Finished processing this candle
                    }
                    else if (isUpCandle)
                    {
                        // Move the swing low to current candle
                        _swingLows[_lastSwingLowIndex] = double.NaN;
                        _swingLows[index] = currentLow;
                        _lastSwingLowIndex = index;
                        _lastSwingLowValue = currentLow;
                        _lastSwingLowCandleHigh = currentHigh;

                        // Then set current high as swing high
                        _swingHighs[index] = currentHigh;
                        _lastSwingHighIndex = index;
                        _lastSwingHighValue = currentHigh;
                        _lastSwingHighCandleLow = currentLow;
                        _lastSwingWasHigh = true;
                        _lastSwingWasLow = false;
                        
                        return; // Finished processing this candle
                    }
                }
            }

            // Handle the special case where previous candle has a swing high
            if (_lastSwingWasHigh && _lastSwingHighIndex >= 0)
            {
                double prevSwingHighValue = _lastSwingHighValue;
                double prevSwingHighCandleLow = _lastSwingHighCandleLow;

                if (currentHigh > prevSwingHighValue && currentLow < prevSwingHighCandleLow)
                {
                    if (isUpCandle)
                    {
                        // Set current low as swing low first
                        _swingLows[index] = currentLow;
                        _lastSwingLowIndex = index;
                        _lastSwingLowValue = currentLow;
                        _lastSwingLowCandleHigh = currentHigh;
                        _lastSwingWasLow = true;
                        _lastSwingWasHigh = false;

                        // Then set current high as swing high
                        _swingHighs[index] = currentHigh;
                        _lastSwingHighIndex = index;
                        _lastSwingHighValue = currentHigh;
                        _lastSwingHighCandleLow = currentLow;
                        _lastSwingWasHigh = true;
                        _lastSwingWasLow = false;
                        
                        return; // Finished processing this candle
                    }
                    else if (isDownCandle)
                    {
                        // Move the swing high to current candle
                        _swingHighs[_lastSwingHighIndex] = double.NaN;
                        _swingHighs[index] = currentHigh;
                        _lastSwingHighIndex = index;
                        _lastSwingHighValue = currentHigh;
                        _lastSwingHighCandleLow = currentLow;

                        // Then set current low as swing low
                        _swingLows[index] = currentLow;
                        _lastSwingLowIndex = index;
                        _lastSwingLowValue = currentLow;
                        _lastSwingLowCandleHigh = currentHigh;
                        _lastSwingWasLow = true;
                        _lastSwingWasHigh = false;
                        
                        return; // Finished processing this candle
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
                    _swingHighs[index] = currentHigh;
                    _lastSwingHighIndex = index;
                    _lastSwingHighValue = currentHigh;
                    _lastSwingHighCandleLow = currentLow;
                    _lastSwingWasHigh = true;
                    _lastSwingWasLow = false;
                    return; // Finished processing this candle
                }
            }
            else if (_lastSwingWasHigh && _lastSwingHighIndex >= 0)
            {
                // If last swing was a high
                if (currentHigh > _lastSwingHighValue)
                {
                    // Move swing high to current candle
                    _swingHighs[_lastSwingHighIndex] = double.NaN;
                    _swingHighs[index] = currentHigh;
                    _lastSwingHighIndex = index;
                    _lastSwingHighValue = currentHigh;
                    _lastSwingHighCandleLow = currentLow;
                    return; // Finished processing this candle
                }
                
                // Check if we should create a new swing low
                if (currentLow < _lastSwingHighCandleLow)
                {
                    // New swing low after a swing high
                    _swingLows[index] = currentLow;
                    _lastSwingLowIndex = index;
                    _lastSwingLowValue = currentLow;
                    _lastSwingLowCandleHigh = currentHigh;
                    _lastSwingWasLow = true;
                    _lastSwingWasHigh = false;
                    return; // Finished processing this candle
                }
            }

            // Normal swing low detection logic
            if (_lastSwingWasHigh || (!_lastSwingWasHigh && !_lastSwingWasLow))
            {
                // If last swing was a high or no swing yet
                if (currentLow < _lastSwingLowValue)
                {
                    // New swing low
                    _swingLows[index] = currentLow;
                    _lastSwingLowIndex = index;
                    _lastSwingLowValue = currentLow;
                    _lastSwingLowCandleHigh = currentHigh;
                    _lastSwingWasLow = true;
                    _lastSwingWasHigh = false;
                    return; // Finished processing this candle
                }
            }
            else if (_lastSwingWasLow && _lastSwingLowIndex >= 0)
            {
                // If last swing was a low
                if (currentLow < _lastSwingLowValue)
                {
                    // Move swing low to current candle
                    _swingLows[_lastSwingLowIndex] = double.NaN;
                    _swingLows[index] = currentLow;
                    _lastSwingLowIndex = index;
                    _lastSwingLowValue = currentLow;
                    _lastSwingLowCandleHigh = currentHigh;
                    return; // Finished processing this candle
                }
                
                // Check if we should create a new swing high
                if (currentHigh > _lastSwingLowCandleHigh)
                {
                    // New swing high after a swing low
                    _swingHighs[index] = currentHigh;
                    _lastSwingHighIndex = index;
                    _lastSwingHighValue = currentHigh;
                    _lastSwingHighCandleLow = currentLow;
                    _lastSwingWasHigh = true;
                    _lastSwingWasLow = false;
                    return; // Finished processing this candle
                }
            }
        }
        
        // Optional: Add methods to retrieve swing point information
        public bool IsSwingHigh(int index)
        {
            return !double.IsNaN(_swingHighs[index]);
        }
        
        public bool IsSwingLow(int index)
        {
            return !double.IsNaN(_swingLows[index]);
        }
        
        public int GetLastSwingHighIndex()
        {
            return _lastSwingHighIndex;
        }
        
        public int GetLastSwingLowIndex()
        {
            return _lastSwingLowIndex;
        }
    }
}