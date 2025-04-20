using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Zuva : Indicator
    {
        [Parameter("Show Swing Points", DefaultValue = true)]
        public bool ShowSwingPoints { get; set; }

        [Parameter("Show Fair Value Gaps", DefaultValue = true)]
        public bool ShowFairValueGaps { get; set; }

        [Parameter("Bullish FVG Color", DefaultValue = "DeepSkyBlue")]
        public string BullishFvgColor { get; set; }

        [Parameter("Bearish FVG Color", DefaultValue = "Magenta")]
        public string BearishFvgColor { get; set; }

        [Parameter("FVG Opacity", DefaultValue = 50, MinValue = 0, MaxValue = 100)]
        public int FvgOpacity { get; set; }

        [Output("Swing High", Color = Colors.Green, PlotType = PlotType.Points, Thickness = 4)]
        public IndicatorDataSeries SwingHighs { get; set; }

        [Output("Swing Low", Color = Colors.Red, PlotType = PlotType.Points, Thickness = 4)]
        public IndicatorDataSeries SwingLows { get; set; }

        private SwingPointDetector _swingDetector;
        private PDArrayDetector _pdDetector;
        private int _lastProcessedIndex = -1;
        private List<FvgDrawing> _fvgDrawings;
        
        protected override void Initialize()
        {
            // Initialize swing detector
            _swingDetector = new SwingPointDetector(Bars);
            
            // Initialize PD Array detector
            _pdDetector = new PDArrayDetector(Bars);
            
            // Initialize FVG drawings list
            _fvgDrawings = new List<FvgDrawing>();
            
            // Process historical data
            for (int i = 2; i < Bars.Count - 1; i++)
            {
                Calculate(i);
            }
        }

        public override void Calculate(int index)
        {
            // Need at least 2 bars to calculate
            if (index < 2)
                return;
                
            // Process swing points
            if (ShowSwingPoints)
            {
                SwingPoint swingPoint = _swingDetector.DetectSwingPoint(index);
                
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
            
            // Process Fair Value Gaps
            if (ShowFairValueGaps)
            {
                ProcessFairValueGaps(index);
            }
        }
        
        private void ProcessFairValueGaps(int index)
        {
            // Detect new FVG
            FairValueGap fvg = _pdDetector.DetectFairValueGap(index);
            
            if (fvg != null)
            {
                // Get color based on FVG type (bullish or bearish)
                Color fvgColor = fvg.IsBullish 
                    ? ParseColorWithOpacity(BullishFvgColor) 
                    : ParseColorWithOpacity(BearishFvgColor);
                
                // Create rectangle to visualize the FVG
                string objectName = $"FVG_{fvg.StartIndex}_{index}_{DateTime.Now.Ticks}";
                var rectangle = Chart.DrawRectangle(
                    objectName, 
                    Bars.OpenTimes[fvg.StartIndex], 
                    fvg.UpperBound, 
                    Bars.OpenTimes[index + 10 > Bars.Count - 1 ? Bars.Count - 1 : index + 10], // Extend rectangle
                    fvg.LowerBound, 
                    Color.Red);
                
                // Add to the tracking list
                _fvgDrawings.Add(new FvgDrawing { 
                    FairValueGap = fvg, 
                    Rectangle = rectangle, 
                    Name = objectName 
                });
                
                // Add debugging
                Print($"Created FVG at index {index}: {fvg.IsBullish} Upper:{fvg.UpperBound} Lower:{fvg.LowerBound}");
            }
            
            // Update FVG status
            _pdDetector.UpdatePDArrayStatus(index);
            
            // Remove inactive FVGs
            List<FvgDrawing> toRemove = new List<FvgDrawing>();
            
            foreach (var drawing in _fvgDrawings)
            {
                if (!drawing.FairValueGap.IsActive)
                {
                    Chart.RemoveObject(drawing.Name);
                    toRemove.Add(drawing);
                    Print($"Removed FVG: {drawing.Name}");
                }
            }
            
            foreach (var item in toRemove)
            {
                _fvgDrawings.Remove(item);
            }
        }
        
        private Color ParseColorWithOpacity(string colorName)
        {
            Color baseColor = Color.FromName(colorName);
            int alpha = (int)(255 * (FvgOpacity / 100.0));
            return Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);
        }
        
        // Helper class to track FVG drawings
        private class FvgDrawing
        {
            public FairValueGap FairValueGap { get; set; }
            public ChartRectangle Rectangle { get; set; }
            public string Name { get; set; }
        }
    }
}