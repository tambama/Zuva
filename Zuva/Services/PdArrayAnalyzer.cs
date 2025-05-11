using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using Mwenje.Extensions;
using Zuva.Models;
using Zuva.Services;

namespace Zuva.Services
{
    /// <summary>
    /// Analyzes price action to identify and track order flow between swing points
    /// </summary>
    public class PdArrayAnalyzer
    {
        // Add a delegate for logging
        private readonly Action<string> _logger;

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

        // Flag to control gauntlet visualization
        private readonly bool _showGauntlet;

        // Flag to control CISD visualization
        private readonly bool _showCISD;
        private readonly List<Level> _cisdLevels = new List<Level>();

        // Collection to store all gauntlet levels
        private readonly List<Level> _gauntlets = new List<Level>();

        // Reference to FVG detector for finding FVGs
        private readonly FvgDetector _fvgDetector;

        // Reference to bars for finding specific candles
        private Bars Bars;

        /// <summary>
        /// Creates a new instance of the PD Array Analyzer
        /// </summary>
        public PdArrayAnalyzer(
            Chart chart,
            Bars bars,
            bool showOrderFlow = false,
            bool showLiquiditySweep = false,
            bool showGauntlet = false,
            FvgDetector fvgDetector = null,
            bool showCISD = false,
            Action<string> logger = null)
        {
            _chart = chart;
            Bars = bars;
            _showOrderFlow = showOrderFlow;
            _showLiquiditySweep = showLiquiditySweep;
            _showGauntlet = showGauntlet;
            _fvgDetector = fvgDetector;
            _showCISD = showCISD;
            _logger = logger ?? (_ => { });
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

            CheckCISDActivity(swingPoint, swingPoint.Index);
        }

        /// <summary>
        /// Handle the removal of a swing point
        /// </summary>
        public void RemoveSwingPoint(SwingPoint removedPoint)
        {
            if (removedPoint == null)
                return;

            // Remove the swing point from our history
            _swingPointHistory.RemoveAll(p => p.Index == removedPoint.Index &&
                                              p.Direction == removedPoint.Direction &&
                                              Math.Abs(p.Price - removedPoint.Price) < 0.0001);

            // Find any orderflow levels that reference this swing point
            var affectedArrays = new List<Level>();

            foreach (var array in _pdArrays)
            {
                bool isAffected = false;

                // Check if this array uses the removed point as one of its key points
                if (array.Direction == Direction.Up)
                {
                    // For bullish orderflow, check if removed point is the low or high
                    if (array.IndexLow == removedPoint.Index && removedPoint.Direction == Direction.Down)
                        isAffected = true;
                    else if (array.IndexHigh == removedPoint.Index && removedPoint.Direction == Direction.Up)
                        isAffected = true;
                }
                else // Direction.Down
                {
                    // For bearish orderflow, check if removed point is the high or low
                    if (array.IndexHigh == removedPoint.Index && removedPoint.Direction == Direction.Up)
                        isAffected = true;
                    else if (array.IndexLow == removedPoint.Index && removedPoint.Direction == Direction.Down)
                        isAffected = true;
                }

                // Check if this array references the removed point in its swept points
                if (!isAffected && array.SweptSwingPoints != null)
                {
                    foreach (var sweptPoint in array.SweptSwingPoints)
                    {
                        if (sweptPoint.Index == removedPoint.Index &&
                            sweptPoint.Direction == removedPoint.Direction &&
                            Math.Abs(sweptPoint.Price - removedPoint.Price) < 0.0001)
                        {
                            isAffected = true;
                            break;
                        }
                    }
                }

                // If this array is affected, add it to our list
                if (isAffected)
                    affectedArrays.Add(array);
            }

            // Remove affected arrays from our collection
            foreach (var array in affectedArrays)
            {
                _pdArrays.Remove(array);

                // Remove any gauntlets associated with this array
                if (array.GauntletFVG != null)
                {
                    _gauntlets.Remove(array.GauntletFVG);

                    // Remove gauntlet visualization
                    if (_chart != null)
                    {
                        string gauntletId = $"gauntlet-{array.GauntletFVG.Direction}-{array.GauntletFVG.Index}";
                        _chart.RemoveObject(gauntletId);
                    }
                }

                // Clean up visualization
                if (_chart != null)
                {
                    // Remove orderflow visualization
                    string ofId = $"of-{array.Direction}-{array.Index}-{array.IndexHigh}-{array.IndexLow}";
                    _chart.RemoveObject(ofId);

                    // Remove liquidity sweep line if present
                    if (array.SweptSwingPoint != null)
                    {
                        string sweptId = $"swept-{array.Direction}-{array.Index}-{array.SweptSwingPoint.Index}";
                        _chart.RemoveObject(sweptId);
                    }
                }
            }

            // If we removed any arrays, we need to recalculate
            if (affectedArrays.Count > 0)
            {
                // Regenerate orderflows based on the remaining points
                RegenerateOrderFlows();
            }
        }

