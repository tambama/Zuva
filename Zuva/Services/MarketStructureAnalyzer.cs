using cAlgo.API;
using Mwenje.Extensions;
using Zuva.Models;
using System.Collections.Generic;
using System.Linq;

namespace Zuva.Services
{
    /// <summary>
    /// Analyzes market structure based on swing points to identify patterns like
    /// Break of Structure (BOS), Change of Character (CHoCH), inducements, and liquidity points
    /// </summary>
    public class MarketStructureAnalyzer
    {
        private readonly Chart _chart;
        private readonly IndicatorDataSeries _highs;
        private readonly IndicatorDataSeries _lows;
        private readonly IndicatorDataSeries _hhs; // Higher Highs
        private readonly IndicatorDataSeries _lhs; // Lower Highs
        private readonly IndicatorDataSeries _lls; // Lower Lows
        private readonly IndicatorDataSeries _hls; // Higher Lows
        private readonly bool _showStructure;
        private readonly bool _showChoch;
        
        // Add a delegate for logging
        private readonly Action<string> _logger;

        // Market structure state
        private Direction _bias = Direction.Up; // Current market bias

        // Break of Structure points
        private SwingPoint _highBOS;
        private SwingPoint _lowBOS;

        // Change of Character points
        private SwingPoint _highCHOCH;
        private SwingPoint _lowCHOCH;

        // Inducement points
        private SwingPoint _highIND;
        private SwingPoint _lowIND;

        // Previous inducement tracking
        private DateTime? _previousHighIndTime;
        private DateTime? _previousLowIndTime;

        // Lists for ordered swing points 
        private List<SwingPoint> _orderedHighs = new List<SwingPoint>();
        private List<SwingPoint> _orderedLows = new List<SwingPoint>();

        // Collection for external liquidity points
        private List<SwingPoint> _externalLiquidity = new List<SwingPoint>();

        // Reference to all swing points
        private List<SwingPoint> _swingPoints = new List<SwingPoint>();

        // Flag to track if we're properly initialized
        private bool _isInitialized = false;

        public MarketStructureAnalyzer(
            Chart chart,
            IndicatorDataSeries highs,
            IndicatorDataSeries lows,
            IndicatorDataSeries hhs,
            IndicatorDataSeries lhs,
            IndicatorDataSeries lls,
            IndicatorDataSeries hls,
            bool showStructure,
            bool showChoch,
            Action<string> logger = null)
        {
            _chart = chart;
            _highs = highs;
            _lows = lows;
            _hhs = hhs;
            _lhs = lhs;
            _lls = lls;
            _hls = hls;
            _showStructure = showStructure;
            _showChoch = showChoch;
            _logger = logger ?? (_ => { });
        }

        /// <summary>
        /// Initializes the market structure analysis with the initial set of swing points
        /// </summary>
        public void Initialize(List<SwingPoint> swingPoints)
        {
            if (swingPoints == null || swingPoints.Count < 2)
                return;

            _swingPoints = swingPoints;

            // Get ordered highs and lows
            _orderedHighs = swingPoints.Where(s => s.SwingType == SwingType.H)
                .OrderByDescending(s => s.Index)
                .ToList();

            _orderedLows = swingPoints.Where(s => s.SwingType == SwingType.L)
                .OrderByDescending(s => s.Index)
                .ToList();

            // Only proceed if we have at least one high and one low
            if (_orderedHighs.Count == 0 || _orderedLows.Count == 0)
                return;

            // Set initial BOS points
            _highBOS = _orderedHighs[0];
            if (_showStructure)
            {
                _chart.DrawIcon($"{_highBOS.Time}-bos", ChartIconType.Circle, _highBOS.Time, _highBOS.Price,
                    Color.Pink);
            }

            _lowBOS = _orderedLows[0];
            if (_showStructure)
            {
                _chart.DrawIcon($"{_lowBOS.Time}-bos", ChartIconType.Circle, _lowBOS.Time, _lowBOS.Price, Color.Pink);
            }

            // Determine initial bias based on the relative positions of the latest highs and lows
            DetermineInitialBias();

            _isInitialized = true;
        }

