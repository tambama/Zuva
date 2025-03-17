using System;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Indicators.Zuva
{
    /// <summary>
    /// Processes the higher timeframe data and maps it to the current timeframe
    /// </summary>
    public class HigherTimeframeProcessor
    {
        private readonly Bars _currentTFBars;
        private readonly Bars _higherTFBars;
        private readonly IndicatorDataSeries _higherTFSwingHighs;
        private readonly IndicatorDataSeries _higherTFSwingLows;
        private readonly Dictionary<long, int> _higherTFBarOpenTimes;

        private readonly SwingPointDetector _swingPointDetector;
        
        private int _lastHigherTFProcessedIndex = -1;
        
        public HigherTimeframeProcessor(
            Bars currentTFBars,
            Bars higherTFBars,
            IndicatorDataSeries higherTFSwingHighs,
            IndicatorDataSeries higherTFSwingLows)
        {
            _currentTFBars = currentTFBars;
            _higherTFBars = higherTFBars;
            _higherTFSwingHighs = higherTFSwingHighs;
            _higherTFSwingLows = higherTFSwingLows;
            
            _higherTFBarOpenTimes = TimeframeHelper.MapHigherTimeframeToCurrent(currentTFBars, higherTFBars);
            
            // Create special detector just for higher timeframe data
            _swingPointDetector = new SwingPointDetector(higherTFBars, higherTFSwingHighs, higherTFSwingLows);
        }
        
        public void ProcessBar(int currentTFIndex)
        {
            // Find the corresponding higher timeframe bar for this current timeframe bar
            DateTime currentBarOpenTime = _currentTFBars.OpenTimes[currentTFIndex];
            int higherTFIndex = -1;
            
            // Declare the variable outside of the loop
            DateTime nextHigherTFOpenTime;
            
            // Find the higher timeframe bar that contains this bar
            for (int i = _higherTFBars.Count - 1; i >= 0; i--)
            {
                DateTime higherTFOpenTime = _higherTFBars.OpenTimes[i];
                nextHigherTFOpenTime = (i < _higherTFBars.Count - 1) ? 
                    _higherTFBars.OpenTimes[i + 1] : DateTime.MaxValue;
                    
                if (currentBarOpenTime >= higherTFOpenTime && currentBarOpenTime < nextHigherTFOpenTime)
                {
                    higherTFIndex = i;
                    break;
                }
            }
            
            if (higherTFIndex < 0 || higherTFIndex < 1 || higherTFIndex >= _higherTFBars.Count - 1)
                return; // Not enough bars or no matching higher timeframe bar
            
            // Only process each higher timeframe bar once
            if (_lastHigherTFProcessedIndex == higherTFIndex)
                return;
            
            // Check if this is the last bar of the higher timeframe
            bool isLastBarOfHigherTF = TimeframeHelper.IsLastBarOfHigherTimeframe(
                currentTFIndex, 
                _currentTFBars, 
                higherTFIndex, 
                _higherTFBars);
                
            // Only calculate swing points on the last bar of each higher timeframe bar
            if (!isLastBarOfHigherTF)
                return;
                
            // Process the higher timeframe bar
            _swingPointDetector.ProcessBar(higherTFIndex);
            
            // Remember this higher TF bar has been processed
            _lastHigherTFProcessedIndex = higherTFIndex;
        }
    }
}