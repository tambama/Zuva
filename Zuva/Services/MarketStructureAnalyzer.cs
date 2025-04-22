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
        
        // Lists for ordered swing points 
        private List<SwingPoint> _orderedHighs = new List<SwingPoint>();
        private List<SwingPoint> _orderedLows = new List<SwingPoint>();
        
        // Collection for external liquidity points
        private List<SwingPoint> _externalLiquidity = new List<SwingPoint>();
        
        // Reference to all swing points
        private List<SwingPoint> _swingPoints = new List<SwingPoint>();

        public MarketStructureAnalyzer(
            Chart chart,
            IndicatorDataSeries highs,
            IndicatorDataSeries lows,
            IndicatorDataSeries hhs,
            IndicatorDataSeries lhs,
            IndicatorDataSeries lls,
            IndicatorDataSeries hls)
        {
            _chart = chart;
            _highs = highs;
            _lows = lows;
            _hhs = hhs;
            _lhs = lhs;
            _lls = lls;
            _hls = hls;
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
            
            // Set initial BOS points
            if (_orderedHighs.Count > 0)
                _highBOS = _orderedHighs[0];
                
            if (_orderedLows.Count > 0)
                _lowBOS = _orderedLows[0];
                
            // Determine initial bias based on the relative positions of the latest highs and lows
            DetermineInitialBias();
        }

        /// <summary>
        /// Determines the initial market bias based on the latest swing points
        /// </summary>
        private void DetermineInitialBias()
        {
            if (_orderedHighs.Count < 2 || _orderedLows.Count < 2)
                return;
                
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
            }
            // Downtrend: Lower Highs and Lower Lows
            else if (!higherHigh && !higherLow)
            {
                _bias = Direction.Down;
            }
            // Mixed signals - use the latest swing point direction
            else
            {
                var latestPoint = _swingPoints.OrderByDescending(s => s.Index).FirstOrDefault();
                if (latestPoint != null)
                {
                    _bias = latestPoint.SwingType == SwingType.H ? Direction.Up : Direction.Down;
                }
            }
            
            UpdateBiasOnChart();
        }

        /// <summary>
        /// Processes a new swing point to update market structure analysis
        /// </summary>
        public void ProcessSwingPoint(SwingPoint swingPoint)
        {
            if (swingPoint == null)
                return;
                
            // Add to the appropriate ordered list
            if (swingPoint.SwingType == SwingType.H)
            {
                _orderedHighs.Insert(0, swingPoint);
                
                // Identify if this is a higher high or lower high
                if (_orderedHighs.Count > 1)
                {
                    var prevHigh = _orderedHighs[1];
                    if (swingPoint.Price > prevHigh.Price)
                    {
                        // Higher High
                        _hhs[swingPoint.Index] = swingPoint.Price;
                        swingPoint.SwingType = SwingType.HH;
                    }
                    else
                    {
                        // Lower High
                        _lhs[swingPoint.Index] = swingPoint.Price;
                        swingPoint.SwingType = SwingType.LH;
                    }
                }
            }
            else if (swingPoint.SwingType == SwingType.L)
            {
                _orderedLows.Insert(0, swingPoint);
                
                // Identify if this is a higher low or lower low
                if (_orderedLows.Count > 1)
                {
                    var prevLow = _orderedLows[1];
                    if (swingPoint.Price > prevLow.Price)
                    {
                        // Higher Low
                        _hls[swingPoint.Index] = swingPoint.Price;
                        swingPoint.SwingType = SwingType.HL;
                    }
                    else
                    {
                        // Lower Low
                        _lls[swingPoint.Index] = swingPoint.Price;
                        swingPoint.SwingType = SwingType.LL;
                    }
                }
            }
            
            // Add to swing points list if it doesn't exist
            if (!_swingPoints.Any(s => s.Index == swingPoint.Index))
            {
                _swingPoints.Add(swingPoint);
            }
            
            // Check for Break of Structure and Change of Character
            CheckForBOS(swingPoint);
            
            // Update the chart with the current bias
            UpdateBiasOnChart();
        }

        /// <summary>
        /// Checks for Break of Structure (BOS) and Change of Character (CHoCH) conditions
        /// </summary>
        private void CheckForBOS(SwingPoint swingPoint)
        {
            if (swingPoint.SwingType == SwingType.H || swingPoint.SwingType == SwingType.HH)
            {
                ProcessHighSwingPoint(swingPoint);
            }
            else
            {
                ProcessLowSwingPoint(swingPoint);
            }
        }

        /// <summary>
        /// Processes a high swing point to detect market structure changes
        /// </summary>
        private void ProcessHighSwingPoint(SwingPoint swingPoint)
        {
            // Break of Structure - Taking out a previous high
            if (_highBOS != null && swingPoint.Price > _highBOS.Price)
            {
                _highBOS = swingPoint;
                _lowCHOCH = _lowBOS; // Mark potential CHoCH point
                
                // Mark previous low point as significant
                if (_lowBOS != null)
                {
                    _lows[_lowBOS.Index] = _lowBOS.Price;
                    
                    var low = _orderedLows.FirstOrDefault(p => p.Index == _lowBOS.Index);
                    if (low != null)
                    {
                        low.SwingType = SwingType.LL;
                        
                        // Add to external liquidity if not already there
                        if (!_externalLiquidity.Any(l => l.Index == _lowBOS.Index))
                        {
                            _externalLiquidity.Add(low);
                        }
                    }
                }

                // Set inducement in an uptrend
                if (_bias == Direction.Up && _orderedLows.Count > 0)
                {
                    _highIND = _orderedLows[0];
                }
                
                // Mark the previous BOS point as swept
                var point = _swingPoints.FirstOrDefault(s => s.Index == _highBOS.Index);
                if (point != null)
                {
                    point.Swept = true;
                }
                
                // Draw BOS line on chart
                if (_highBOS != null && _lowBOS != null)
                {
                    _chart.DrawTrendLine($"BOS-{swingPoint.Time.Ticks}", _highBOS, _lowBOS, LineType.BOS);
                }
            }
            
            // Inducement taken out in a downtrend
            if (_bias == Direction.Down && _lowIND != null && swingPoint.Bar.Close > _lowIND.Price)
            {
                var point = _swingPoints.FirstOrDefault(s => s.Index == _lowBOS.Index);
                if (point != null)
                {
                    point.SwingType = SwingType.LL;
                    _lows[point.Index] = point.Price;
                    
                    if (!_externalLiquidity.Any(l => l.Index == point.Index))
                    {
                        _externalLiquidity.Add(point);
                    }
                }
                
                _highIND = null;
                _highBOS = swingPoint;
                _lowIND = null;
                
                // Draw inducement line
                _chart.DrawStraightLine(
                    $"IND-{swingPoint.Time.Ticks}",
                    _lowIND.Time, 
                    _lowIND.Price,
                    swingPoint.Time,
                    _lowIND.Price,
                    "IND",
                    LineStyle.Dots,
                    null,
                    true,
                    true,
                    false
                );
            }

            // Change of Character - Taking out a CHoCH point
            if (_highCHOCH != null && swingPoint.Price > _highCHOCH.Price)
            {
                var point = _swingPoints.FirstOrDefault(s => s.Index == _highCHOCH.Index);
                if (point != null)
                {
                    point.Swept = true;
                }
                
                _highBOS = swingPoint;
                
                if (_orderedLows.Count > 0)
                {
                    _highIND = _orderedLows[0];
                }
                
                _highCHOCH = null;
                _bias = Direction.Up;
                
                if (_lowCHOCH != null)
                {
                    _lows[_lowCHOCH.Index] = _lowCHOCH.Price;
                    
                    var low = _swingPoints.FirstOrDefault(s => s.Index == _lowCHOCH.Index);
                    if (low != null)
                    {
                        low.SwingType = SwingType.LL;
                        
                        if (!_externalLiquidity.Any(l => l.Index == _lowCHOCH.Index) && low != null)
                        {
                            _externalLiquidity.Add(low);
                        }
                    }
                }
                
                // Draw CHoCH line
                _chart.DrawStraightLine(
                    $"CHOCH-{swingPoint.Time.Ticks}",
                    _lowCHOCH.Time,
                    _lowCHOCH.Price,
                    swingPoint.Time,
                    swingPoint.Price,
                    "CHoCH",
                    LineStyle.Solid,
                    Color.Red,
                    true,
                    true,
                    false
                );
            }
        }

        /// <summary>
        /// Processes a low swing point to detect market structure changes
        /// </summary>
        private void ProcessLowSwingPoint(SwingPoint swingPoint)
        {
            // Break of Structure - Taking out a previous low
            if (_lowBOS != null && swingPoint.Price < _lowBOS.Price)
            {
                _lowBOS = swingPoint;
                _highCHOCH = _highBOS; // Mark potential CHoCH point
                
                // Mark previous high point as significant
                if (_highBOS != null)
                {
                    _highs[_highBOS.Index] = _highBOS.Price;
                    
                    var high = _orderedHighs.FirstOrDefault(p => p.Index == _highBOS.Index);
                    if (high != null)
                    {
                        high.SwingType = SwingType.HH;
                        
                        // Add to external liquidity if not already there
                        if (!_externalLiquidity.Any(l => l.Index == _highBOS.Index))
                        {
                            _externalLiquidity.Add(high);
                        }
                    }
                }

                // Set inducement in a downtrend
                if (_bias == Direction.Down && _orderedHighs.Count > 0)
                {
                    _lowIND = _orderedHighs[0];
                }
                
                // Mark the previous BOS point as swept
                var point = _swingPoints.FirstOrDefault(s => s.Index == _lowBOS.Index);
                if (point != null)
                {
                    point.Swept = true;
                }
                
                // Draw BOS line on chart
                if (_highBOS != null && _lowBOS != null)
                {
                    _chart.DrawTrendLine($"BOS-{swingPoint.Time.Ticks}", _highBOS, _lowBOS, LineType.BOS);
                }
            }
            
            // Inducement taken out in an uptrend
            if (_bias == Direction.Up && _highIND != null && swingPoint.Bar.Close < _highIND.Price)
            {
                var point = _swingPoints.FirstOrDefault(s => s.Index == _highBOS.Index);
                if (point != null)
                {
                    point.SwingType = SwingType.HH;
                    _highs[point.Index] = point.Price;
                    
                    if (!_externalLiquidity.Any(l => l.Index == point.Index))
                    {
                        _externalLiquidity.Add(point);
                    }
                }
                
                _lowIND = null;
                _lowBOS = swingPoint;
                _highIND = null;
                
                // Draw inducement line
                _chart.DrawStraightLine(
                    $"IND-{swingPoint.Time.Ticks}",
                    _highIND.Time, 
                    _highIND.Price,
                    swingPoint.Time,
                    _highIND.Price,
                    "IND",
                    LineStyle.Dots,
                    null,
                    true,
                    true,
                    false
                );
            }
            
            // Change of Character - Taking out a CHoCH point
            if (_lowCHOCH != null && swingPoint.Price < _lowCHOCH.Price)
            {
                var point = _swingPoints.FirstOrDefault(s => s.Index == _lowCHOCH.Index);
                if (point != null)
                {
                    point.Swept = true;
                }
                
                _lowBOS = swingPoint;
                
                if (_orderedHighs.Count > 0)
                {
                    _lowIND = _orderedHighs[0];
                }
                
                _lowCHOCH = null;
                _bias = Direction.Down;
                
                if (_highBOS != null)
                {
                    _highs[_highBOS.Index] = _highBOS.Price;
                    
                    var high = _swingPoints.FirstOrDefault(s => s.Index == _highBOS.Index);
                    if (high != null)
                    {
                        high.SwingType = SwingType.HH;
                        
                        if (!_externalLiquidity.Any(l => l.Index == _highBOS.Index) && high != null)
                        {
                            _externalLiquidity.Add(high);
                        }
                    }
                }
                
                // Draw CHoCH line
                _chart.DrawStraightLine(
                    $"CHOCH-{swingPoint.Time.Ticks}",
                    _highCHOCH.Time,
                    _highCHOCH.Price,
                    swingPoint.Time,
                    swingPoint.Price,
                    "CHoCH",
                    LineStyle.Solid,
                    Color.Red,
                    true,
                    true,
                    false
                );
            }
        }

        /// <summary>
        /// Updates the chart with the current market bias
        /// </summary>
        private void UpdateBiasOnChart()
        {
            string biasText = _bias == Direction.Up ? "Bullish" : "Bearish";
            Color biasColor = _bias == Direction.Up ? Color.Green : Color.Red;
            
            _chart.DrawText("CurrentBias", $"Bias: {biasText}", _chart.Bars.OpenTimes[0], _chart.Bars.HighPrices[0] + 0.0005, biasColor);
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
    }
}