        /// <summary>
        /// Determines the initial market bias based on the latest swing points
        /// </summary>
        private void DetermineInitialBias()
        {
            // If we have at least 2 highs and 2 lows, we can determine the bias based on their patterns
            if (_orderedHighs.Count >= 2 && _orderedLows.Count >= 2)
            {
                var latestHigh = _orderedHighs[0];
                var prevHigh = _orderedHighs[1];
                var latestLow = _orderedLows[0];
                var prevLow = _orderedLows[1];

                bool higherHigh = latestHigh.Price > prevHigh.Price;
                bool higherLow = latestLow.Price > prevLow.Price;

                // Uptrend: Higher Highs and Higher Lows
                if (higherHigh && higherLow)
                {
                    _bias = Direction.Up;

                    // Draw green icons for HH and HL
                    if (_showStructure)
                    {
                        _chart.DrawIcon($"{latestHigh.Time}-hh", ChartIconType.Circle, latestHigh.Time,
                            latestHigh.Price, Color.Green);
                        _chart.DrawIcon($"{latestLow.Time}-hl", ChartIconType.Circle, latestLow.Time,
                            latestLow.Price, Color.Red);
                    }

                    // Mark in the indicator series
                    if (latestHigh.Index >= 0 && latestHigh.Index < _hhs.Count)
                        _hhs[latestHigh.Index] = latestHigh.Price;

                    if (latestLow.Index >= 0 && latestLow.Index < _hls.Count)
                        _hls[latestLow.Index] = latestLow.Price;
                }
                // Downtrend: Lower Highs and Lower Lows
                else if (!higherHigh && !higherLow)
                {
                    _bias = Direction.Down;

                    // Draw red icons for LH and LL
                    if (_showStructure)
                    {
                        _chart.DrawIcon($"{latestHigh.Time}-lh", ChartIconType.Circle, latestHigh.Time,
                            latestHigh.Price, Color.Green);
                        _chart.DrawIcon($"{latestLow.Time}-ll", ChartIconType.Circle, latestLow.Time,
                            latestLow.Price, Color.Red);
                    }

                    // Mark in the indicator series
                    if (latestHigh.Index >= 0 && latestHigh.Index < _lhs.Count)
                        _lhs[latestHigh.Index] = latestHigh.Price;

                    if (latestLow.Index >= 0 && latestLow.Index < _lls.Count)
                        _lls[latestLow.Index] = latestLow.Price;
                }
                // Mixed signals - use the latest swing point direction
                else
                {
                    _bias = latestHigh.Index > latestLow.Index ? Direction.Up : Direction.Down;
                }
            }
            // If we don't have enough swing points, we can use the most recent swing point
            else if (_swingPoints.Count > 0)
            {
                var latestPoint = _swingPoints.OrderByDescending(s => s.Index).FirstOrDefault();
                if (latestPoint != null)
                {
                    _bias = latestPoint.SwingType == SwingType.H ? Direction.Up : Direction.Down;
                }
            }

            // Always update the bias on the chart regardless of how we determined it
            UpdateBiasOnChart();
        }

