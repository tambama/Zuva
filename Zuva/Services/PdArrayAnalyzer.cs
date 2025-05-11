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

        // Flag to control liquidity sweep visualization
        private readonly bool _showLiquiditySweep;

        // Reference to bars for finding specific candles
        private Bars Bars;

        /// <summary>
        /// Creates a new instance of the PD Array Analyzer
        /// </summary>
        public PdArrayAnalyzer(Chart chart, Bars bars, bool showOrderFlow = false, bool showLiquiditySweep = false)
        {
            _chart = chart;
            Bars = bars;
            _showOrderFlow = showOrderFlow;
            _showLiquiditySweep = showLiquiditySweep;
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
                    previousSwingLow.Index, // Index is the swing low index for bullish orderflow
                    recentSwingHigh.Index, // IndexHigh is the recent swing high index
                    previousSwingLow.Index // IndexLow is the previous swing low index
                );

                // Check for swept swing highs
                CheckForSweptSwingHighs(bullishOrderFlow);

                // Add to collection
                _pdArrays.Add(bullishOrderFlow);

                // Draw the orderflow rectangle if visualization is enabled
                if (_showOrderFlow)
                {
                    DrawOrderFlow(bullishOrderFlow);
                }

                // Draw swept liquidity line if applicable - independent of orderflow visibility
                if (bullishOrderFlow.SweptSwingPoint != null)
                {
                    DrawSweptLiquidityLine(bullishOrderFlow);
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
                    previousSwingHigh.Index, // Index is the swing high index for bearish orderflow
                    previousSwingHigh.Index, // IndexHigh is the previous swing high index
                    recentSwingLow.Index // IndexLow is the recent swing low index
                );

                // Check for swept swing lows
                CheckForSweptSwingLows(bearishOrderFlow);

                // Add to collection
                _pdArrays.Add(bearishOrderFlow);

                // Draw the orderflow rectangle if visualization is enabled
                if (_showOrderFlow)
                {
                    DrawOrderFlow(bearishOrderFlow);
                }

                // Draw swept liquidity line if applicable - independent of orderflow visibility
                if (bearishOrderFlow.SweptSwingPoint != null)
                {
                    DrawSweptLiquidityLine(bearishOrderFlow);
                }
            }
        }

        /// <summary>
        /// Checks if the bullish orderflow level swept any swing highs
        /// </summary>
        private void CheckForSweptSwingHighs(Level orderflow)
        {
            // Get all unswept swing highs
            var unsweptSwingHighs = _swingPointHistory
                .Where(p => p.Direction == Direction.Up && !p.Swept)
                .OrderByDescending(p => p.Price)
                .ToList();

            // Check if any swing highs were swept (high of orderflow > swing high AND low of orderflow < swing high)
            var sweptHighs = unsweptSwingHighs
                .Where(h => orderflow.High > h.Price && orderflow.Low < h.Price && h.Index < orderflow.IndexHigh)
                .ToList();

            // Only proceed if we have swept points
            if (sweptHighs.Count > 0)
            {
                // Initialize the swept swing points collection
                orderflow.SweptSwingPoints = new List<SwingPoint>();

                // Find the highest swept swing high (the extreme point)
                var highestSweptPoint = sweptHighs.OrderByDescending(h => h.Price).First();

                // Now find the exact candle that swept this extreme point
                int sweepingCandleIndex = FindSweepingCandleForPoint(orderflow, highestSweptPoint);
                orderflow.IndexOfSweepingCandle = sweepingCandleIndex;

                // Mark all swept points
                foreach (var sweptPoint in sweptHighs)
                {
                    // Mark it as swept
                    sweptPoint.Swept = true;
                    sweptPoint.SweptLiquidity = true;
                    // Use the same sweeping candle index for all (from the extreme point)
                    sweptPoint.IndexOfSweepingCandle = sweepingCandleIndex;

                    // Add to the collection of swept points
                    orderflow.SweptSwingPoints.Add(sweptPoint);
                }

                // Set the extreme point as the primary swept point for visualization
                orderflow.SweptSwingPoint = highestSweptPoint;

                // Add score based on how many sweep points were triggered
                // More points = higher score
                orderflow.Score += Math.Min(3, sweptHighs.Count); // Cap at 3 for scoring
            }
        }

        /// <summary>
        /// Checks if the bearish orderflow level swept any swing lows
        /// </summary>
        private void CheckForSweptSwingLows(Level orderflow)
        {
            // Get all unswept swing lows
            var unsweptSwingLows = _swingPointHistory
                .Where(p => p.Direction == Direction.Down && !p.Swept)
                .OrderBy(p => p.Price)
                .ToList();

            // Check if any swing lows were swept (low of orderflow < swing low AND high of orderflow > swing low)
            var sweptLows = unsweptSwingLows
                .Where(l => orderflow.Low < l.Price && orderflow.High > l.Price && l.Index < orderflow.IndexLow)
                .ToList();

            // Only proceed if we have swept points
            if (sweptLows.Count > 0)
            {
                // Initialize the swept swing points collection
                orderflow.SweptSwingPoints = new List<SwingPoint>();

                // Find the lowest swept swing low (the extreme point)
                var lowestSweptPoint = sweptLows.OrderBy(l => l.Price).First();

                // Now find the exact candle that swept this extreme point
                int sweepingCandleIndex = FindSweepingCandleForPoint(orderflow, lowestSweptPoint);
                orderflow.IndexOfSweepingCandle = sweepingCandleIndex;

                // Mark all swept points
                foreach (var sweptPoint in sweptLows)
                {
                    // Mark it as swept
                    sweptPoint.Swept = true;
                    sweptPoint.SweptLiquidity = true;
                    // Use the same sweeping candle index for all (from the extreme point)
                    sweptPoint.IndexOfSweepingCandle = sweepingCandleIndex;

                    // Add to the collection of swept points
                    orderflow.SweptSwingPoints.Add(sweptPoint);
                }

                // Set the extreme point as the primary swept point for visualization
                orderflow.SweptSwingPoint = lowestSweptPoint;

                // Add score based on how many sweep points were triggered
                // More points = higher score
                orderflow.Score += Math.Min(3, sweptLows.Count); // Cap at 3 for scoring
            }
        }

        /// <summary>
        /// Finds the exact candle that swept a specific swing point
        /// </summary>
        private int FindSweepingCandleForPoint(Level orderflow, SwingPoint sweptPoint)
        {
            // If no point was provided, use the default index
            if (sweptPoint == null)
                return orderflow.Direction == Direction.Up ? orderflow.IndexHigh : orderflow.IndexLow;

            // Get the price of the swept point
            double sweepPrice = sweptPoint.Price;

            // Define search range based on direction of the orderflow
            int startIndex = orderflow.Direction == Direction.Up ? orderflow.IndexLow : orderflow.IndexHigh;
            int endIndex = orderflow.Direction == Direction.Up ? orderflow.IndexHigh : orderflow.IndexLow;

            // Ensure we have valid indices
            if (startIndex < 0 || endIndex < 0 || startIndex >= Bars.Count || endIndex >= Bars.Count)
                return orderflow.Direction == Direction.Up
                    ? orderflow.IndexHigh
                    : orderflow.IndexLow; // Default fallback

            // Search for the candle that swept the price
            for (int i = startIndex; i <= endIndex; i++)
            {
                if (orderflow.Direction == Direction.Up && Bars[i].High > sweepPrice)
                {
                    return i; // This is the candle that swept the high
                }
                else if (orderflow.Direction == Direction.Down && Bars[i].Low < sweepPrice)
                {
                    return i; // This is the candle that swept the low
                }
            }

            // If no specific candle is found, default to the extreme index
            return orderflow.Direction == Direction.Up ? orderflow.IndexHigh : orderflow.IndexLow;
        }

        /// <summary>
        /// Draws a liquidity sweep line on the chart to show when a swing point was swept
        /// </summary>
        private void DrawSweptLiquidityLine(Level orderflow)
        {
            if (_chart == null || orderflow.SweptSwingPoint == null)
                return;

            // Skip drawing if liquidity sweep visualization is disabled
            if (!_showLiquiditySweep)
                return;

            // Get the swept swing point (the extreme one)
            var sweptPoint = orderflow.SweptSwingPoint;

            // Create a unique ID for this liquidity sweep line
            string id = $"swept-{orderflow.Direction}-{orderflow.Index}-{sweptPoint.Index}";

            // Determine the start time (the swept swing point)
            DateTime startTime = sweptPoint.Time;

            // Determine the price (the price of the swept swing point)
            double price = sweptPoint.Price;

            // Get the time of the actual sweeping candle using the stored index
            DateTime endTime;
            if (orderflow.IndexOfSweepingCandle >= 0 && orderflow.IndexOfSweepingCandle < Bars.Count)
            {
                endTime = Bars[orderflow.IndexOfSweepingCandle].OpenTime;
            }
            else
            {
                // Fallback to the order flow's extreme time if index is invalid
                endTime = orderflow.Direction == Direction.Up ? orderflow.HighTime : orderflow.LowTime;
            }

            // Draw the dotted yellow line without label as requested
            _chart.DrawStraightLine(
                id,
                startTime,
                price,
                endTime,
                price,
                null, // No label
                LineStyle.Dots,
                Color.Yellow,
                false, // Don't show label
                true, // Remove existing
                false // Not extended
            );
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
        /// Gets all order flow levels that swept liquidity
        /// </summary>
        public List<Level> GetLiquiditySweepLevels()
        {
            return _pdArrays.Where(l => l.SweptSwingPoint != null).ToList();
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
    }
}