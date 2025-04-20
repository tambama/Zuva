using cAlgo.API;
using Zuva.Services;
using Zuva.Models;
using System.Collections.Generic;
using Zuva.Extensions;
using System;

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

        [Output("Swing High", Color = Colors.White, PlotType = PlotType.Points, Thickness = 4)]
        public IndicatorDataSeries SwingHighs { get; set; }

        [Output("Swing Low", Color = Colors.White, PlotType = PlotType.Points, Thickness = 4)]
        public IndicatorDataSeries SwingLows { get; set; }
        
        [Output("HTF Swing High", Color = Colors.Green, PlotType = PlotType.Points, Thickness = 8)]
        public IndicatorDataSeries HtfSwingHighs { get; set; }
        
        [Output("HTF Swing Low", Color = Colors.Red, PlotType = PlotType.Points, Thickness = 8)]
        public IndicatorDataSeries HtfSwingLows { get; set; }

        private SwingPointDetector _swingDetector;
        private List<SwingPoint> _swingPoints;
        private SwingPointDetector _htfSwingDetector;
        private List<SwingPoint> _htfSwingPoints;
        
        private TimeFrame _highTimeFrame;
        
        // Keep track of processed HTF bars to avoid duplicate processing
        private readonly Dictionary<DateTime, bool> _processedHtfBars = new Dictionary<DateTime, bool>();
        
        private Bar _currentBar;
        private int _currentBarIndex;

        protected override void Initialize()
        {
            // Initialize the swing detector
            _swingPoints = new List<SwingPoint>();
            _htfSwingPoints = new List<SwingPoint>();
            
            _swingDetector = new SwingPointDetector(SwingHighs, SwingLows);
            _htfSwingDetector = new SwingPointDetector(HtfSwingHighs, HtfSwingLows);

            _highTimeFrame = HTF.GetTimeFrameFromString();
        }

        public override void Calculate(int index)
        {
            // Need at least 1 bar to calculate
            if (index <= 1)
                return;

            _currentBar = Bars[index - 1];
            _currentBarIndex = index - 1;

            if (ShowSwingPoints)
            {
                // Pass the current bar properties to the regular swing detector
                _swingDetector.ProcessBar(
                    index - 1,
                    new Candle(_currentBar, index - 1)
                );
                
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
    }
}