        /// <summary>
        /// Regenerate all orderflow levels based on the current swing point history
        /// </summary>
        private void RegenerateOrderFlows()
        {
            // Clear existing order flows
            _pdArrays.Clear();

            // Clear existing gauntlets
            _gauntlets.Clear();

            // Remove all orderflow visualization
            if (_chart != null)
            {
                // This is a simplistic approach - in a real implementation we would 
                // need to be more selective about which objects to remove
                for (int i = 0; i < 1000; i++) // Arbitrary limit
                {
                    _chart.RemoveObject($"of-Up-{i}");
                    _chart.RemoveObject($"of-Down-{i}");
                    _chart.RemoveObject($"swept-Up-{i}");
                    _chart.RemoveObject($"swept-Down-{i}");
                    _chart.RemoveObject($"gauntlet-Up-{i}");
                    _chart.RemoveObject($"gauntlet-Down-{i}");
                }
            }

            // Sort the history by index to ensure chronological order
            _swingPointHistory.Sort((a, b) => a.Index.CompareTo(b.Index));

            // Skip if we don't have enough points
            if (_swingPointHistory.Count < 3)
                return;

            // Process each swing point to recreate order flows
            for (int i = 2; i < _swingPointHistory.Count; i++)
            {
                var swingPoint = _swingPointHistory[i];

                if (swingPoint.Direction == Direction.Down)
                {
                    ProcessNewSwingLow(swingPoint);
                }
                else if (swingPoint.Direction == Direction.Up)
                {
                    ProcessNewSwingHigh(swingPoint);
                }
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

                // Draw gauntlet if it exists and visualization is enabled
                if (bullishOrderFlow.GauntletFVG != null && _showGauntlet)
                {
                    DrawGauntlet(bullishOrderFlow.GauntletFVG);
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

                // Draw gauntlet if it exists and visualization is enabled
                if (bearishOrderFlow.GauntletFVG != null && _showGauntlet)
                {
                    DrawGauntlet(bearishOrderFlow.GauntletFVG);
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

                DetectCISDLevel(orderflow);

                // Add score based on how many sweep points were triggered
                // More points = higher score
                orderflow.Score += Math.Min(3, sweptHighs.Count); // Cap at 3 for scoring

                // Check for Gauntlet pattern after finding the sweeping candle
                CheckForGauntlet(orderflow, sweepingCandleIndex);
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

                DetectCISDLevel(orderflow);

                // Add score based on how many sweep points were triggered
                // More points = higher score
                orderflow.Score += Math.Min(3, sweptLows.Count); // Cap at 3 for scoring

                // Check for Gauntlet pattern after finding the sweeping candle
                CheckForGauntlet(orderflow, sweepingCandleIndex);
            }
        }

        /// <summary>
        /// Checks if the sweeping candle is part of an FVG pattern to detect Gauntlets
        /// by first finding the last FVG within the orderflow
        /// </summary>
        private void CheckForGauntlet(Level orderflow, int sweepingCandleIndex)
        {
            // Skip if no FVG detector available or index is invalid
            if (_fvgDetector == null || sweepingCandleIndex < 1 || sweepingCandleIndex >= Bars.Count)
                return;

            // Skip if the orderflow doesn't have swept liquidity
            if (orderflow.SweptSwingPoint == null)
                return;

            // Get all FVGs from the detector
            var allFvgs = _fvgDetector.GetAllFVGs();
            if (allFvgs == null || allFvgs.Count == 0)
                return;

            // First, find the last FVG within the orderflow
            var gauntletFVG = FindLastFVGInOrderflow(orderflow, allFvgs);

            // If we found a matching FVG, check if the sweeping candle is part of it
            if (gauntletFVG != null)
            {
                // Check if the sweeping candle is either the second or third candle of the FVG
                bool isSweepingCandlePartOfFVG = false;

                if (orderflow.Direction == Direction.Up)
                {
                    // For bullish FVGs, check if sweeping candle is either IndexMid or IndexHigh
                    isSweepingCandlePartOfFVG =
                        sweepingCandleIndex == gauntletFVG.IndexMid ||
                        sweepingCandleIndex == gauntletFVG.IndexHigh;
                }
                else // Direction.Down
                {
                    // For bearish FVGs, check if sweeping candle is either IndexMid or IndexLow
                    isSweepingCandlePartOfFVG =
                        sweepingCandleIndex == gauntletFVG.IndexMid ||
                        sweepingCandleIndex == gauntletFVG.IndexLow;
                }

                // If the sweeping candle is part of the FVG, mark it as a Gauntlet
                if (isSweepingCandlePartOfFVG)
                {
                    // Mark the FVG as a Gauntlet
                    gauntletFVG.IsGauntlet = true;

                    // Associate it with the orderflow
                    orderflow.GauntletFVG = gauntletFVG;

                    // Add to our collection of Gauntlets if not already present
                    if (!_gauntlets.Any(g => g.Index == gauntletFVG.Index &&
                                             g.Direction == gauntletFVG.Direction))
                    {
                        _gauntlets.Add(gauntletFVG);
                    }

                    // Draw it if visualization is enabled
                    if (_showGauntlet)
                    {
                        DrawGauntlet(gauntletFVG);
                    }

                    // Exit early - we found our Gauntlet
                    return;
                }
            }

            // If we couldn't find a matching FVG from the detector, 
            // try to detect an FVG pattern directly
            gauntletFVG = DetectFVGPatternInOrderflow(orderflow, sweepingCandleIndex);

            if (gauntletFVG != null)
            {
                // Mark as Gauntlet
                gauntletFVG.IsGauntlet = true;

                // Associate with orderflow
                orderflow.GauntletFVG = gauntletFVG;

                // Add to Gauntlets collection
                _gauntlets.Add(gauntletFVG);

                // Draw if visualization is enabled
                if (_showGauntlet)
                {
                    DrawGauntlet(gauntletFVG);
                }
            }
        }

        /// <summary>
        /// Finds the last (most recent) FVG contained within an orderflow's boundaries
        /// </summary>
        private Level FindLastFVGInOrderflow(Level orderflow, List<Level> allFvgs)
        {
            // Define the price and time boundaries of the orderflow
            double lowPrice = orderflow.Low;
            double highPrice = orderflow.High;
            DateTime earliestTime = orderflow.Direction == Direction.Up ? orderflow.LowTime : orderflow.HighTime;
            DateTime latestTime = orderflow.Direction == Direction.Up ? orderflow.HighTime : orderflow.LowTime;

            // Filter FVGs that have the same direction as the orderflow and are contained within its boundaries
            var matchingFvgs = allFvgs
                .Where(fvg =>
                    // Same direction
                    fvg.Direction == orderflow.Direction &&
                    // Within price boundaries (at least partially)
                    !(fvg.High < lowPrice || fvg.Low > highPrice) &&
                    // Within time boundaries
                    fvg.MidTime >= earliestTime && fvg.MidTime <= latestTime)
                // Sort by index (descending) to get the most recent first
                .OrderByDescending(fvg => fvg.Index)
                .ToList();

            // Return the most recent FVG if any were found
            return matchingFvgs.FirstOrDefault();
        }

        /// <summary>
        /// Detects an FVG pattern directly from the price action within the orderflow boundaries
        /// </summary>
        private Level DetectFVGPatternInOrderflow(Level orderflow, int sweepingCandleIndex)
        {
            // Define the index range within the orderflow
            int startIndex = Math.Min(orderflow.IndexLow, orderflow.IndexHigh);
            int endIndex = Math.Max(orderflow.IndexLow, orderflow.IndexHigh);

            // Check if the sweeping candle is within the orderflow
            if (sweepingCandleIndex < startIndex || sweepingCandleIndex > endIndex)
                return null;

            // Try to detect an FVG with the sweeping candle as either the second or third candle

            // Case 1: Sweeping candle as the third candle
            if (sweepingCandleIndex >= startIndex + 2 && sweepingCandleIndex <= endIndex)
            {
                // Get the three consecutive bars
                var bar1 = Bars[sweepingCandleIndex - 2]; // First candle
                var bar2 = Bars[sweepingCandleIndex - 1]; // Middle candle
                var bar3 = Bars[sweepingCandleIndex]; // Sweeping candle (third candle)

                // Check for valid FVG pattern
                if (orderflow.Direction == Direction.Up && bar1.High < bar3.Low)
                {
                    // Bullish FVG
                    return new Level(
                        LevelType.FairValueGap,
                        bar1.High,
                        bar3.Low,
                        bar1.OpenTime,
                        bar3.OpenTime,
                        bar2.OpenTime,
                        Direction.Up,
                        sweepingCandleIndex - 2,
                        sweepingCandleIndex,
                        sweepingCandleIndex - 2,
                        sweepingCandleIndex - 1,
                        Zone.Premium
                    );
                }
                else if (orderflow.Direction == Direction.Down && bar1.Low > bar3.High)
                {
                    // Bearish FVG
                    return new Level(
                        LevelType.FairValueGap,
                        bar3.High,
                        bar1.Low,
                        bar3.OpenTime,
                        bar1.OpenTime,
                        bar2.OpenTime,
                        Direction.Down,
                        sweepingCandleIndex - 2,
                        sweepingCandleIndex - 2,
                        sweepingCandleIndex,
                        sweepingCandleIndex - 1,
                        Zone.Discount
                    );
                }
            }

            // Case 2: Sweeping candle as the second candle
            if (sweepingCandleIndex >= startIndex + 1 && sweepingCandleIndex < endIndex)
            {
                // Get the three consecutive bars
                var bar1 = Bars[sweepingCandleIndex - 1]; // First candle
                var bar2 = Bars[sweepingCandleIndex]; // Sweeping candle (second candle)
                var bar3 = Bars[sweepingCandleIndex + 1]; // Third candle

                // Check for valid FVG pattern
                if (orderflow.Direction == Direction.Up && bar1.High < bar3.Low)
                {
                    // Bullish FVG
                    return new Level(
                        LevelType.FairValueGap,
                        bar1.High,
                        bar3.Low,
                        bar1.OpenTime,
                        bar3.OpenTime,
                        bar2.OpenTime,
                        Direction.Up,
                        sweepingCandleIndex - 1,
                        sweepingCandleIndex + 1,
                        sweepingCandleIndex - 1,
                        sweepingCandleIndex,
                        Zone.Premium
                    );
                }
                else if (orderflow.Direction == Direction.Down && bar1.Low > bar3.High)
                {
                    // Bearish FVG
                    return new Level(
                        LevelType.FairValueGap,
                        bar3.High,
                        bar1.Low,
                        bar3.OpenTime,
                        bar1.OpenTime,
                        bar2.OpenTime,
                        Direction.Down,
                        sweepingCandleIndex - 1,
                        sweepingCandleIndex - 1,
                        sweepingCandleIndex + 1,
                        sweepingCandleIndex,
                        Zone.Discount
                    );
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the most relevant FVG where the sweeping candle is either the second or third candle,
        /// prioritizing the third candle of the FVG pattern
        /// </summary>
        private Level FindGauntletFVG(Level orderflow, int sweepingCandleIndex, List<Level> allFvgs)
        {
            // First, try to find FVGs where the sweeping candle is the third candle (most significant)
            Level thirdCandleMatch = null;
            Level secondCandleMatch = null;

            // Sort FVGs by Index (descending) to get the most recent ones first
            var sortedFvgs = allFvgs
                .Where(fvg => fvg.Direction == orderflow.Direction)
                .OrderByDescending(fvg => fvg.Index)
                .ToList();

            foreach (var fvg in sortedFvgs)
            {
                if (orderflow.Direction == Direction.Up)
                {
                    // For bullish FVGs, the third candle is IndexHigh (the candle that creates the gap)
                    if (sweepingCandleIndex == fvg.IndexHigh)
                    {
                        thirdCandleMatch = fvg;
                        break; // Found the most relevant FVG, stop searching
                    }
                    // Store second candle match as fallback
                    else if (sweepingCandleIndex == fvg.IndexMid && secondCandleMatch == null)
                    {
                        secondCandleMatch = fvg;
                        // Don't break, keep looking for third candle matches
                    }
                }
                else // Direction.Down
                {
                    // For bearish FVGs, the third candle is IndexLow (the candle that creates the gap)
                    if (sweepingCandleIndex == fvg.IndexLow)
                    {
                        thirdCandleMatch = fvg;
                        break; // Found the most relevant FVG, stop searching
                    }
                    // Store second candle match as fallback
                    else if (sweepingCandleIndex == fvg.IndexMid && secondCandleMatch == null)
                    {
                        secondCandleMatch = fvg;
                        // Don't break, keep looking for third candle matches
                    }
                }
            }

            // Return the third candle match if found, otherwise return the second candle match
            return thirdCandleMatch ?? secondCandleMatch;
        }

        /// <summary>
        /// Detects an FVG pattern directly from the price action if not found in the FVG detector,
        /// prioritizing patterns where the sweeping candle is the third candle
        /// </summary>
        private Level DetectFVGPattern(Level orderflow, int sweepingCandleIndex)
        {
            // First, try to detect an FVG with the sweeping candle as the third candle (most significant)
            Level thirdCandlePattern = DetectThirdCandleFVGPattern(orderflow, sweepingCandleIndex);
            if (thirdCandlePattern != null)
                return thirdCandlePattern;

            // If not found, fall back to detecting an FVG with the sweeping candle as the second candle
            return DetectSecondCandleFVGPattern(orderflow, sweepingCandleIndex);
        }

        /// <summary>
        /// Detects an FVG pattern where the sweeping candle is the third candle
        /// </summary>
        private Level DetectThirdCandleFVGPattern(Level orderflow, int sweepingCandleIndex)
        {
            // We need at least 3 candles to detect an FVG
            if (sweepingCandleIndex < 2)
                return null;

            // Get the three consecutive bars with sweeping candle as the third
            var bar1 = Bars[sweepingCandleIndex - 2]; // First candle
            var bar2 = Bars[sweepingCandleIndex - 1]; // Middle candle
            var bar3 = Bars[sweepingCandleIndex]; // Sweeping candle (Third candle)

            // Check for bullish FVG pattern (bar1's high is lower than bar3's low)
            if (bar1.High < bar3.Low && orderflow.Direction == Direction.Up)
            {
                // Create a bullish FVG level
                return new Level(
                    LevelType.FairValueGap,
                    bar1.High,
                    bar3.Low,
                    bar1.OpenTime,
                    bar3.OpenTime,
                    bar2.OpenTime,
                    Direction.Up,
                    sweepingCandleIndex - 2, // Index is first candle
                    sweepingCandleIndex, // IndexHigh is third candle (sweeping candle)
                    sweepingCandleIndex - 2, // IndexLow is first candle
                    sweepingCandleIndex - 1, // IndexMid is second candle
                    Zone.Premium
                );
            }
            // Check for bearish FVG pattern (bar1's low is higher than bar3's high)
            else if (bar1.Low > bar3.High && orderflow.Direction == Direction.Down)
            {
                // Create a bearish FVG level
                return new Level(
                    LevelType.FairValueGap,
                    bar3.High,
                    bar1.Low,
                    bar3.OpenTime,
                    bar1.OpenTime,
                    bar2.OpenTime,
                    Direction.Down,
                    sweepingCandleIndex - 2, // Index is first candle
                    sweepingCandleIndex - 2, // IndexHigh is first candle
                    sweepingCandleIndex, // IndexLow is third candle (sweeping candle)
                    sweepingCandleIndex - 1, // IndexMid is second candle
                    Zone.Discount
                );
            }

            return null;
        }

        /// <summary>
        /// Detects an FVG pattern where the sweeping candle is the second candle
        /// </summary>
        private Level DetectSecondCandleFVGPattern(Level orderflow, int sweepingCandleIndex)
        {
            // We need to have available bars for all three candles
            if (sweepingCandleIndex < 1 || sweepingCandleIndex + 1 >= Bars.Count)
                return null;

            // Get the three consecutive bars with sweeping candle as the second
            var bar1 = Bars[sweepingCandleIndex - 1]; // First candle
            var bar2 = Bars[sweepingCandleIndex]; // Sweeping candle (Second candle)
            var bar3 = Bars[sweepingCandleIndex + 1]; // Third candle

            // Check for bullish FVG pattern (bar1's high is lower than bar3's low)
            if (bar1.High < bar3.Low && orderflow.Direction == Direction.Up)
            {
                // Create a bullish FVG level
                return new Level(
                    LevelType.FairValueGap,
                    bar1.High,
                    bar3.Low,
                    bar1.OpenTime,
                    bar3.OpenTime,
                    bar2.OpenTime,
                    Direction.Up,
                    sweepingCandleIndex - 1, // Index is first candle
                    sweepingCandleIndex + 1, // IndexHigh is third candle
                    sweepingCandleIndex - 1, // IndexLow is first candle
                    sweepingCandleIndex, // IndexMid is second candle (sweeping candle)
                    Zone.Premium
                );
            }
            // Check for bearish FVG pattern (bar1's low is higher than bar3's high)
            else if (bar1.Low > bar3.High && orderflow.Direction == Direction.Down)
            {
                // Create a bearish FVG level
                return new Level(
                    LevelType.FairValueGap,
                    bar3.High,
                    bar1.Low,
                    bar3.OpenTime,
                    bar1.OpenTime,
                    bar2.OpenTime,
                    Direction.Down,
                    sweepingCandleIndex - 1, // Index is first candle
                    sweepingCandleIndex - 1, // IndexHigh is first candle
                    sweepingCandleIndex + 1, // IndexLow is third candle
                    sweepingCandleIndex, // IndexMid is second candle (sweeping candle)
                    Zone.Discount
                );
            }

            return null;
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
        /// Draws a Gauntlet on the chart using the order block drawing method
        /// </summary>
        private void DrawGauntlet(Level gauntlet)
        {
            if (_chart == null)
                return;

            // Create a unique ID for this Gauntlet
            string id = $"gauntlet-{gauntlet.Direction}-{gauntlet.Index}-{gauntlet.IndexHigh}-{gauntlet.IndexLow}";

            // Use the existing DrawRectangle method from ChartExtensions
            // but with higher opacity to distinguish from other elements
            _chart.DrawRectangle(
                gauntlet,
                id,
                true, // Draw midpoint
                25 // Higher opacity for Gauntlets
            );
        }

        // Detect CISD from orderflow that swept liquidity
        private void DetectCISDLevel(Level orderflow)
        {
            // Skip if CISD visualization is not enabled
            if (!_showCISD)
                return;

            // Only process orderflows that swept liquidity
            if (orderflow.SweptSwingPoint == null)
                return;

            // Define search range based on direction of the orderflow
            int startIndex = Math.Min(orderflow.IndexLow, orderflow.IndexHigh);
            int endIndex = Math.Max(orderflow.IndexLow, orderflow.IndexHigh);

            // Ensure we have valid indices
            if (startIndex < 0 || endIndex < 0 || startIndex >= Bars.Count || endIndex >= Bars.Count)
                return;

            if (orderflow.Direction == Direction.Up)
            {
                // For bullish orderflow, find a sequence of consecutive bullish candles
                int firstBullishIndex = -1;
                int lastBullishIndex = -1;

                // Start from the beginning of the orderflow
                for (int i = startIndex; i <= endIndex; i++)
                {
                    Bar currentBar = Bars[i];
                    bool isBullish = currentBar.Close > currentBar.Open;

                    if (isBullish)
                    {
                        if (firstBullishIndex == -1)
                        {
                            // Found first bullish candle
                            firstBullishIndex = i;
                        }

                        // Update the last bullish candle index
                        lastBullishIndex = i;
                    }
                    else if (firstBullishIndex != -1)
                    {
                        // Break the sequence on bearish candle
                        break;
                    }
                }

                // If we found at least one bullish candle, create a CISD level
                if (firstBullishIndex >= 0 && lastBullishIndex >= 0)
                {
                    // Create a bullish CISD level - using first candle's open as low and last candle's close as high
                    var cisdLevel = new Level(
                        LevelType.CISD,
                        Bars[firstBullishIndex].Open, // Low is the opening price of first bullish candle
                        Bars[lastBullishIndex].Close, // High is the closing price of last bullish candle
                        Bars[firstBullishIndex].OpenTime,
                        Bars[lastBullishIndex].OpenTime,
                        null,
                        Direction.Up,
                        firstBullishIndex,
                        lastBullishIndex,
                        firstBullishIndex
                    );

                    // Associate with orderflow
                    orderflow.CISDLevel = cisdLevel;

                    // Add to CISD collection
                    _cisdLevels.Add(cisdLevel);
                }
            }
            else // Direction.Down
            {
                // For bearish orderflow, find a sequence of consecutive bearish candles
                int firstBearishIndex = -1;
                int lastBearishIndex = -1;

                // Start from the beginning of the orderflow
                for (int i = startIndex; i <= endIndex; i++)
                {
                    Bar currentBar = Bars[i];
                    bool isBearish = currentBar.Close < currentBar.Open;

                    if (isBearish)
                    {
                        if (firstBearishIndex == -1)
                        {
                            // Found first bearish candle
                            firstBearishIndex = i;
                        }

                        // Update the last bearish candle index
                        lastBearishIndex = i;
                    }
                    else if (firstBearishIndex != -1)
                    {
                        // Break the sequence on bullish candle
                        break;
                    }
                }

                // If we found at least one bearish candle, create a CISD level
                if (firstBearishIndex >= 0 && lastBearishIndex >= 0)
                {
                    // Create a bearish CISD level - using first candle's open as high and last candle's close as low
                    var cisdLevel = new Level(
                        LevelType.CISD,
                        Bars[lastBearishIndex].Close, // Low is the closing price of last bearish candle
                        Bars[firstBearishIndex].Open, // High is the opening price of first bearish candle
                        Bars[lastBearishIndex].OpenTime,
                        Bars[firstBearishIndex].OpenTime,
                        null,
                        Direction.Down,
                        firstBearishIndex,
                        firstBearishIndex,
                        lastBearishIndex
                    );

                    // Associate with orderflow
                    orderflow.CISDLevel = cisdLevel;

                    // Add to CISD collection
                    _cisdLevels.Add(cisdLevel);
                }
            }
        }

// Check for CISD confirmation and activation
        private void CheckCISDActivity(SwingPoint swingPoint, int currentIndex)
        {
            if (!_showCISD)
                return;

            // Get all CISD levels that are not yet confirmed
            var pendingCisdLevels = _cisdLevels
                .Where(cisd => !cisd.IsConfirmed)
                .ToList();

            // Check for CISD confirmation
            foreach (var cisd in pendingCisdLevels)
            {
                if (cisd.Direction == Direction.Up)
                {
                    // Bullish CISD is confirmed when a bearish candle closes below the CISD level (low)
                    if (swingPoint.CandleDirection == Direction.Down &&
                        swingPoint.Bar.Close < cisd.Low)
                    {
                        cisd.IsConfirmed = true;

                        // Draw a confirmation line
                        if (_chart != null)
                        {
                            string confirmId = $"cisd-confirm-{cisd.Direction}-{cisd.Index}-{swingPoint.Index}";
                            _chart.DrawStraightLine(
                                confirmId,
                                cisd.LowTime,
                                cisd.Low,
                                swingPoint.Time,
                                cisd.Low,
                                null,
                                LineStyle.Dots,
                                Color.Pink,
                                false,
                                true,
                                false
                            );
                        }
                    }
                }
                else // Direction.Down
                {
                    // Bearish CISD is confirmed when a bullish candle closes above the CISD level (high)
                    if (swingPoint.CandleDirection == Direction.Up &&
                        swingPoint.Bar.Close > cisd.High)
                    {
                        cisd.IsConfirmed = true;

                        // Draw a confirmation line
                        if (_chart != null)
                        {
                            string confirmId = $"cisd-confirm-{cisd.Direction}-{cisd.Index}-{swingPoint.Index}";
                            _chart.DrawStraightLine(
                                confirmId,
                                cisd.HighTime,
                                cisd.High,
                                swingPoint.Time,
                                cisd.High,
                                null,
                                LineStyle.Dots,
                                Color.Pink,
                                false,
                                true,
                                false
                            );
                        }
                    }
                }
            }

            // Get all CISD levels that are confirmed but not activated
            var confirmedCisdLevels = _cisdLevels
                .Where(cisd => cisd.IsConfirmed && !cisd.Activated)
                .ToList();

            // Check for CISD activation
            foreach (var cisd in confirmedCisdLevels)
            {
                if (cisd.Direction == Direction.Up)
                {
                    // Bullish CISD is activated when a bullish swing point's price moves above the CISD level (low)
                    if (swingPoint.Direction == Direction.Up &&
                        swingPoint.Price > cisd.Low)
                    {
                        cisd.Activated = true;

                        // Draw a CISD activation line
                        DrawCISDLine(cisd, swingPoint);

                        // Update swing point
                        swingPoint.ActivatedCISD = true;
                        swingPoint.ActivatedCISDLevel = cisd;
                    }
                }
                else // Direction.Down
                {
                    // Bearish CISD is activated when a bearish swing point's price moves below the CISD level (high)
                    if (swingPoint.Direction == Direction.Down &&
                        swingPoint.Price < cisd.High)
                    {
                        cisd.Activated = true;

                        // Draw a CISD activation line
                        DrawCISDLine(cisd, swingPoint);

                        // Update swing point
                        swingPoint.ActivatedCISD = true;
                        swingPoint.ActivatedCISDLevel = cisd;
                    }
                }
            }
        }

// Replace the incorrect DrawCISDLine method with this corrected version
        private void DrawCISDLine(Level cisdLevel, SwingPoint activatingPoint)
        {
            if (_chart == null)
                return;

            // Create a unique ID for this CISD line
            string id = $"cisd-{cisdLevel.Direction}-{cisdLevel.Index}-{activatingPoint.Index}";
    
            double priceLevel = cisdLevel.Direction == Direction.Up ? cisdLevel.Low : cisdLevel.High;
            DateTime startTime = cisdLevel.Direction == Direction.Up ? cisdLevel.LowTime : cisdLevel.HighTime;
    
            // Draw a horizontal line from CISD level to activating point
            _chart.DrawStraightLine(
                id,
                startTime,
                priceLevel,
                activatingPoint.Time,
                priceLevel,
                null,  // No label
                LineStyle.Solid,
                Color.Pink,
                false,  // No label displayed
                true,   // Remove existing
                false   // Not extended
            );
        }

// Add method to get all CISD levels
        public List<Level> GetAllCISDLevels()
        {
            return _cisdLevels;
        }

// Add method to get active CISD levels
        public List<Level> GetActiveCISDLevels()
        {
            return _cisdLevels.Where(cisd => cisd.Activated).ToList();
        }

// Add method to get confirmed CISD levels
        public List<Level> GetConfirmedCISDLevels()
        {
            return _cisdLevels.Where(cisd => cisd.IsConfirmed).ToList();
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
        /// Gets all Gauntlets
        /// </summary>
        public List<Level> GetGauntlets()
        {
            return _gauntlets;
        }

        /// <summary>
        /// Gets all Gauntlets that match the given direction
        /// </summary>
        public List<Level> GetGauntlets(Direction direction)
        {
            return _gauntlets.Where(g => g.Direction == direction).ToList();
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