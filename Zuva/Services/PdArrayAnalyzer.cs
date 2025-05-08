using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using Mwenje.Extensions;
using Zuva.Models;

namespace Zuva.Services
{
    /// <summary>
    /// Analyzes price action to identify and track order flow between swing points
    /// </summary>
    public class PdArrayAnalyzer
    {
        // Collection to store all order flow levels
        private readonly List<Level> _pdArrays = new List<Level>();
        
        // Store history of swing points to identify patterns
        private readonly List<SwingPoint> _swingPointHistory = new List<SwingPoint>();
        
        // Chart reference for visualization
        private readonly Chart _chart;
        
        // Flag to control orderflow visualization
        private readonly bool _showOrderFlow;
        
        /// <summary>
        /// Creates a new instance of the PD Array Analyzer
        /// </summary>
        public PdArrayAnalyzer(Chart chart, bool showOrderFlow = false)
        {
            _chart = chart;
            _showOrderFlow = showOrderFlow;
        }
        
        /// <summary>
        /// Process a new swing point to update order flow tracking
        /// </summary>
        public void ProcessSwingPoint(SwingPoint swingPoint)
        {
            if (swingPoint == null)
                return;
                
            // Add the new swing point to our history
            _swingPointHistory.Add(swingPoint);
            
            // Sort the history by index to ensure chronological order
            _swingPointHistory.Sort((a, b) => a.Index.CompareTo(b.Index));
            
            if (swingPoint.Direction == Direction.Down)
            {
                // Process a new swing low - calculate bullish orderflow
                ProcessNewSwingLow(swingPoint);
            }
            else if (swingPoint.Direction == Direction.Up)
            {
                // Process a new swing high - calculate bearish orderflow
                ProcessNewSwingHigh(swingPoint);
            }
        }
        
        /// <summary>
        /// Process a new swing low to calculate bullish orderflow
        /// </summary>
        private void ProcessNewSwingLow(SwingPoint newSwingLow)
        {
            // To calculate bullish orderflow when a new low is created, we need:
            // 1. The previous swing low (before the most recent swing high)
            // 2. The most recent swing high
            
            // Get chronologically ordered swing highs and lows
            var swingHighs = _swingPointHistory.Where(p => p.Direction == Direction.Up)
                .OrderByDescending(p => p.Index)
                .ToList();
                
            var swingLows = _swingPointHistory.Where(p => p.Direction == Direction.Down)
                .OrderByDescending(p => p.Index)
                .ToList();
            
            // We need at least one swing high and two swing lows (including the new one)
            if (swingHighs.Count < 1 || swingLows.Count < 2)
                return;
            
            // The most recent swing high
            var recentSwingHigh = swingHighs.First();
            
            // The previous swing low (not the current one we're processing)
            var previousSwingLow = swingLows.Count > 1 ? swingLows[1] : null;
            
            // Make sure the previous swing low came before the recent swing high
            if (previousSwingLow != null && previousSwingLow.Index < recentSwingHigh.Index)
            {
                // Create a bullish orderflow level from the previous swing low to the recent swing high
                var bullishOrderFlow = new Level(
                    LevelType.Orderflow,
                    previousSwingLow.Price,
                    recentSwingHigh.Price,
                    previousSwingLow.Time,
                    recentSwingHigh.Time,
                    null,
                    Direction.Up,
                    previousSwingLow.Index,  // Index is the swing low index for bullish orderflow
                    recentSwingHigh.Index,   // IndexHigh is the recent swing high index
                    previousSwingLow.Index   // IndexLow is the previous swing low index
                );
                
                // Add to collection
                _pdArrays.Add(bullishOrderFlow);
                
                // Draw the orderflow rectangle if visualization is enabled
                if (_showOrderFlow)
                {
                    DrawOrderFlow(bullishOrderFlow);
                }
            }
        }
        
