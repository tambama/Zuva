using cAlgo.API;
using Zuva.Services;

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

        protected override void Initialize()
        {
            // Initialize the swing detector with just the data series
            _swingDetector = new SwingPointDetector(SwingHighs, SwingLows);
        }

        public override void Calculate(int index)
        {
            // Need at least 1 bar to calculate
            if (index <= 0)
                return;

            // Calculate swing points
            if (ShowSwingPoints)
            {
                // Pass the current bar properties instead of the whole Bars collection
                _swingDetector.ProcessBar(
                    index,
                    Bars.HighPrices[index],
                    Bars.LowPrices[index],
                    Bars.OpenPrices[index],
                    Bars.ClosePrices[index]
                );
            }
        }
    }
}