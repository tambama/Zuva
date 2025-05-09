using cAlgo.API;
using Mwenje.Extensions;
using Zuva.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Zuva.Services
{
    /// <summary>
    /// Detects and tracks Fair Value Gaps (FVGs) in price action
    /// </summary>
    public class FvgDetector
    {
        private readonly Chart _chart;
        private readonly bool _showFVG;
        private readonly List<Level> _fvgs = new List<Level>();
        
        public FvgDetector(Chart chart, bool showFVG)
        {
            _chart = chart;
            _showFVG = showFVG;
        }
        
        /// <summary>
        /// Detects Fair Value Gaps (FVGs) in a series of bars
        /// </summary>
        public void DetectFVG(Bars bars, int currentIndex)
        {
            // Need at least 3 bars to detect a FVG
            if (currentIndex < 2)
                return;
                
            // Get the three consecutive bars
            var bar1 = bars[currentIndex - 2];
            var bar2 = bars[currentIndex - 1];
            var bar3 = bars[currentIndex];
            
            // Check for bullish FVG (bar1's high is lower than bar3's low)
            if (bar1.High < bar3.Low)
            {
                // Check for volume imbalance between candle1 and candle2
                bool hasVolumeImbalance1 = bar1.Close < bar2.Open;
                
                // Determine low boundary based on volume imbalance
                double low = hasVolumeImbalance1 ? bar1.Close : bar1.High;
                
                // Check for volume imbalance between candle2 and candle3
                bool hasVolumeImbalance2 = bar2.Close < bar3.Open;
                
                // Determine high boundary based on volume imbalance
                double high = hasVolumeImbalance2 ? bar3.Open : bar3.Low;
                
                // Create a bullish FVG level
                var bullishFVG = new Level(
                    LevelType.FairValueGap,
                    low,
                    high,
                    bar1.OpenTime,
                    bar3.OpenTime,
                    bar2.OpenTime,
                    Direction.Up,
                    currentIndex - 2,
                    currentIndex,
                    currentIndex - 2,
                    Zone.Premium  // FVGs in an uptrend are typically in the Premium zone
                );
                
                // Add to collection - always store FVGs regardless of visibility setting
                _fvgs.Add(bullishFVG);
                
                // Draw the FVG if visualization is enabled
                if (_showFVG)
                {
                    DrawFVG(bullishFVG);
                }
            }
            
            // Check for bearish FVG (bar1's low is higher than bar3's high)
            else if (bar1.Low > bar3.High)
            {
                // Check for volume imbalance between candle1 and candle2
                bool hasVolumeImbalance1 = bar1.Close > bar2.Open;
                
                // Determine high boundary based on volume imbalance
                double high = hasVolumeImbalance1 ? bar1.Close : bar1.Low;
                
                // Check for volume imbalance between candle2 and candle3
                bool hasVolumeImbalance2 = bar2.Close > bar3.Open;
                
                // Determine low boundary based on volume imbalance
                double low = hasVolumeImbalance2 ? bar3.Open : bar3.High;
                
                // Create a bearish FVG level
                var bearishFVG = new Level(
                    LevelType.FairValueGap,
                    low,
                    high,
                    bar3.OpenTime,
                    bar1.OpenTime,
                    bar2.OpenTime,
                    Direction.Down,
                    currentIndex - 2,
                    currentIndex - 2,
                    currentIndex,
                    Zone.Discount  // FVGs in a downtrend are typically in the Discount zone
                );
                
                // Add to collection - always store FVGs regardless of visibility setting
                _fvgs.Add(bearishFVG);
                
                // Draw the FVG if visualization is enabled
                if (_showFVG)
                {
                    DrawFVG(bearishFVG);
                }
            }
        }
        
        /// <summary>
        /// Draws a Fair Value Gap on the chart
        /// </summary>
        private void DrawFVG(Level fvg)
        {
            // Create a unique ID for this FVG
            string id = $"fvg-{fvg.Direction}-{fvg.Index}-{fvg.IndexHigh}-{fvg.IndexLow}";
            
            // Use the extended chart extension method for better FVG visualization
            _chart.DrawFairValueGap(fvg, id);
        }
        
        /// <summary>
        /// Checks if a level is in a Fair Value Gap
        /// </summary>
        public bool IsInFVG(double price, DateTime time)
        {
            return _fvgs.Any(fvg => price >= fvg.Low && price <= fvg.High && time >= fvg.LowTime && time <= fvg.HighTime.AddMinutes(5));
        }
        
        /// <summary>
        /// Get all FVGs
        /// </summary>
        public List<Level> GetAllFVGs()
        {
            return _fvgs;
        }
        
        /// <summary>
        /// Get bullish FVGs
        /// </summary>
        public List<Level> GetBullishFVGs()
        {
            return _fvgs.Where(f => f.Direction == Direction.Up).ToList();
        }
        
        /// <summary>
        /// Get bearish FVGs
        /// </summary>
        public List<Level> GetBearishFVGs()
        {
            return _fvgs.Where(f => f.Direction == Direction.Down).ToList();
        }
        
        /// <summary>
        /// Updates the visibility of all FVGs based on the showFVG parameter
        /// </summary>
        public void UpdateFVGVisibility(bool showFVG)
        {
            if (_chart == null)
                return;
                
            if (showFVG)
            {
                // Draw all existing FVGs
                foreach (var fvg in _fvgs)
                {
                    DrawFVG(fvg);
                }
            }
            else
            {
                // Remove all FVG visualization elements
                foreach (var fvg in _fvgs)
                {
                    string id = $"fvg-{fvg.Direction}-{fvg.Index}-{fvg.IndexHigh}-{fvg.IndexLow}";
                    _chart.RemoveObject(id); // Remove the rectangle
                    _chart.RemoveObject($"{id}-low"); // Remove the low line
                    _chart.RemoveObject($"{id}-mid"); // Remove the mid line
                    _chart.RemoveObject($"{id}-high"); // Remove the high line
                    _chart.RemoveObject($"{id}-label"); // Remove the label
                }
            }
        }
    }
}