using System;
using cAlgo.API;
using cAlgo.API.Internals;
using Zuva.Interfaces;
using Zuva.Models;

namespace Zuva.Services
{
    public class MarketDataProvider : IMarketDataProvider
    {
        private readonly Symbols _symbols;
        private readonly MarketData _marketData;
        private readonly TimeFrame _timeFrame;
        private readonly Action<string> _logger;
        
        private Symbol _pairSymbol;
        private Bars _pairBars;

        public MarketDataProvider(
            Symbols symbols,
            MarketData marketData,
            TimeFrame timeFrame,
            Action<string> logger)
        {
            _symbols = symbols;
            _marketData = marketData;
            _timeFrame = timeFrame;
            _logger = logger ?? (_ => { });
        }

        public bool InitializePairSymbol(string smtPair)
        {
            if (string.IsNullOrEmpty(smtPair))
                return false;

            try
            {
                _pairSymbol = _symbols.GetSymbol(smtPair);
                if (_pairSymbol != null)
                {
                    _pairBars = _marketData.GetBars(_timeFrame, smtPair);
                    return true;
                }
                else
                {
                    _logger($"Symbol '{smtPair}' not found. SMT functionality will be disabled.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger($"Error initializing pair symbol: {ex.Message}");
                return false;
            }
        }

        public double GetPairPrice(string pairSymbol, DateTime time, int index, Direction direction)
        {
            if (_pairSymbol == null || _pairBars == null)
            {
                _logger($"No pair symbol or bars initialized for {pairSymbol}");
                return 0;
            }

            if (_pairBars.Count == 0)
            {
                _logger($"Pair bars collection is empty for {pairSymbol}");
                return 0;
            }

            try
            {
                // Method 1: Try to find the bar at the exact same time
                for (int i = 0; i < _pairBars.Count; i++)
                {
                    if (_pairBars[i].OpenTime == time)
                    {
                        return direction == Direction.Up ? _pairBars[i].High : _pairBars[i].Low;
                    }
                }

                // Method 2: Find the closest bar
                int closestIndex = FindClosestBarIndex(time);
                if (closestIndex >= 0)
                {
                    return direction == Direction.Up ? _pairBars[closestIndex].High : _pairBars[closestIndex].Low;
                }

                // Method 3: Use the same index if it's in range
                if (index < _pairBars.Count)
                {
                    return direction == Direction.Up ? _pairBars[index].High : _pairBars[index].Low;
                }

                _logger($"Could not find matching bar for {pairSymbol} at time {time}");
                return 0;
            }
            catch (Exception ex)
            {
                _logger($"Error getting pair price: {ex.Message}");
                return 0;
            }
        }

        private int FindClosestBarIndex(DateTime time)
        {
            int closestIndex = -1;
            TimeSpan minTimeDiff = TimeSpan.MaxValue;

            for (int i = 0; i < _pairBars.Count; i++)
            {
                TimeSpan timeDiff = _pairBars[i].OpenTime > time
                    ? _pairBars[i].OpenTime - time
                    : time - _pairBars[i].OpenTime;

                if (timeDiff < minTimeDiff)
                {
                    minTimeDiff = timeDiff;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }
    }
}