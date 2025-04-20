using cAlgo.API;
using Zuva.Services;
using Zuva.Models;
using System.Collections.Generic;

namespace Zuva
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Zuva : Indicator
    {
        [Parameter("Show Swing Points", DefaultValue = true)]
        public bool ShowSwingPoints { get; set; }

        [Output("Swing High", Color = Colors.Green, PlotType = PlotType.Points, Thickness = 4)]
        public IndicatorDataSeries SwingHighs { get; set; }

        [Output("Swing Low", Color = Colors.Red, PlotType = PlotType.Points, Thickness = 4)]
        public IndicatorDataSeries SwingLows { get; set; }

        private SwingPointDetector _swingDetector;
        private List<SwingPoint> _swingPoints;

        protected override void Initialize()
        {
            // Initialize the swing detector
            _swingDetector = new SwingPointDetector(SwingHighs, SwingLows);
            _swingPoints = new List<SwingPoint>();
        }

        public override void Calculate(int index)
        {
            // Need at least 1 bar to calculate
            if (index <= 0)
                return;

            // Calculate swing points
            if (ShowSwingPoints)
            {
                // Pass the current bar properties
                _swingDetector.ProcessBar(
                    index,
                    Bars[index]
                );
                
                // Update the relationships between swing points
                if (index == Bars.Count - 1) // Only on the last bar for efficiency
                {
                    _swingDetector.UpdateSwingPointRelationships();
                    _swingPoints = _swingDetector.GetAllSwingPoints();
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