using cAlgo.API;
using Zuva.Services;
using Zuva.Models;
using System.Collections.Generic;
using Zuva.Extensions;

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

        [Output("Swing High", Color = Colors.Green, PlotType = PlotType.Points, Thickness = 4)]
        public IndicatorDataSeries SwingHighs { get; set; }

        [Output("Swing Low", Color = Colors.Red, PlotType = PlotType.Points, Thickness = 4)]
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
                // Pass the current bar properties
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
            if (_currentBar.OpenTime.IsStartOfHigherTimeframeBar(_highTimeFrame))
            {
                // Get the previous HTF bar indices
                var (startIndex, endIndex) = Bars.GetPreviousHigherTimeframeBarRange(_currentBarIndex, HTF.GetTimeFrameFromString());

                if (startIndex >= 0 && endIndex >= 0)
                {
                    // Now you can analyze the previous higher timeframe bar
                    var (minTime, minIndex, min, maxTime, maxIndex, max) = Bars.GetMinMax(Bars[startIndex].OpenTime, Bars[endIndex].OpenTime);
                    // Use these values in your trading strategy...
                    
                    Chart.DrawTrendLine($"{minTime}", minTime, min, _currentBar.OpenTime, min, Color.Pink);
                    Chart.DrawTrendLine($"{maxTime}", maxTime, max, _currentBar.OpenTime, max, Color.Pink);
                }
            }
        }
        
        // Methods to expose swing points to other components
        public List<SwingPoint> GetAllSwingPoints()
        {
            return _swingPoints;
        }
        
        public SwingPoint GetLastSwingHigh()
        {
            return _swingDetector.GetLastSwingHigh();
        }
        
        public SwingPoint GetLastSwingLow()
        {
            return _swingDetector.GetLastSwingLow();
        }
    }
}