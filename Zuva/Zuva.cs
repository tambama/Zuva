using System;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators.Zuva;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Zuva : Indicator
    {
        [Parameter("Use Higher Timeframe", DefaultValue = false)]
        public bool UseHigherTimeframe { get; set; }

        [Parameter("Higher Timeframe", DefaultValue = "H4")]
        public string HigherTimeframeStr { get; set; }
        
        [Parameter("Show Current Timeframe Swings", DefaultValue = true)]
        public bool ShowCurrentTimeframeSwings { get; set; }

        [Output("Current TF Swing High", Color = Colors.Green, PlotType = PlotType.Points, Thickness = 4)]
        public IndicatorDataSeries CurrentTFSwingHighs { get; set; }

        [Output("Current TF Swing Low", Color = Colors.Red, PlotType = PlotType.Points, Thickness = 4)]
        public IndicatorDataSeries CurrentTFSwingLows { get; set; }

        [Output("Higher TF Swing High", Color = Colors.Lime, PlotType = PlotType.Points, Thickness = 6)]
        public IndicatorDataSeries HigherTFSwingHighs { get; set; }

        [Output("Higher TF Swing Low", Color = Colors.Crimson, PlotType = PlotType.Points, Thickness = 6)]
        public IndicatorDataSeries HigherTFSwingLows { get; set; }

        private Bars _currentTimeframeBars;
        private Bars _higherTimeframeBars;
        private TimeFrame _higherTimeframe;
        
        private SwingPointDetector _currentTFSwingDetector;
        private HigherTimeframeProcessor _higherTFProcessor;

        protected override void Initialize()
        {
            _currentTimeframeBars = Bars;
            
            // Initialize the current timeframe swing detector
            _currentTFSwingDetector = new SwingPointDetector(
                _currentTimeframeBars,
                CurrentTFSwingHighs,
                CurrentTFSwingLows);

            if (UseHigherTimeframe)
            {
                // Convert the string parameter to a TimeFrame
                _higherTimeframe = TimeframeHelper.GetTimeFrameFromString(HigherTimeframeStr);
                
                // Use the higher timeframe selected by the user
                _higherTimeframeBars = MarketData.GetBars(_higherTimeframe);
                
                // Initialize the higher timeframe processor
                _higherTFProcessor = new HigherTimeframeProcessor(
                    _currentTimeframeBars,
                    _higherTimeframeBars,
                    HigherTFSwingHighs,
                    HigherTFSwingLows);
            }
        }

        public override void Calculate(int index)
        {
            // Need at least 1 bar to calculate
            if (index <= 0)
                return;

            // Calculate current timeframe swing points
            if (ShowCurrentTimeframeSwings)
            {
                _currentTFSwingDetector.ProcessBar(index);
            }

            // Calculate higher timeframe swing points if enabled
            if (UseHigherTimeframe && _higherTimeframeBars != null)
            {
                _higherTFProcessor.ProcessBar(index);
            }
        }
    }
}