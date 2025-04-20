using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using Zuva.Models;

namespace Zuva.Extensions
{
    public static class ChochExtensions
    {
        /// <summary>
        /// Detects and processes Change of Character (CHoCH) patterns
        /// </summary>
        /// <param name="swingPoints">The list of all swing points</param>
        /// <param name="currentBar">The current bar being processed</param>
        /// <param name="chart">The chart for drawing</param>
        /// <param name="showChoCh">Flag to determine whether to display CHoCH on chart</param>
        /// <param name="externalLiquidity">List to track external liquidity points</param>
        /// <param name="currentIndex">The index of the current bar</param>
        /// <returns>Tuple containing: whether a CHoCH was detected, the CHoCH level, and direction</returns>
        public static (bool detected, Level chochLevel, Direction direction) DetectChoCh(
            this List<SwingPoint> swingPoints, 
            Bar currentBar, 
            Chart chart, 
            bool showChoCh,
            ref List<SwingPoint> externalLiquidity,
            int currentIndex)
        {
            if (swingPoints.Count < 4)
                return (false, null, Direction.Up);

            var orderedPoints = swingPoints.OrderByDescending(p => p.Index).ToList();
            
            // Check for bullish CHoCH
            var bullishChoCh = DetectBullishChoCh(orderedPoints, currentBar, chart, showChoCh, ref externalLiquidity, currentIndex);
            if (bullishChoCh.detected)
                return bullishChoCh;
                
            // Check for bearish CHoCH
            var bearishChoCh = DetectBearishChoCh(orderedPoints, currentBar, chart, showChoCh, ref externalLiquidity, currentIndex);
            if (bearishChoCh.detected)
                return bearishChoCh;
                
            // No CHoCH detected
            return (false, null, Direction.Up);
        }
        
        /// <summary>
        /// Detects a bullish Change of Character pattern
        /// </summary>
        private static (bool detected, Level chochLevel, Direction direction) DetectBullishChoCh(
            List<SwingPoint> orderedPoints,
            Bar currentBar,
            Chart chart,
            bool showChoCh,
            ref List<SwingPoint> externalLiquidity,
            int currentIndex)
        {
            // Get the recent low swing points
            var lowPoints = orderedPoints.Where(p => p.Direction == Direction.Down).Take(3).ToList();
            
            if (lowPoints.Count < 2)
                return (false, null, Direction.Up);
                
            // Check if we have a lower low (most recent low is lower than previous low)
            if (lowPoints[0].Price >= lowPoints[1].Price)
                return (false, null, Direction.Up);
                
            // Find the swing high that led to the current lowest swing low
            var highPoints = orderedPoints.Where(p => p.Direction == Direction.Up).ToList();
            if (highPoints.Count < 1)
                return (false, null, Direction.Up);
                
            // The high point that we need to break for a bullish CHoCH
            var potentialChochPoint = highPoints.FirstOrDefault(h => h.Index < lowPoints[0].Index && h.Index > lowPoints[1].Index);
            
            if (potentialChochPoint == null)
                return (false, null, Direction.Up);
                
            // Check if current bar closes above the potential CHoCH point
            if (currentBar.Close > potentialChochPoint.Price && currentBar.Close > currentBar.Open)
            {
                // CHoCH confirmed!
                var chochLevel = new Level(
                    LevelType.CISD,
                    lowPoints[0].Price,
                    potentialChochPoint.Price,
                    lowPoints[0].Time,
                    potentialChochPoint.Time,
                    direction: Direction.Up,
                    index: potentialChochPoint.Index,
                    indexLow: lowPoints[0].Index,
                    indexHigh: potentialChochPoint.Index);
                
                // Mark the lowest low as external liquidity
                var lowPoint = lowPoints[0];
                lowPoint.SwingType = SwingType.LL;
                
                if (!externalLiquidity.Any(l => l.Index == lowPoint.Index))
                {
                    externalLiquidity.Add(lowPoint);
                }
                
                if (showChoCh)
                {
                    // Create a temporary swing point to represent current bar for drawing
                    var currentPoint = new SwingPoint(
                        currentIndex,
                        currentBar.Close,
                        currentBar.OpenTime,
                        currentBar,
                        SwingType.H,
                        direction: Direction.Up
                    );
                    
                    chart.DrawTrendLine($"choch-{potentialChochPoint.Time}", potentialChochPoint, currentPoint, LineType.CHOCH);
                }
                
                return (true, chochLevel, Direction.Up);
            }
            
            return (false, null, Direction.Up);
        }
        