        /// <summary>
        /// Processes a new swing point to update market structure analysis
        /// </summary>
        public void ProcessSwingPoint(SwingPoint swingPoint)
        {
            // Skip if we haven't been properly initialized
            if (!_isInitialized || swingPoint == null || _highBOS == null || _lowBOS == null)
                return;

            // Update ordered lists
            if (swingPoint.SwingType == SwingType.H)
            {
                _orderedHighs.Insert(0, swingPoint);
            }
            else
            {
                _orderedLows.Insert(0, swingPoint);
            }

            if (swingPoint.Direction == Direction.Up)
            {
                // Compare with _highBOS only if it exists
                if (_highBOS != null && swingPoint.Price > _highBOS.Price)
                {
                    _highBOS = swingPoint;

                    // Only set _lowCHOCH if _lowBOS exists
                    if (_lowBOS != null)
                    {
                        _lowCHOCH = _lowBOS;

                        // Only try to mark the low in the series if we have a valid index
                        if (_lowBOS.Index >= 0 && _lowBOS.Index < _lows.Count)
                        {
                            _lows[_lowBOS.Index] = _lowBOS.Price;

                            // Find the low in ordered lows and check if it exists before manipulating
                            var low = _orderedLows.FirstOrDefault(p => p.Index == _lowBOS.Index);
                            if (low != null)
                            {
                                low.SwingType = SwingType.LL;
                                var liquidity = _externalLiquidity.Any(l => l.Index == _lowBOS.Index);
                                if (!liquidity)
                                {
                                    _externalLiquidity.Add(low);
                                }

                                // Draw a red icon for LL confirmation
                                if (_showStructure)
                                {
                                    _chart.DrawIcon($"{low.Time}-ll", ChartIconType.Circle, low.Time, low.Price,
                                        Color.Red);
                                }

                                // Mark in indicator series
                                if (low.Index >= 0 && low.Index < _lls.Count)
                                    _lls[low.Index] = low.Price;
                            }
                        }
                    }

                    if (_bias == Direction.Up && _orderedLows.Count > 0)
                    {
                        // Clean up previous inducement
                        CleanupPreviousInducement(_previousHighIndTime, true);

                        _highIND = _orderedLows[0];
                        _previousHighIndTime = _highIND.Time;

                        // Draw a pink icon for inducement
                        if (_showStructure)
                        {
                            _chart.DrawIcon($"{_highIND.Time}-ind", ChartIconType.Diamond, _highIND.Time,
                                _highIND.Price, Color.Pink);
                        }
                    }

                    var point = _swingPoints.FirstOrDefault(s => s.Index == _highBOS.Index);
                    if (point != null)
                    {
                        point.Swept = true;
                        point.SwingType = SwingType.HH;

                        // Mark in indicator series
                        if (point.Index >= 0 && point.Index < _hhs.Count)
                            _hhs[point.Index] = point.Price;
                    }
                }

                // Mark Low point after taking out inducement in a downtrend
                if (_bias == Direction.Down && _lowIND != null && swingPoint.Bar.High > _lowIND.Price)
                {
                    // Draw a line for swept inducement
                    if (_showChoch)
                    {
                        _chart.DrawStraightLine(
                            $"ind-sweep-{_lowIND.Time}-{swingPoint.Time}",
                            _lowIND.Time,
                            _lowIND.Price,
                            swingPoint.Time,
                            _lowIND.Price,
                            null,
                            LineStyle.Solid,
                            Color.LightGray,
                            false,
                            true,
                            false
                        );
                    }
                    else
                    {
                        // Draw small 2-minute inducement line
                        _chart.DrawStraightLine(
                            $"ind-sweep-small-{_lowIND.Time}",
                            _lowIND.Time,
                            _lowIND.Price,
                            _lowIND.Time.AddMinutes(2),
                            _lowIND.Price,
                            null,
                            LineStyle.Solid,
                            Color.LightGray,
                            false,
                            true,
                            false
                        );
                    }

                    if (_lowBOS != null)
                    {
                        var point = _swingPoints.FirstOrDefault(s => s.Index == _lowBOS.Index);
                        if (point != null)
                        {
                            point.SwingType = SwingType.LL;

                            // Only try to mark the low in the series if we have a valid index
                            if (point.Index >= 0 && point.Index < _lows.Count)
                            {
                                _lows[point.Index] = point.Price;
                            }

                            var liquidity = _externalLiquidity.Any(l => l.Index == point.Index);
                            if (!liquidity)
                            {
                                _externalLiquidity.Add(point);
                            }

                            // Draw a red icon for LL confirmation
                            if (_showStructure)
                            {
                                _chart.DrawIcon($"{point.Time}-ll", ChartIconType.Circle, point.Time, point.Price,
                                    Color.Red);
                            }

                            // Mark in indicator series
                            if (point.Index >= 0 && point.Index < _lls.Count)
                                _lls[point.Index] = point.Price;
                        }
                    }

                    // Clear the inducement
                    _highIND = null;
                    _highBOS = swingPoint;
                    _lowIND = null;
                    _previousLowIndTime = null;
                }

                // Change of Character
                if (_highCHOCH != null && swingPoint.Price > _highCHOCH.Price)
                {
                    var point = _swingPoints.FirstOrDefault(s => s.Index == _highCHOCH.Index);
                    if (point != null)
                    {
                        point.Swept = true;

                        // Draw a straight trendline from the CHOCH point to the confirming candle
                        if (_showChoch)
                        {
                            _chart.DrawStraightLine(
                                $"choch-{point.Time}-{swingPoint.Time}",
                                point.Time,
                                point.Price,
                                swingPoint.Time,
                                point.Price,
                                null,
                                LineStyle.Solid,
                                Color.Red,
                                false,
                                true,
                                false
                            );
                        }
                        else
                        {
                            // Draw small 2-minute CHOCH line
                            _chart.DrawStraightLine(
                                $"choch-small-{point.Time}",
                                point.Time,
                                point.Price,
                                point.Time.AddMinutes(2),
                                point.Price,
                                null,
                                LineStyle.Solid,
                                Color.Red,
                                false,
                                true,
                                false
                            );
                        }
                    }

                    _highBOS = swingPoint;

                    if (_orderedLows.Count > 0)
                    {
                        // Clean up previous inducement
                        CleanupPreviousInducement(_previousHighIndTime, true);

                        _highIND = _orderedLows[0];
                        _previousHighIndTime = _highIND.Time;

                        // Draw a pink icon for inducement
                        if (_showStructure)
                        {
                            _chart.DrawIcon($"{_highIND.Time}-ind", ChartIconType.Diamond, _highIND.Time,
                                _highIND.Price, Color.Pink);
                        }
                    }

                    _highCHOCH = null;
                    _bias = Direction.Up;

                    if (_lowCHOCH != null && _lowCHOCH.Index >= 0 && _lowCHOCH.Index < _lows.Count)
                    {
                        _lows[_lowCHOCH.Index] = _lowCHOCH.Price;

                        var low = _swingPoints.FirstOrDefault(s => s.Index == _lowCHOCH.Index);
                        if (low != null)
                        {
                            low.SwingType = SwingType.LL;

                            var liquidity = _externalLiquidity.Any(l => l.Index == _lowCHOCH.Index);
                            if (!liquidity)
                            {
                                _externalLiquidity.Add(low);
                            }

                            // Draw a red icon for LL confirmation
                            if (_showStructure)
                            {
                                _chart.DrawIcon($"{low.Time}-ll", ChartIconType.Circle, low.Time, low.Price, Color.Red);
                            }

                            // Mark in indicator series
                            if (low.Index >= 0 && low.Index < _lls.Count)
                                _lls[low.Index] = low.Price;
                        }
                    }
                }
            }
            else // Direction.Down
            {
                // Compare with _lowBOS only if it exists
                if (_lowBOS != null && swingPoint.Price < _lowBOS.Price)
                {
                    _lowBOS = swingPoint;

                    // Only set _highCHOCH if _highBOS exists
                    if (_highBOS != null)
                    {
                        _highCHOCH = _highBOS;

                        // Only try to mark the high in the series if we have a valid index
                        if (_highBOS.Index >= 0 && _highBOS.Index < _highs.Count)
                        {
                            _highs[_highBOS.Index] = _highBOS.Price;

                            // Find the high in ordered highs and check if it exists before manipulating
                            var high = _orderedHighs.FirstOrDefault(p => p.Index == _highBOS.Index);
                            if (high != null)
                            {
                                high.SwingType = SwingType.HH;

                                var liquidity = _externalLiquidity.Any(l => l.Index == _highBOS.Index);
                                if (!liquidity)
                                {
                                    _externalLiquidity.Add(high);
                                }

                                // Draw a green icon for HH confirmation
                                if (_showStructure)
                                {
                                    _chart.DrawIcon($"{high.Time}-hh", ChartIconType.Circle, high.Time, high.Price,
                                        Color.Green);
                                }

                                // Mark in indicator series
                                if (high.Index >= 0 && high.Index < _hhs.Count)
                                    _hhs[high.Index] = high.Price;
                            }
                        }
                    }

                    if (_bias == Direction.Down && _orderedHighs.Count > 0)
                    {
                        // Clean up previous inducement
                        CleanupPreviousInducement(_previousLowIndTime, false);

                        _lowIND = _orderedHighs[0];
                        _previousLowIndTime = _lowIND.Time;

                        // Draw a pink icon for inducement
                        if (_showStructure)
                        {
                            _chart.DrawIcon($"{_lowIND.Time}-ind", ChartIconType.Diamond, _lowIND.Time, _lowIND.Price,
                                Color.Pink);
                        }
                    }

                    var point = _swingPoints.FirstOrDefault(s => s.Index == _lowBOS.Index);
                    if (point != null)
                    {
                        point.Swept = true;
                        point.SwingType = SwingType.LL;

                        // Mark in indicator series
                        if (point.Index >= 0 && point.Index < _lls.Count)
                            _lls[point.Index] = point.Price;
                    }
                }

                // Mark High point after taking out inducement in an uptrend
                if (_bias == Direction.Up && _highIND != null && swingPoint.Bar.Low < _highIND.Price)
                {
                    // Draw a line for swept inducement
                    if (_showChoch)
                    {
                        _chart.DrawStraightLine(
                            $"ind-sweep-{_highIND.Time}-{swingPoint.Time}",
                            _highIND.Time,
                            _highIND.Price,
                            swingPoint.Time,
                            _highIND.Price,
                            null,
                            LineStyle.Solid,
                            Color.LightGray,
                            false,
                            true,
                            false
                        );
                    }
                    else
                    {
                        // Draw small 2-minute inducement line
                        _chart.DrawStraightLine(
                            $"ind-sweep-small-{_highIND.Time}",
                            _highIND.Time,
                            _highIND.Price,
                            _highIND.Time.AddMinutes(2),
                            _highIND.Price,
                            null,
                            LineStyle.Solid,
                            Color.LightGray,
                            false,
                            true,
                            false
                        );
                    }

                    if (_highBOS != null)
                    {
                        var point = _swingPoints.FirstOrDefault(s => s.Index == _highBOS.Index);
                        if (point != null)
                        {
                            point.SwingType = SwingType.HH;

                            // Only try to mark the high in the series if we have a valid index
                            if (point.Index >= 0 && point.Index < _highs.Count)
                            {
                                _highs[point.Index] = point.Price;
                            }

                            var liquidity = _externalLiquidity.Any(l => l.Index == point.Index);
                            if (!liquidity)
                            {
                                _externalLiquidity.Add(point);
                            }

                            // Draw a green icon for HH confirmation
                            if (_showStructure)
                            {
                                _chart.DrawIcon($"{point.Time}-hh", ChartIconType.Circle, point.Time, point.Price,
                                    Color.Green);
                            }

                            // Mark in indicator series
                            if (point.Index >= 0 && point.Index < _hhs.Count)
                                _hhs[point.Index] = point.Price;
                        }
                    }

                    // Clear the inducement
                    _lowIND = null;
                    _lowBOS = swingPoint;
                    _highIND = null;
                    _previousHighIndTime = null;
                }

                // Change of Character
                if (_lowCHOCH != null && swingPoint.Price < _lowCHOCH.Price)
                {
                    var point = _swingPoints.FirstOrDefault(s => s.Index == _lowCHOCH.Index);
                    if (point != null)
                    {
                        point.Swept = true;

                        // Draw a straight trendline from the CHOCH point to the confirming candle
                        if (_showChoch)
                        {
                            _chart.DrawStraightLine(
                                $"choch-{point.Time}-{swingPoint.Time}",
                                point.Time,
                                point.Price,
                                swingPoint.Time,
                                point.Price,
                                null,
                                LineStyle.Solid,
                                Color.Red,
                                false,
                                true,
                                false
                            );
                        }
                        else
                        {
                            // Draw small 2-minute CHOCH line
                            _chart.DrawStraightLine(
                                $"choch-small-{point.Time}",
                                point.Time,
                                point.Price,
                                point.Time.AddMinutes(2),
                                point.Price,
                                null,
                                LineStyle.Solid,
                                Color.Red,
                                false,
                                true,
                                false
                            );
                        }
                    }

                    _lowBOS = swingPoint;

                    if (_orderedHighs.Count > 0)
                    {
                        // Clean up previous inducement
                        CleanupPreviousInducement(_previousLowIndTime, false);

                        _lowIND = _orderedHighs[0];
                        _previousLowIndTime = _lowIND.Time;

                        // Draw a pink icon for inducement
                        if (_showStructure)
                        {
                            _chart.DrawIcon($"{_lowIND.Time}-ind", ChartIconType.Diamond, _lowIND.Time, _lowIND.Price,
                                Color.Pink);
                        }
                    }

                    _lowCHOCH = null;
                    _bias = Direction.Down;

                    if (_highBOS != null && _highBOS.Index >= 0 && _highBOS.Index < _highs.Count)
                    {
                        _highs[_highBOS.Index] = _highBOS.Price;

                        var high = _swingPoints.FirstOrDefault(s => s.Index == _highBOS.Index);
                        if (high != null)
                        {
                            high.SwingType = SwingType.HH;

                            var liquidity = _externalLiquidity.Any(l => l.Index == _highBOS.Index);
                            if (!liquidity && _highBOS != null)
                            {
                                _externalLiquidity.Add(_highBOS);
                            }

                            // Draw a green icon for HH confirmation
                            if (_showStructure)
                            {
                                _chart.DrawIcon($"{high.Time}-hh", ChartIconType.Circle, high.Time, high.Price,
                                    Color.Green);
                            }

                            // Mark in indicator series
                            if (high.Index >= 0 && high.Index < _hhs.Count)
                                _hhs[high.Index] = high.Price;
                        }
                    }
                }
            }

            UpdateBiasOnChart();
        }

        /// <summary>
        /// Updates the chart with the current market bias
        /// </summary>
        private void UpdateBiasOnChart()
        {
            try
            {
                if (_chart != null)
                {
                    _chart.UpdateBias(_bias);
                }
            }
            catch (Exception ex)
            {
                // Cannot use Print - must be handled externally
            }
        }

        /// <summary>
        /// Gets all identified external liquidity points
        /// </summary>
        public List<SwingPoint> GetExternalLiquidityPoints()
        {
            return _externalLiquidity;
        }

        /// <summary>
        /// Gets the current market bias
        /// </summary>
        public Direction GetBias()
        {
            return _bias;
        }

        // Method to clean up previous inducement icons
        private void CleanupPreviousInducement(DateTime? previousTime, bool isHigh)
        {
            if (previousTime.HasValue)
            {
                string iconId = $"{previousTime.Value}-ind";
                _chart.RemoveObject(iconId);
            }
        }
    }
}