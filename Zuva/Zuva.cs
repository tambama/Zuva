using cAlgo.API;
using Zuva.Services;
using Zuva.Models;
using System.Collections.Generic;
using Zuva.Helpers;

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

        protected override void Initialize()
        {
            // Initialize the swing detector
            _swingPoints = new List<SwingPoint>();
            _htfSwingPoints = new List<SwingPoint>();
            
            _swingDetector = new SwingPointDetector(SwingHighs, SwingLows);
            _htfSwingDetector = new SwingPointDetector(HtfSwingHighs, HtfSwingLows);

            _highTimeFrame = TimeframeHelper.GetTimeFrameFromString(HTF);
        }

        public override void Calculate(int index)
        {
            // Need at least 1 bar to calculate
            if (index <= 1)
                return;

            _currentBar = Bars[index - 1];

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
            
            // High Timeframe Processing
            if (_currentBar != null && _currentBar.OpenTime.Minute == 00)
            {
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