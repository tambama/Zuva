using System;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Zuva : Indicator
    {
        [Parameter("Use Higher Timeframe", DefaultValue = false)]
        public bool UseHigherTimeframe { get; set; }

        [Parameter("Higher Timeframe", DefaultValue = "H4")]
        public string HigherTimeframeStr { get; set; }
        
        private TimeFrame _higherTimeframe;

        [Parameter("Show Current Timeframe Swings", DefaultValue = true)]
        public bool ShowCurrentTimeframeSwings { get; set; }

        [Output("Current TF Swing High", Color = Colors.Green, PlotType = PlotType.Points, Thickness = 4)]
        public IndicatorDataSeries CurrentTFSwingHighs { get; set; }

        [Output("Current TF Swing Low", Color = Colors.Red, PlotType = PlotType.Points, Thickness = 4)]
        public IndicatorDataSeries CurrentTFSwingLows { get; set; }

        [Output("Higher TF Swing High", Color = Colors.Lime, PlotType = PlotType.Points, Thickness = 6)]
        public IndicatorDataSeries HigherTFSwingHighs { get; set; }

        [Output("Higher TF Swing Low", Color = Colors.Crimson, PlotType = PlotType.Points, Thickness = 6)]
        public IndicatorDataSeries HigherTFSwingLows { get; set; }

        private Bars _currentTimeframeBars;
        private Bars _higherTimeframeBars;


        // Current timeframe swing tracking
        private int _lastSwingHighIndex = -1;
        private int _lastSwingLowIndex = -1;
        private double _lastSwingHighValue = double.MinValue;
        private double _lastSwingLowValue = double.MaxValue;
        private bool _lastSwingWasHigh = false;
        private bool _lastSwingWasLow = false;

        // Higher timeframe swing tracking
        private int _lastHigherTFSwingHighIndex = -1;
        private int _lastHigherTFSwingLowIndex = -1;
        private double _lastHigherTFSwingHighValue = double.MinValue;
        private double _lastHigherTFSwingLowValue = double.MaxValue;
        private bool _lastHigherTFSwingWasHigh = false;
        private bool _lastHigherTFSwingWasLow = false;
        private readonly Dictionary<long, int> _higherTFBarOpenTimes = new Dictionary<long, int>();

        protected override void Initialize()
        {
            _currentTimeframeBars = Bars;

            if (UseHigherTimeframe)
            {
                // Convert the string parameter to a TimeFrame
                _higherTimeframe = GetTimeFrameFromString(HigherTimeframeStr);
                
                // Use the higher timeframe selected by the user
                _higherTimeframeBars = MarketData.GetBars(_higherTimeframe);
                
                // Map higher timeframe bars to current timeframe bars for drawing
                MapHigherTimeframeBars();
            }
        }

        private void MapHigherTimeframeBars()
        {
            // Create a mapping between higher timeframe bar open times and current timeframe indices
            for (int i = 0; i < _higherTimeframeBars.Count; i++)
            {
                DateTime higherTFOpenTime = _higherTimeframeBars.OpenTimes[i];
                long openTimeTicks = higherTFOpenTime.Ticks;
                
                for (int j = 0; j < Bars.Count; j++)
                {
                    if (Bars.OpenTimes[j] >= higherTFOpenTime)
                    {
                        _higherTFBarOpenTimes[openTimeTicks] = j;
                        break;
                    }
                }
            }
        }

        public override void Calculate(int index)
        {
            // Need at least 1 bar to calculate
            if (index <= 0)
                return;

            // Calculate current timeframe swing points
            if (ShowCurrentTimeframeSwings)
            {
                CalculateCurrentTimeframeSwings(index);
            }

            // Calculate higher timeframe swing points if enabled
            if (UseHigherTimeframe && _higherTimeframeBars != null)
            {
                CalculateHigherTimeframeSwings(index);
            }
        }

        private void CalculateCurrentTimeframeSwings(int index)
        {
            // Current candle properties
            double currentHigh = Bars.HighPrices[index];
            double currentLow = Bars.LowPrices[index];
            double currentOpen = Bars.OpenPrices[index];
            double currentClose = Bars.ClosePrices[index];
            bool isDownCandle = currentClose < currentOpen;
            bool isUpCandle = currentClose > currentOpen;

            // Handle the special case where previous candle has a swing low
            if (_lastSwingWasLow && _lastSwingLowIndex >= 0)
            {
                double prevSwingLowValue = Bars.LowPrices[_lastSwingLowIndex];
                double prevSwingLowCandleHigh = Bars.HighPrices[_lastSwingLowIndex];

                if (currentLow < prevSwingLowValue && currentHigh > prevSwingLowCandleHigh)
                {
                    if (isDownCandle)
                    {
                        // Set current high as swing high first
                        CurrentTFSwingHighs[index] = currentHigh;
                        _lastSwingHighIndex = index;
                        _lastSwingHighValue = currentHigh;
                        _lastSwingWasHigh = true;
                        _lastSwingWasLow = false;

                        // Then set current low as swing low
                        CurrentTFSwingLows[index] = currentLow;
                        _lastSwingLowIndex = index;
                        _lastSwingLowValue = currentLow;
                        _lastSwingWasLow = true;
                        _lastSwingWasHigh = false;
                        
                        return; // Finished processing this candle
                    }
                    else if (isUpCandle)
                    {
                        // Move the swing low to current candle
                        CurrentTFSwingLows[_lastSwingLowIndex] = double.NaN;
                        CurrentTFSwingLows[index] = currentLow;
                        _lastSwingLowIndex = index;
                        _lastSwingLowValue = currentLow;

                        // Then set current high as swing high
                        CurrentTFSwingHighs[index] = currentHigh;
                        _lastSwingHighIndex = index;
                        _lastSwingHighValue = currentHigh;
                        _lastSwingWasHigh = true;
                        _lastSwingWasLow = false;
                        
                        return; // Finished processing this candle
                    }
                }
            }

            // Handle the special case where previous candle has a swing high
            if (_lastSwingWasHigh && _lastSwingHighIndex >= 0)
            {
                double prevSwingHighValue = Bars.HighPrices[_lastSwingHighIndex];
                double prevSwingHighCandleLow = Bars.LowPrices[_lastSwingHighIndex];

                if (currentHigh > prevSwingHighValue && currentLow < prevSwingHighCandleLow)
                {
                    if (isUpCandle)
                    {
                        // Set current low as swing low first
                        CurrentTFSwingLows[index] = currentLow;
                        _lastSwingLowIndex = index;
                        _lastSwingLowValue = currentLow;
                        _lastSwingWasLow = true;
                        _lastSwingWasHigh = false;

                        // Then set current high as swing high
                        CurrentTFSwingHighs[index] = currentHigh;
                        _lastSwingHighIndex = index;
                        _lastSwingHighValue = currentHigh;
                        _lastSwingWasHigh = true;
                        _lastSwingWasLow = false;
                        
                        return; // Finished processing this candle
                    }
                    else if (isDownCandle)
                    {
                        // Move the swing high to current candle
                        CurrentTFSwingHighs[_lastSwingHighIndex] = double.NaN;
                        CurrentTFSwingHighs[index] = currentHigh;
                        _lastSwingHighIndex = index;
                        _lastSwingHighValue = currentHigh;

                        // Then set current low as swing low
                        CurrentTFSwingLows[index] = currentLow;
                        _lastSwingLowIndex = index;
                        _lastSwingLowValue = currentLow;
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
                    CurrentTFSwingHighs[index] = currentHigh;
                    _lastSwingHighIndex = index;
                    _lastSwingHighValue = currentHigh;
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
                    CurrentTFSwingHighs[_lastSwingHighIndex] = double.NaN;
                    CurrentTFSwingHighs[index] = currentHigh;
                    _lastSwingHighIndex = index;
                    _lastSwingHighValue = currentHigh;
                    return; // Finished processing this candle
                }
                
                // Check if we should create a new swing low
                if (currentLow < Bars.LowPrices[_lastSwingHighIndex])
                {
                    // New swing low after a swing high
                    CurrentTFSwingLows[index] = currentLow;
                    _lastSwingLowIndex = index;
                    _lastSwingLowValue = currentLow;
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
                    CurrentTFSwingLows[index] = currentLow;
                    _lastSwingLowIndex = index;
                    _lastSwingLowValue = currentLow;
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
                    CurrentTFSwingLows[_lastSwingLowIndex] = double.NaN;
                    CurrentTFSwingLows[index] = currentLow;
                    _lastSwingLowIndex = index;
                    _lastSwingLowValue = currentLow;
                    return; // Finished processing this candle
                }
                
                // Check if we should create a new swing high
                if (currentHigh > Bars.HighPrices[_lastSwingLowIndex])
                {
                    // New swing high after a swing low
                    CurrentTFSwingHighs[index] = currentHigh;
                    _lastSwingHighIndex = index;
                    _lastSwingHighValue = currentHigh;
                    _lastSwingWasHigh = true;
                    _lastSwingWasLow = false;
                    return; // Finished processing this candle
                }
            }
        }
        
        private void CalculateHigherTimeframeSwings(int index)
        {
            // Find the corresponding higher timeframe bar for this current timeframe bar
            DateTime currentBarOpenTime = Bars.OpenTimes[index];
            int higherTFIndex = -1;
            
            // Declare the variable outside of the loop
            DateTime nextHigherTFOpenTime;
            
            // Find the higher timeframe bar that contains this bar
            for (int i = _higherTimeframeBars.Count - 1; i >= 0; i--)
            {
                DateTime higherTFOpenTime = _higherTimeframeBars.OpenTimes[i];
                nextHigherTFOpenTime = (i < _higherTimeframeBars.Count - 1) ? 
                    _higherTimeframeBars.OpenTimes[i + 1] : DateTime.MaxValue;
                    
                if (currentBarOpenTime >= higherTFOpenTime && currentBarOpenTime < nextHigherTFOpenTime)
                {
                    higherTFIndex = i;
                    break;
                }
            }
            
            if (higherTFIndex < 0 || higherTFIndex < 1 || higherTFIndex >= _higherTimeframeBars.Count - 1)
                return; // Not enough bars or no matching higher timeframe bar
            
            // Check if this is the last bar of the higher timeframe
            DateTime nextBarOpenTime = (index < Bars.Count - 1) ? Bars.OpenTimes[index + 1] : DateTime.MaxValue;
            DateTime thisHigherTFOpenTime = _higherTimeframeBars.OpenTimes[higherTFIndex];
            // Reuse the variable already declared above
            nextHigherTFOpenTime = (higherTFIndex < _higherTimeframeBars.Count - 1) ? 
                _higherTimeframeBars.OpenTimes[higherTFIndex + 1] : DateTime.MaxValue;
                
            bool isLastBarOfHigherTF = nextBarOpenTime >= nextHigherTFOpenTime;
            
            // Only calculate swing points on the last bar of each higher timeframe bar
            if (!isLastBarOfHigherTF)
                return;
            
            // Higher timeframe candle properties
            double htfCurrentHigh = _higherTimeframeBars.HighPrices[higherTFIndex];
            double htfCurrentLow = _higherTimeframeBars.LowPrices[higherTFIndex];
            double htfCurrentOpen = _higherTimeframeBars.OpenPrices[higherTFIndex];
            double htfCurrentClose = _higherTimeframeBars.ClosePrices[higherTFIndex];
            bool htfIsDownCandle = htfCurrentClose < htfCurrentOpen;
            bool htfIsUpCandle = htfCurrentClose > htfCurrentOpen;
            
            // Apply the same swing point logic for higher timeframe, but draw on current timeframe chart
            
            // Handle the special case where previous higher TF candle has a swing low
            if (_lastHigherTFSwingWasLow && _lastHigherTFSwingLowIndex >= 0)
            {
                double prevSwingLowValue = _higherTimeframeBars.LowPrices[_lastHigherTFSwingLowIndex];
                double prevSwingLowCandleHigh = _higherTimeframeBars.HighPrices[_lastHigherTFSwingLowIndex];

                if (htfCurrentLow < prevSwingLowValue && htfCurrentHigh > prevSwingLowCandleHigh)
                {
                    if (htfIsDownCandle)
                    {
                        // Set current high as swing high first
                        HigherTFSwingHighs[index] = htfCurrentHigh;
                        _lastHigherTFSwingHighIndex = higherTFIndex;
                        _lastHigherTFSwingHighValue = htfCurrentHigh;
                        _lastHigherTFSwingWasHigh = true;
                        _lastHigherTFSwingWasLow = false;

                        // Then set current low as swing low
                        HigherTFSwingLows[index] = htfCurrentLow;
                        _lastHigherTFSwingLowIndex = higherTFIndex;
                        _lastHigherTFSwingLowValue = htfCurrentLow;
                        _lastHigherTFSwingWasLow = true;
                        _lastHigherTFSwingWasHigh = false;
                        
                        return; // Finished processing this candle
                    }
                    else if (htfIsUpCandle)
                    {
                        // Find the index of the previous swing low in current timeframe
                        int prevSwingLowCurrentTFIndex = FindCurrentTimeframeIndexForHigherTimeframe(_lastHigherTFSwingLowIndex);
                        
                        if (prevSwingLowCurrentTFIndex >= 0)
                        {
                            // Move the swing low to current candle
                            HigherTFSwingLows[prevSwingLowCurrentTFIndex] = double.NaN;
                        }
                        
                        HigherTFSwingLows[index] = htfCurrentLow;
                        _lastHigherTFSwingLowIndex = higherTFIndex;
                        _lastHigherTFSwingLowValue = htfCurrentLow;

                        // Then set current high as swing high
                        HigherTFSwingHighs[index] = htfCurrentHigh;
                        _lastHigherTFSwingHighIndex = higherTFIndex;
                        _lastHigherTFSwingHighValue = htfCurrentHigh;
                        _lastHigherTFSwingWasHigh = true;
                        _lastHigherTFSwingWasLow = false;
                        
                        return; // Finished processing this candle
                    }
                }
            }

            // Handle the special case where previous higher TF candle has a swing high
            if (_lastHigherTFSwingWasHigh && _lastHigherTFSwingHighIndex >= 0)
            {
                double prevSwingHighValue = _higherTimeframeBars.HighPrices[_lastHigherTFSwingHighIndex];
                double prevSwingHighCandleLow = _higherTimeframeBars.LowPrices[_lastHigherTFSwingHighIndex];

                if (htfCurrentHigh > prevSwingHighValue && htfCurrentLow < prevSwingHighCandleLow)
                {
                    if (htfIsUpCandle)
                    {
                        // Set current low as swing low first
                        HigherTFSwingLows[index] = htfCurrentLow;
                        _lastHigherTFSwingLowIndex = higherTFIndex;
                        _lastHigherTFSwingLowValue = htfCurrentLow;
                        _lastHigherTFSwingWasLow = true;
                        _lastHigherTFSwingWasHigh = false;

                        // Then set current high as swing high
                        HigherTFSwingHighs[index] = htfCurrentHigh;
                        _lastHigherTFSwingHighIndex = higherTFIndex;
                        _lastHigherTFSwingHighValue = htfCurrentHigh;
                        _lastHigherTFSwingWasHigh = true;
                        _lastHigherTFSwingWasLow = false;
                        
                        return; // Finished processing this candle
                    }
                    else if (htfIsDownCandle)
                    {
                        // Find the index of the previous swing high in current timeframe
                        int prevSwingHighCurrentTFIndex = FindCurrentTimeframeIndexForHigherTimeframe(_lastHigherTFSwingHighIndex);
                        
                        if (prevSwingHighCurrentTFIndex >= 0)
                        {
                            // Move the swing high to current candle
                            HigherTFSwingHighs[prevSwingHighCurrentTFIndex] = double.NaN;
                        }
                        
                        HigherTFSwingHighs[index] = htfCurrentHigh;
                        _lastHigherTFSwingHighIndex = higherTFIndex;
                        _lastHigherTFSwingHighValue = htfCurrentHigh;

                        // Then set current low as swing low
                        HigherTFSwingLows[index] = htfCurrentLow;
                        _lastHigherTFSwingLowIndex = higherTFIndex;
                        _lastHigherTFSwingLowValue = htfCurrentLow;
                        _lastHigherTFSwingWasLow = true;
                        _lastHigherTFSwingWasHigh = false;
                        
                        return; // Finished processing this candle
                    }
                }
            }

            // Normal swing high detection logic for higher timeframe
            if (_lastHigherTFSwingWasLow || (!_lastHigherTFSwingWasHigh && !_lastHigherTFSwingWasLow))
            {
                // If last swing was a low or no swing yet
                if (htfCurrentHigh > _lastHigherTFSwingHighValue)
                {
                    // New swing high
                    HigherTFSwingHighs[index] = htfCurrentHigh;
                    _lastHigherTFSwingHighIndex = higherTFIndex;
                    _lastHigherTFSwingHighValue = htfCurrentHigh;
                    _lastHigherTFSwingWasHigh = true;
                    _lastHigherTFSwingWasLow = false;
                    return; // Finished processing this candle
                }
            }
            else if (_lastHigherTFSwingWasHigh && _lastHigherTFSwingHighIndex >= 0)
            {
                // If last swing was a high
                if (htfCurrentHigh > _lastHigherTFSwingHighValue)
                {
                    // Find the index of the previous swing high in current timeframe
                    int prevSwingHighCurrentTFIndex = FindCurrentTimeframeIndexForHigherTimeframe(_lastHigherTFSwingHighIndex);
                    
                    if (prevSwingHighCurrentTFIndex >= 0)
                    {
                        // Move swing high to current candle
                        HigherTFSwingHighs[prevSwingHighCurrentTFIndex] = double.NaN;
                    }
                    
                    HigherTFSwingHighs[index] = htfCurrentHigh;
                    _lastHigherTFSwingHighIndex = higherTFIndex;
                    _lastHigherTFSwingHighValue = htfCurrentHigh;
                    return; // Finished processing this candle
                }
                
                // Check if we should create a new swing low
                if (htfCurrentLow < _higherTimeframeBars.LowPrices[_lastHigherTFSwingHighIndex])
                {
                    // New swing low after a swing high
                    HigherTFSwingLows[index] = htfCurrentLow;
                    _lastHigherTFSwingLowIndex = higherTFIndex;
                    _lastHigherTFSwingLowValue = htfCurrentLow;
                    _lastHigherTFSwingWasLow = true;
                    _lastHigherTFSwingWasHigh = false;
                    return; // Finished processing this candle
                }
            }

            // Normal swing low detection logic for higher timeframe
            if (_lastHigherTFSwingWasHigh || (!_lastHigherTFSwingWasHigh && !_lastHigherTFSwingWasLow))
            {
                // If last swing was a high or no swing yet
                if (htfCurrentLow < _lastHigherTFSwingLowValue)
                {
                    // New swing low
                    HigherTFSwingLows[index] = htfCurrentLow;
                    _lastHigherTFSwingLowIndex = higherTFIndex;
                    _lastHigherTFSwingLowValue = htfCurrentLow;
                    _lastHigherTFSwingWasLow = true;
                    _lastHigherTFSwingWasHigh = false;
                    return; // Finished processing this candle
                }
            }
            else if (_lastHigherTFSwingWasLow && _lastHigherTFSwingLowIndex >= 0)
            {
                // If last swing was a low
                if (htfCurrentLow < _lastHigherTFSwingLowValue)
                {
                    // Find the index of the previous swing low in current timeframe
                    int prevSwingLowCurrentTFIndex = FindCurrentTimeframeIndexForHigherTimeframe(_lastHigherTFSwingLowIndex);
                    
                    if (prevSwingLowCurrentTFIndex >= 0)
                    {
                        // Move swing low to current candle
                        HigherTFSwingLows[prevSwingLowCurrentTFIndex] = double.NaN;
                    }
                    
                    HigherTFSwingLows[index] = htfCurrentLow;
                    _lastHigherTFSwingLowIndex = higherTFIndex;
                    _lastHigherTFSwingLowValue = htfCurrentLow;
                    return; // Finished processing this candle
                }
                
                // Check if we should create a new swing high
                if (htfCurrentHigh > _higherTimeframeBars.HighPrices[_lastHigherTFSwingLowIndex])
                {
                    // New swing high after a swing low
                    HigherTFSwingHighs[index] = htfCurrentHigh;
                    _lastHigherTFSwingHighIndex = higherTFIndex;
                    _lastHigherTFSwingHighValue = htfCurrentHigh;
                    _lastHigherTFSwingWasHigh = true;
                    _lastHigherTFSwingWasLow = false;
                    return; // Finished processing this candle
                }
            }
        }
        
        private int FindCurrentTimeframeIndexForHigherTimeframe(int higherTFIndex)
        {
            if (higherTFIndex < 0 || higherTFIndex >= _higherTimeframeBars.Count)
                return -1;
                
            DateTime higherTFOpenTime = _higherTimeframeBars.OpenTimes[higherTFIndex];
            long openTimeTicks = higherTFOpenTime.Ticks;
            
            if (_higherTFBarOpenTimes.TryGetValue(openTimeTicks, out int currentTFIndex))
            {
                return currentTFIndex;
            }
            
            return -1;
        }
        
        private TimeFrame GetTimeFrameFromString(string timeframeStr)
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