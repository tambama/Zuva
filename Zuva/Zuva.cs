using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Zuva : Indicator
    {
        [Output("Swing High", Color = Colors.Green, PlotType = PlotType.Points, Thickness = 4)]
        public IndicatorDataSeries SwingHighs { get; set; }

        [Output("Swing Low", Color = Colors.Red, PlotType = PlotType.Points, Thickness = 4)]
        public IndicatorDataSeries SwingLows { get; set; }

        private SwingPointDetector _detector;
        private int _lastProcessedIndex = -1;

        protected override void Initialize()
        {
            // Initialize swing detector
            _detector = new SwingPointDetector(Bars);
        }

        public override void Calculate(int index)
        {
            // Need at least 1 bar to calculate
            if (index <= 0)
                return;
                
            // Check if this is a new bar or still the current bar forming
            bool isNewBar = index > _lastProcessedIndex;
            bool isLastBar = index == Bars.Count - 1;
            
            // Only process closed bars (any bar except the last one) or if this is a historical bar we haven't processed yet
            if (!isLastBar || (isLastBar && isNewBar && index > 1))
            {
                // Update the last processed index
                _lastProcessedIndex = index;
                
                // Calculate swing points
                SwingPoint swingPoint = _detector.DetectSwingPoint(index);
                
                if (swingPoint.HasSwingHigh)
                {
                    SwingHighs[index] = swingPoint.SwingHighValue;
                }
                
                if (swingPoint.HasSwingLow)
                {
                    SwingLows[index] = swingPoint.SwingLowValue;
                }
                
                // Clear previous swing points if needed
                if (swingPoint.ClearPreviousHigh && swingPoint.PreviousSwingHighIndex >= 0)
                {
                    SwingHighs[swingPoint.PreviousSwingHighIndex] = double.NaN;
                }
                
                if (swingPoint.ClearPreviousLow && swingPoint.PreviousSwingLowIndex >= 0)
                {
                    SwingLows[swingPoint.PreviousSwingLowIndex] = double.NaN;
                }
            }
        }
    }
}