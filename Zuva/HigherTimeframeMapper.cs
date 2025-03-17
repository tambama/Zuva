using System;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Internals;

namespace cAlgo
{
    /// <summary>
    /// Maps between current timeframe and higher timeframe indices
    /// </summary>
    public class HigherTimeframeMapper
    {
        private readonly Bars _currentTimeframeBars;
        private readonly Bars _higherTimeframeBars;
        private readonly Dictionary<long, int> _higherTFBarOpenTimes = new Dictionary<long, int>();
        private readonly Dictionary<int, int> _higherToCurrentTFMap = new Dictionary<int, int>();

        public HigherTimeframeMapper(Bars currentTFBars, Bars higherTFBars)
        {
            _currentTimeframeBars = currentTFBars;
            _higherTimeframeBars = higherTFBars;
            
            // Create mappings between timeframes
            InitializeTimeframeMappings();
        }

        private void InitializeTimeframeMappings()
        {
            // Create a mapping between higher timeframe bar open times and current timeframe indices
            for (int htfIndex = 0; htfIndex < _higherTimeframeBars.Count; htfIndex++)
            {
                DateTime higherTFOpenTime = _higherTimeframeBars.OpenTimes[htfIndex];
                long openTimeTicks = higherTFOpenTime.Ticks;
                
                // Find the first current timeframe bar that corresponds to this higher timeframe bar
                for (int ctfIndex = 0; ctfIndex < _currentTimeframeBars.Count; ctfIndex++)
                {
                    if (_currentTimeframeBars.OpenTimes[ctfIndex] >= higherTFOpenTime)
                    {
                        _higherTFBarOpenTimes[openTimeTicks] = ctfIndex;
                        _higherToCurrentTFMap[htfIndex] = ctfIndex;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the higher timeframe index that corresponds to the given current timeframe index
        /// </summary>
        public int GetHigherTimeframeIndex(int currentTFIndex)
        {
            if (currentTFIndex < 0 || currentTFIndex >= _currentTimeframeBars.Count)
                return -1;
                
            DateTime currentBarOpenTime = _currentTimeframeBars.OpenTimes[currentTFIndex];
            
            // Find the higher timeframe bar that contains this current timeframe bar
            for (int htfIndex = _higherTimeframeBars.Count - 1; htfIndex >= 0; htfIndex--)
            {
                DateTime higherTFOpenTime = _higherTimeframeBars.OpenTimes[htfIndex];
                DateTime nextHigherTFOpenTime = (htfIndex < _higherTimeframeBars.Count - 1) ? 
                    _higherTimeframeBars.OpenTimes[htfIndex + 1] : DateTime.MaxValue;
                    
                if (currentBarOpenTime >= higherTFOpenTime && currentBarOpenTime < nextHigherTFOpenTime)
                {
                    return htfIndex;
                }
            }
            
            return -1;
        }

        /// <summary>
        /// Checks if the current timeframe bar is the last bar of its corresponding higher timeframe bar
        /// </summary>
        public bool IsLastBarOfHigherTimeframe(int currentTFIndex)
        {
            // Handle the case where we're looking at the very last bar
            if (currentTFIndex == _currentTimeframeBars.Count - 1)
                return true;
                
            if (currentTFIndex < 0 || currentTFIndex >= _currentTimeframeBars.Count - 1)
                return false;
                
            int higherTFIndex = GetHigherTimeframeIndex(currentTFIndex);
            
            // If we can't find a higher TF index, we'll just say no
            if (higherTFIndex < 0)
                return false;
            
            // If this is the last higher TF bar, and we're at the last current TF bar, then yes
            if (higherTFIndex == _higherTimeframeBars.Count - 1 && currentTFIndex == _currentTimeframeBars.Count - 1)
                return true;
                
            // If this is the last higher TF bar but not the last current TF bar, need special handling
            if (higherTFIndex == _higherTimeframeBars.Count - 1)
            {
                // Since there's no next higher TF bar, we can't use the standard check
                // So we'll say it's not the last bar (something will be eventually)
                return false;
            }
                
            // Standard case - check if the next bar belongs to the next higher timeframe bar
            DateTime nextBarOpenTime = _currentTimeframeBars.OpenTimes[currentTFIndex + 1];
            DateTime nextHigherTFOpenTime = _higherTimeframeBars.OpenTimes[higherTFIndex + 1];
            
            bool result = nextBarOpenTime >= nextHigherTFOpenTime;
            
            return result;
        }

        /// <summary>
        /// Gets the current timeframe index that corresponds to the given higher timeframe index
        /// </summary>
        public int GetCurrentTimeframeIndex(int higherTFIndex)
        {
            if (_higherToCurrentTFMap.TryGetValue(higherTFIndex, out int currentTFIndex))
            {
                return currentTFIndex;
            }
            
            return -1;
        }
    }
}