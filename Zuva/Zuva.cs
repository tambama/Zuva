using System;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

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

        private TimeFrame _higherTimeframe;
        private SwingPointDetector _currentTFDetector;
        private SwingPointDetector _higherTFDetector;
        private HigherTimeframeMapper _timeframeMapper;
        private int _lastProcessedIndex = -1;

        protected override void Initialize()
        {
            // Initialize current timeframe swing detector
            _currentTFDetector = new SwingPointDetector(Bars);

            if (UseHigherTimeframe)
            {
                // Convert the string parameter to a TimeFrame
                _higherTimeframe = TimeFrameHelper.GetTimeFrameFromString(HigherTimeframeStr);
                
                // Get higher timeframe bars
                Bars higherTFBars = MarketData.GetBars(_higherTimeframe);
                
                // Initialize higher timeframe swing detector
                _higherTFDetector = new SwingPointDetector(higherTFBars);
                
                // Create mapper between timeframes
                _timeframeMapper = new HigherTimeframeMapper(Bars, higherTFBars);
            }
        }

        private List<SwingPointHistory> _higherTFSwingHighHistory = new List<SwingPointHistory>();
        private List<SwingPointHistory> _higherTFSwingLowHistory = new List<SwingPointHistory>();

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
                
                // Calculate current timeframe swing points
                if (ShowCurrentTimeframeSwings)
                {
                    SwingPoint swingPoint = _currentTFDetector.DetectSwingPoint(index);
                    
                    if (swingPoint.HasSwingHigh)
                    {
                        CurrentTFSwingHighs[index] = swingPoint.SwingHighValue;
                    }
                    
                    if (swingPoint.HasSwingLow)
                    {
                        CurrentTFSwingLows[index] = swingPoint.SwingLowValue;
                    }
                    
                    // Clear previous swing points if needed
                    if (swingPoint.ClearPreviousHigh && swingPoint.PreviousSwingHighIndex >= 0)
                    {
                        CurrentTFSwingHighs[swingPoint.PreviousSwingHighIndex] = double.NaN;
                    }
                    
                    if (swingPoint.ClearPreviousLow && swingPoint.PreviousSwingLowIndex >= 0)
                    {
                        CurrentTFSwingLows[swingPoint.PreviousSwingLowIndex] = double.NaN;
                    }
                }

                // Calculate higher timeframe swing points if enabled
                if (UseHigherTimeframe && _higherTFDetector != null && _timeframeMapper != null)
                {
                    int higherTFIndex = _timeframeMapper.GetHigherTimeframeIndex(index);
                    
                    if (higherTFIndex >= 0)
                    {
                        // Only process higher timeframe bars if they're closed or this is a historical bar
                        bool isLastBarOfHigherTF = _timeframeMapper.IsLastBarOfHigherTimeframe(index);
                        
                        // Process the higher timeframe swing detection if it's a closed higher timeframe bar
                        // or if we're processing historical data
                        if (isLastBarOfHigherTF || !isLastBar)
                        {
                            SwingPoint htfSwingPoint = _higherTFDetector.DetectSwingPoint(higherTFIndex);
                            
                            // Record swing high history
                            if (htfSwingPoint.HasSwingHigh)
                            {
                                var swingHigh = new SwingPointHistory { 
                                    Index = index, 
                                    Value = htfSwingPoint.SwingHighValue, 
                                    HigherTimeframeIndex = higherTFIndex 
                                };
                                
                                // Check if this is replacing a previous swing high
                                if (htfSwingPoint.ClearPreviousHigh && _higherTFSwingHighHistory.Count > 0)
                                {
                                    _higherTFSwingHighHistory.RemoveAt(_higherTFSwingHighHistory.Count - 1);
                                }
                                
                                _higherTFSwingHighHistory.Add(swingHigh);
                            }
                            
                            // Record swing low history
                            if (htfSwingPoint.HasSwingLow)
                            {
                                var swingLow = new SwingPointHistory { 
                                    Index = index, 
                                    Value = htfSwingPoint.SwingLowValue, 
                                    HigherTimeframeIndex = higherTFIndex 
                                };
                                
                                // Check if this is replacing a previous swing low
                                if (htfSwingPoint.ClearPreviousLow && _higherTFSwingLowHistory.Count > 0)
                                {
                                    _higherTFSwingLowHistory.RemoveAt(_higherTFSwingLowHistory.Count - 1);
                                }
                                
                                _higherTFSwingLowHistory.Add(swingLow);
                            }
                        }
                        
                        // Always redraw all the higher timeframe swing points we have recorded
                        // Clear previous drawings
                        if (index > 0)
                        {
                            for (int i = 0; i < index; i++)
                            {
                                HigherTFSwingHighs[i] = double.NaN;
                                HigherTFSwingLows[i] = double.NaN;
                            }
                        }
                        
                        // Draw all swing highs
                        foreach (var swingHigh in _higherTFSwingHighHistory)
                        {
                            HigherTFSwingHighs[swingHigh.Index] = swingHigh.Value;
                        }
                        
                        // Draw all swing lows
                        foreach (var swingLow in _higherTFSwingLowHistory)
                        {
                            HigherTFSwingLows[swingLow.Index] = swingLow.Value;
                        }
                    }
                }
            }
        }
    }
}