        /// <summary>
        /// Detects a bearish Change of Character pattern
        /// </summary>
        private static (bool detected, Level chochLevel, Direction direction) DetectBearishChoCh(
            List<SwingPoint> orderedPoints,
            Bar currentBar,
            Chart chart,
            bool showChoCh,
            ref List<SwingPoint> externalLiquidity,
            int currentIndex)
        {
            // Get the recent high swing points
            var highPoints = orderedPoints.Where(p => p.Direction == Direction.Up).Take(3).ToList();
            
            if (highPoints.Count < 2)
                return (false, null, Direction.Down);
                
            // Check if we have a higher high (most recent high is higher than previous high)
            if (highPoints[0].Price <= highPoints[1].Price)
                return (false, null, Direction.Down);
                
            // Find the swing low that led to the current highest swing high
            var lowPoints = orderedPoints.Where(p => p.Direction == Direction.Down).ToList();
            if (lowPoints.Count < 1)
                return (false, null, Direction.Down);
                
            // The low point that we need to break for a bearish CHoCH
            var potentialChochPoint = lowPoints.FirstOrDefault(l => l.Index < highPoints[0].Index && l.Index > highPoints[1].Index);
            
            if (potentialChochPoint == null)
                return (false, null, Direction.Down);
                
            // Check if current bar closes below the potential CHoCH point
            if (currentBar.Close < potentialChochPoint.Price && currentBar.Close < currentBar.Open)
            {
                // CHoCH confirmed!
                var chochLevel = new Level(
                    LevelType.CISD,
                    potentialChochPoint.Price,
                    highPoints[0].Price,
                    potentialChochPoint.Time,
                    highPoints[0].Time,
                    direction: Direction.Down,
                    index: potentialChochPoint.Index,
                    indexLow: potentialChochPoint.Index,
                    indexHigh: highPoints[0].Index);
                
                // Mark the highest high as external liquidity
                var highPoint = highPoints[0];
                highPoint.SwingType = SwingType.HH;
                
                if (!externalLiquidity.Any(l => l.Index == highPoint.Index))
                {
                    externalLiquidity.Add(highPoint);
                }
                
                if (showChoCh)
                {
                    // Create a temporary swing point to represent current bar for drawing
                    var currentPoint = new SwingPoint(
                        currentIndex,
                        currentBar.Close,
                        currentBar.OpenTime,
                        currentBar,
                        SwingType.L,
                        direction: Direction.Down
                    );
                    
                    chart.DrawTrendLine($"choch-{potentialChochPoint.Time}", potentialChochPoint, currentPoint, LineType.CHOCH);
                }
                
                return (true, chochLevel, Direction.Down);
            }
            
            return (false, null, Direction.Down);
        }
        
        /// <summary>
        /// Updates the potential CHoCH point if conditions change
        /// </summary>
        /// <param name="swingPoints">The list of all swing points</param>
        /// <param name="potentialChochLevel">Current potential CHoCH level</param>
        /// <param name="currentSwingPoint">The current swing point being processed</param>
        /// <returns>Updated potential CHoCH level or null if conditions no longer valid</returns>
        public static Level UpdatePotentialChoCh(
            this List<SwingPoint> swingPoints,
            Level potentialChochLevel,
            SwingPoint currentSwingPoint)
        {
            if (potentialChochLevel == null || swingPoints.Count < 4)
                return potentialChochLevel;
                
            var orderedPoints = swingPoints.OrderByDescending(p => p.Index).ToList();
            
            if (potentialChochLevel.Direction == Direction.Up)
            {
                // If we form a new lower low that has a different high point before the marked potential CHoCH is taken out
                if (currentSwingPoint.Direction == Direction.Down)
                {
                    var previousLow = orderedPoints.FirstOrDefault(p => p.Direction == Direction.Down && p.Index != currentSwingPoint.Index);
                    
                    if (previousLow != null && currentSwingPoint.Price < previousLow.Price)
                    {
                        // Find the swing high that led to this new lowest swing low
                        var highPoint = orderedPoints.FirstOrDefault(h => 
                            h.Direction == Direction.Up && 
                            h.Index < currentSwingPoint.Index && 
                            h.Index != potentialChochLevel.IndexHigh);
                            
                        if (highPoint != null)
                        {
                            // Move the potential CHoCH to this new swing high
                            return new Level(
                                LevelType.CISD,
                                currentSwingPoint.Price,
                                highPoint.Price,
                                currentSwingPoint.Time,
                                highPoint.Time,
                                direction: Direction.Up,
                                index: highPoint.Index,
                                indexLow: currentSwingPoint.Index,
                                indexHigh: highPoint.Index);
                        }
                    }
                }
            }
            else // Direction.Down
            {
                // If we form a new higher high that has a different low point before the marked potential CHoCH is taken out
                if (currentSwingPoint.Direction == Direction.Up)
                {
                    var previousHigh = orderedPoints.FirstOrDefault(p => p.Direction == Direction.Up && p.Index != currentSwingPoint.Index);
                    
                    if (previousHigh != null && currentSwingPoint.Price > previousHigh.Price)
                    {
                        // Find the swing low that led to this new highest swing high
                        var lowPoint = orderedPoints.FirstOrDefault(l => 
                            l.Direction == Direction.Down && 
                            l.Index < currentSwingPoint.Index && 
                            l.Index != potentialChochLevel.IndexLow);
                            
                        if (lowPoint != null)
                        {
                            // Move the potential CHoCH to this new swing low
                            return new Level(
                                LevelType.CISD,
                                lowPoint.Price,
                                currentSwingPoint.Price,
                                lowPoint.Time,
                                currentSwingPoint.Time,
                                direction: Direction.Down,
                                index: lowPoint.Index,
                                indexLow: lowPoint.Index,
                                indexHigh: currentSwingPoint.Index);
                        }
                    }
                }
            }
            
            return potentialChochLevel;
        }
    }
}