        /// <summary>
        /// Process a new swing high to calculate bearish orderflow
        /// </summary>
        private void ProcessNewSwingHigh(SwingPoint newSwingHigh)
        {
            // To calculate bearish orderflow when a new high is created, we need:
            // 1. The previous swing high (before the most recent swing low)
            // 2. The most recent swing low
            
            // Get chronologically ordered swing highs and lows
            var swingHighs = _swingPointHistory.Where(p => p.Direction == Direction.Up)
                .OrderByDescending(p => p.Index)
                .ToList();
                
            var swingLows = _swingPointHistory.Where(p => p.Direction == Direction.Down)
                .OrderByDescending(p => p.Index)
                .ToList();
            
            // We need at least two swing highs (including the new one) and one swing low
            if (swingHighs.Count < 2 || swingLows.Count < 1)
                return;
            
            // The most recent swing low
            var recentSwingLow = swingLows.First();
            
            // The previous swing high (not the current one we're processing)
            var previousSwingHigh = swingHighs.Count > 1 ? swingHighs[1] : null;
            
            // Make sure the previous swing high came before the recent swing low
            if (previousSwingHigh != null && previousSwingHigh.Index < recentSwingLow.Index)
            {
                // Create a bearish orderflow level from the previous swing high to the recent swing low
                var bearishOrderFlow = new Level(
                    LevelType.Orderflow,
                    recentSwingLow.Price,
                    previousSwingHigh.Price,
                    recentSwingLow.Time,
                    previousSwingHigh.Time,
                    null,
                    Direction.Down,
                    previousSwingHigh.Index,  // Index is the swing high index for bearish orderflow
                    previousSwingHigh.Index,  // IndexHigh is the previous swing high index
                    recentSwingLow.Index      // IndexLow is the recent swing low index
                );
                
                // Add to collection
                _pdArrays.Add(bearishOrderFlow);
                
                // Draw the orderflow rectangle if visualization is enabled
                if (_showOrderFlow)
                {
                    DrawOrderFlow(bearishOrderFlow);
                }
            }
        }
        
        /// <summary>
        /// Draw an order flow rectangle on the chart
        /// </summary>
        private void DrawOrderFlow(Level level)
        {
            if (_chart == null)
                return;
                
            // Create a unique ID for this order flow
            string id = $"of-{level.Direction}-{level.Index}-{level.IndexHigh}-{level.IndexLow}";
            
            // Draw rectangle with the appropriate color based on direction
            _chart.DrawOrderFlowRectangle(level, id);
        }
        
        /// <summary>
        /// Gets all order flow levels
        /// </summary>
        public List<Level> GetPdArrays()
        {
            return _pdArrays;
        }
        
        /// <summary>
        /// Gets all bullish order flow levels
        /// </summary>
        public List<Level> GetBullishPdArrays()
        {
            return _pdArrays.Where(l => l.Direction == Direction.Up).ToList();
        }
        
        /// <summary>
        /// Gets all bearish order flow levels
        /// </summary>
        public List<Level> GetBearishPdArrays()
        {
            return _pdArrays.Where(l => l.Direction == Direction.Down).ToList();
        }
        
        /// <summary>
        /// Gets the most recent bullish order flow level
        /// </summary>
        public Level GetLastBullishPdArray()
        {
            return _pdArrays.Where(l => l.Direction == Direction.Up)
                .OrderByDescending(l => l.Index)
                .FirstOrDefault();
        }
        
        /// <summary>
        /// Gets the most recent bearish order flow level
        /// </summary>
        public Level GetLastBearishPdArray()
        {
            return _pdArrays.Where(l => l.Direction == Direction.Down)
                .OrderByDescending(l => l.Index)
                .FirstOrDefault();
        }
        
        /// <summary>
        /// Initialize with existing swing points
        /// </summary>
        public void Initialize(List<SwingPoint> swingPoints)
        {
            if (swingPoints == null || swingPoints.Count < 3) // Need at least 3 points to form an orderflow
                return;
                
            // Clear existing history
            _swingPointHistory.Clear();
            
            // Add all swing points to our history
            _swingPointHistory.AddRange(swingPoints);
            
            // Sort by index to ensure chronological order
            _swingPointHistory.Sort((a, b) => a.Index.CompareTo(b.Index));
            
            // Process each swing point in sequence
            for (int i = 2; i < _swingPointHistory.Count; i++)
            {
                var currentPoint = _swingPointHistory[i];
                
                if (currentPoint.Direction == Direction.Down)
                {
                    ProcessNewSwingLow(currentPoint);
                }
                else if (currentPoint.Direction == Direction.Up)
                {
                    ProcessNewSwingHigh(currentPoint);
                }
            }
        }
        
        /// <summary>
        /// Updates the visibility of all order flow rectangles
        /// </summary>
        public void UpdateOrderFlowVisibility(bool showOrderFlow)
        {
            if (_chart == null)
                return;
                
            if (showOrderFlow)
            {
                // Draw all existing order flows
                foreach (var level in _pdArrays)
                {
                    DrawOrderFlow(level);
                }
            }
            else
            {
                // Remove all order flow rectangles
                foreach (var level in _pdArrays)
                {
                    string id = $"of-{level.Direction}-{level.Index}-{level.IndexHigh}-{level.IndexLow}";
                    _chart.RemoveObject(id);
                    
                    // Also remove the midline
                    string midLineId = $"{id}-midline";
                    _chart.RemoveObject(midLineId);
                }
            }
        }
    }
}