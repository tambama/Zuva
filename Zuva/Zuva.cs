using cAlgo.API;
using Zuva.Services;
using Zuva.Models;
using System.Collections.Generic;
using Zuva.Extensions;
using System;
using Mwenje.Extensions;

namespace Zuva
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Zuva : Indicator
    {
        [Parameter("Show Swing Points", DefaultValue = true)]
        public bool ShowSwingPoints { get; set; }
        
        [Parameter("Show HTF Swing Points", DefaultValue = false)]
        public bool ShowHtfSwingPoints { get; set; }
        
        [Parameter("HTF", DefaultValue = "H1")]
        public string HTF { get; set; }

        [Parameter("Show Market Structure", DefaultValue = true)]
        public bool ShowMarketStructure { get; set; }

        [Output("Swing High", Color = Colors.White, PlotType = PlotType.Points, Thickness = 1)]
        public IndicatorDataSeries SwingHighs { get; set; }

        [Output("Swing Low", Color = Colors.White, PlotType = PlotType.Points, Thickness = 1)]
        public IndicatorDataSeries SwingLows { get; set; }
        
        [Output("HTF Swing High", Color = Colors.Green, PlotType = PlotType.Points, Thickness = 8)]
        public IndicatorDataSeries HtfSwingHighs { get; set; }
        
        [Output("HTF Swing Low", Color = Colors.Red, PlotType = PlotType.Points, Thickness = 8)]
        public IndicatorDataSeries HtfSwingLows { get; set; }

        // Market Structure Series
        [Output("Higher High", Color = Colors.Green, PlotType = PlotType.Points, Thickness = 4)]
        public IndicatorDataSeries HigherHighs { get; set; }
        
        [Output("Lower High", Color = Colors.Red, PlotType = PlotType.Points, Thickness = 4)]
        public IndicatorDataSeries LowerHighs { get; set; }
        
        [Output("Higher Low", Color = Colors.Green, PlotType = PlotType.Points, Thickness = 4)]
        public IndicatorDataSeries HigherLows { get; set; }
        
        [Output("Lower Low", Color = Colors.Red, PlotType = PlotType.Points, Thickness = 4)]
        public IndicatorDataSeries LowerLows { get; set; }

        private SwingPointDetector _swingDetector;
        private List<SwingPoint> _swingPoints;
        private SwingPointDetector _htfSwingDetector;
        private List<SwingPoint> _htfSwingPoints;
        
        private TimeFrame _highTimeFrame;
        
        // Keep track of processed HTF bars to avoid duplicate processing
        private readonly Dictionary<DateTime, bool> _processedHtfBars = new Dictionary<DateTime, bool>();
        
        private Bar _currentBar;
        private int _currentBarIndex;

        // Market structure analyzer
        private MarketStructureAnalyzer _marketStructureAnalyzer;

        protected override void Initialize()
        {
            // Initialize the swing detector
            _swingPoints = new List<SwingPoint>();
            _htfSwingPoints = new List<SwingPoint>();
            
            _swingDetector = new SwingPointDetector(SwingHighs, SwingLows);
            _htfSwingDetector = new SwingPointDetector(HtfSwingHighs, HtfSwingLows);

            _highTimeFrame = HTF.GetTimeFrameFromString();
            
            // Initialize market structure analyzer if enabled
            if (ShowMarketStructure)
            {
                _marketStructureAnalyzer = new MarketStructureAnalyzer(
                    Chart,
                    SwingHighs,
                    SwingLows,
                    HigherHighs,
                    LowerHighs,
                    LowerLows,
                    HigherLows
                );
            }
        }

        public override void Calculate(int index)
        {
            // Need at least 1 bar to calculate
            if (index <= 1)
                return;

            _currentBar = Bars[index - 1];
            _currentBarIndex = index - 1;

            if (_currentBar.OpenTime.Date != DateTime.Today)
            {
                return;
            }

            if (ShowSwingPoints)
            {
                // Create a new candle object from the current bar
                var candle = new Candle(_currentBar, index - 1);
                
                // Pass the current bar properties to the regular swing detector
                _swingDetector.ProcessBar(index - 1, candle);
                
                // Process market structure if enabled
                if (ShowMarketStructure && _marketStructureAnalyzer != null)
                {
                    // Check if a swing point was identified at this index
                    SwingPoint swingPoint = _swingDetector.GetSwingPointAtIndex(index - 1);
                    
                    if (swingPoint != null)
                    {
                        // If this is the first swing point, initialize the market structure analyzer
                        if (index == 2)
                        {
                            _marketStructureAnalyzer.Initialize(_swingDetector.GetAllSwingPoints());
                        }
                        else
                        {
                            // Process the new swing point for market structure analysis
                            _marketStructureAnalyzer.ProcessSwingPoint(swingPoint);
                        }
                    }
                }
                
                // Update the relationships between swing points
                if (index == Bars.Count - 1) // Only on the last bar for efficiency
                {
                    _swingDetector.UpdateSwingPointRelationships();
                    _swingPoints = _swingDetector.GetAllSwingPoints();
                }
            }
            
            // High Timeframe Processing
            if (ShowHtfSwingPoints && _currentBar.OpenTime.IsStartOfHigherTimeframeBar(_highTimeFrame))
            {
                // Get the previous HTF bar indices
                var (startIndex, endIndex) = Bars.GetPreviousHigherTimeframeBarRange(_currentBarIndex, _highTimeFrame);

                if (startIndex >= 0 && endIndex >= 0)
                {
                    // Create the HTF candle from the range of bars
                    var htfCandle = Bars.GetHigherTimeframeCandle(startIndex, endIndex);
                    
                    // Check if we've already processed this HTF bar
                    if (htfCandle != null && !_processedHtfBars.ContainsKey(htfCandle.Time))
                    {
                        // Process the HTF candle using our specialized HTF swing point detector
                        _htfSwingDetector.ProcessHighTimeframeBar(htfCandle);
                        
                        // Mark this HTF bar as processed
                        _processedHtfBars[htfCandle.Time] = true;
                        
                        // Update HTF swing point relationships at the end of all calculations
                        if (index == Bars.Count - 1)
                        {
                            _htfSwingDetector.UpdateSwingPointRelationships();
                            _htfSwingPoints = _htfSwingDetector.GetAllSwingPoints();
                        }
                        
                        // Optionally draw HTF levels on the chart
                        if (false && htfCandle.TimeOfLow.HasValue && htfCandle.TimeOfHigh.HasValue)
                        {
                            Chart.DrawTrendLine(
                                $"htf-low-{htfCandle.Time}", 
                                htfCandle.TimeOfLow.Value, 
                                htfCandle.Low, 
                                _currentBar.OpenTime.AddHours(1), // Extend a bit for visibility
                                htfCandle.Low, 
                                Color.DarkGoldenrod, 
                                1, 
                                LineStyle.Solid
                            );
                            
                            Chart.DrawTrendLine(
                                $"htf-high-{htfCandle.Time}", 
                                htfCandle.TimeOfHigh.Value, 
                                htfCandle.High, 
                                _currentBar.OpenTime.AddHours(1), // Extend a bit for visibility
                                htfCandle.High, 
                                Color.DarkGoldenrod, 
                                1, 
                                LineStyle.Solid
                            );
                        }
                    }
                }
            }
        }
        
        // Methods to expose swing points to other components
        public List<SwingPoint> GetAllSwingPoints()
        {
            return _swingPoints;
        }
        
        public List<SwingPoint> GetAllHtfSwingPoints()
        {
            return _htfSwingPoints;
        }
        
        public SwingPoint GetLastSwingHigh()
        {
            return _swingDetector.GetLastSwingHigh();
        }
        
        public SwingPoint GetLastSwingLow()
        {
            return _swingDetector.GetLastSwingLow();
        }
        
        public SwingPoint GetLastHtfSwingHigh()
        {
            return _htfSwingDetector.GetLastSwingHigh();
        }
        
        public SwingPoint GetLastHtfSwingLow()
        {
            return _htfSwingDetector.GetLastSwingLow();
        }
        
        // Get market structure information
        public Direction GetMarketBias()
        {
            return ShowMarketStructure ? _marketStructureAnalyzer.GetBias() : Direction.Up;
        }
        
        public List<SwingPoint> GetExternalLiquidityPoints()
        {
            return ShowMarketStructure ? _marketStructureAnalyzer.GetExternalLiquidityPoints() : new List<SwingPoint>();
        }
    }
}