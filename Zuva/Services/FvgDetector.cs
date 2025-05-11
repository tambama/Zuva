using cAlgo.API;
using Mwenje.Extensions;
using Zuva.Models;

namespace Zuva.Services
{
    /// <summary>
    /// Detects and tracks Fair Value Gaps (FVGs) and Order Blocks in price action
    /// </summary>
    public class FvgDetector
    {
        private readonly Chart _chart;
        private readonly bool _showFVG;
        private readonly bool _showOrderBlock;
        private readonly List<Level> _fvgs = new List<Level>();
        private readonly List<Level> _orderBlocks = new List<Level>();
        
        // Reference to swing points for order block detection
        private SwingPointDetector _swingPointDetector;
        
        public FvgDetector(Chart chart, bool showFVG, bool showOrderBlock, SwingPointDetector swingPointDetector = null)
        {
            _chart = chart;
            _showFVG = showFVG;
            _showOrderBlock = showOrderBlock;
            _swingPointDetector = swingPointDetector;
        }
        
        /// <summary>
        /// Detects Fair Value Gaps (FVGs) and Order Blocks in a series of bars
        /// </summary>
        public void DetectFVG(Bars bars, int currentIndex)
        {
            // Need at least 3 bars to detect a FVG
            if (currentIndex < 2)
                return;
                
            // Get the three consecutive bars
            var bar1 = bars[currentIndex - 2]; // First candle (order block candidate)
            var bar2 = bars[currentIndex - 1]; // Middle candle
            var bar3 = bars[currentIndex];     // Last candle
            
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
                    currentIndex - 1, // Store the middle candle index for Gauntlet detection
                    Zone.Premium  // FVGs in an uptrend are typically in the Premium zone
                );
                
                // Add to collection - always store FVGs regardless of visibility setting
                _fvgs.Add(bullishFVG);
                
                // Draw the FVG if visualization is enabled
                if (_showFVG)
                {
                    DrawFVG(bullishFVG);
                }
                
                // Check for bullish order block
                if (_showOrderBlock && currentIndex >= 3 && _swingPointDetector != null)
                {
                    // First, check if candle1 is a swing point (specifically a swing low)
                    // We're looking for an opposing swing point in the direction of the recent swing
                    var bar1SwingPoint = _swingPointDetector.GetSwingPointAtIndex(currentIndex - 2);
                    
                    // We need bar1 to be a swing low for a bullish order block
                    if (bar1SwingPoint != null && bar1SwingPoint.Direction == Direction.Down)
                    {
                        // Get the most recent swing point before this one
                        var previousSwingPoints = _swingPointDetector.GetAllSwingPoints()
                            .Where(sp => sp.Index < currentIndex - 2)
                            .OrderByDescending(sp => sp.Index)
                            .ToList();
                            
                        // Find the most recent swing high (for confirming directional alignment)
                        var lastSwingHigh = previousSwingPoints
                            .FirstOrDefault(sp => sp.Direction == Direction.Up);
                            
                        // Check if we've swept a previous swing low
                        bool sweptPreviousLow = false;
                        var previousSwingLow = previousSwingPoints
                            .FirstOrDefault(sp => sp.Direction == Direction.Down);
                            
                        if (previousSwingLow != null)
                        {
                            sweptPreviousLow = bar1.Low <= previousSwingLow.Price && 
                                              bar1.Close > previousSwingLow.Price;
                        }
                        
                        // Verify that the candle meets our order block criteria:
                        // 1. It's a swing low
                        // 2. Either it swept a previous swing low OR it came after a swing high
                        if (sweptPreviousLow || (lastSwingHigh != null && lastSwingHigh.Index < bar1SwingPoint.Index))
                        {
                            CreateBullishOrderBlock(bars, bar1, currentIndex - 2);
                        }
                    }
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
                    currentIndex - 1, // Store the middle candle index for Gauntlet detection
                    Zone.Discount  // FVGs in a downtrend are typically in the Discount zone
                );
                
                // Add to collection - always store FVGs regardless of visibility setting
                _fvgs.Add(bearishFVG);
                
                // Draw the FVG if visualization is enabled
                if (_showFVG)
                {
                    DrawFVG(bearishFVG);
                }
                
                // Check for bearish order block
                if (_showOrderBlock && currentIndex >= 3 && _swingPointDetector != null)
                {
                    // First, check if candle1 is a swing point (specifically a swing high)
                    // We're looking for an opposing swing point in the direction of the recent swing
                    var bar1SwingPoint = _swingPointDetector.GetSwingPointAtIndex(currentIndex - 2);
                    
                    // We need bar1 to be a swing high for a bearish order block
                    if (bar1SwingPoint != null && bar1SwingPoint.Direction == Direction.Up)
                    {
                        // Get the most recent swing point before this one
                        var previousSwingPoints = _swingPointDetector.GetAllSwingPoints()
                            .Where(sp => sp.Index < currentIndex - 2)
                            .OrderByDescending(sp => sp.Index)
                            .ToList();
                            
                        // Find the most recent swing low (for confirming directional alignment)
                        var lastSwingLow = previousSwingPoints
                            .FirstOrDefault(sp => sp.Direction == Direction.Down);
                            
                        // Check if we've swept a previous swing high
                        bool sweptPreviousHigh = false;
                        var previousSwingHigh = previousSwingPoints
                            .FirstOrDefault(sp => sp.Direction == Direction.Up);
                            
                        if (previousSwingHigh != null)
                        {
                            sweptPreviousHigh = bar1.High >= previousSwingHigh.Price && 
                                               bar1.Close < previousSwingHigh.Price;
                        }
                        
                        // Verify that the candle meets our order block criteria:
                        // 1. It's a swing high
                        // 2. Either it swept a previous swing high OR it came after a swing low
                        if (sweptPreviousHigh || (lastSwingLow != null && lastSwingLow.Index < bar1SwingPoint.Index))
                        {
                            CreateBearishOrderBlock(bars, bar1, currentIndex - 2);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Creates a bullish order block from a candle
        /// </summary>
        private void CreateBullishOrderBlock(Bars bars, Bar bar, int index)
        {
            // Create an order block from the candle's FULL range (high to low)
            var orderBlock = new Level(
                LevelType.OrderBlock,
                bar.Low,              // Use the full candle low
                bar.High,             // Use the full candle high
                bar.OpenTime,
                bar.OpenTime.AddMinutes(5),     // 5 minute span for visualization
                bar.OpenTime,
                Direction.Up,
                index,
                index,
                index
            );
            
            // Check if we already have this order block to avoid duplicates
            if (!_orderBlocks.Any(ob => 
                ob.Index == orderBlock.Index && 
                ob.Direction == orderBlock.Direction))
            {
                _orderBlocks.Add(orderBlock);
                
                // Draw the order block if visualization is enabled
                if (_showOrderBlock)
                {
                    DrawOrderBlock(orderBlock);
                }
            }
        }
        
        /// <summary>
        /// Creates a bearish order block from a candle
        /// </summary>
        private void CreateBearishOrderBlock(Bars bars, Bar bar, int index)
        {
            // Create an order block from the candle's FULL range (high to low)
            var orderBlock = new Level(
                LevelType.OrderBlock,
                bar.Low,              // Use the full candle low
                bar.High,             // Use the full candle high
                bar.OpenTime,
                bar.OpenTime.AddMinutes(5),     // 5 minute span for visualization
                bar.OpenTime,
                Direction.Down,
                index,
                index,
                index
            );
            
            // Check if we already have this order block to avoid duplicates
            if (!_orderBlocks.Any(ob => 
                ob.Index == orderBlock.Index && 
                ob.Direction == orderBlock.Direction))
            {
                _orderBlocks.Add(orderBlock);
                
                // Draw the order block if visualization is enabled
                if (_showOrderBlock)
                {
                    DrawOrderBlock(orderBlock);
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
        /// Draws an Order Block on the chart
        /// </summary>
        private void DrawOrderBlock(Level orderBlock)
        {
            // Create a unique ID for this order block
            string id = $"ob-{orderBlock.Direction}-{orderBlock.Index}";
            
            // Use the existing DrawRectangle method from ChartExtensions
            // Create a 5-minute rectangle for the order block
            Color color = orderBlock.Direction == Direction.Up ? Color.Green : Color.Red;
            
            // Draw rectangle with 5-minute duration
            _chart.DrawRectangle(
                orderBlock,
                id,
                true, // Draw midpoint
                20    // Higher opacity for order blocks to make them more visible
            );
        }
        
        /// <summary>
        /// Checks if a level is in a Fair Value Gap
        /// </summary>
        public bool IsInFVG(double price, DateTime time)
        {
            return _fvgs.Any(fvg => price >= fvg.Low && price <= fvg.High && time >= fvg.LowTime && time <= fvg.HighTime.AddMinutes(5));
        }
        
        /// <summary>
        /// Checks if a level is in an Order Block
        /// </summary>
        public bool IsInOrderBlock(double price, DateTime time)
        {
            return _orderBlocks.Any(ob => 
                price >= ob.Low && 
                price <= ob.High && 
                time >= ob.LowTime);
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
        /// Get all Order Blocks
        /// </summary>
        public List<Level> GetAllOrderBlocks()
        {
            return _orderBlocks;
        }
        
        /// <summary>
        /// Get bullish Order Blocks
        /// </summary>
        public List<Level> GetBullishOrderBlocks()
        {
            return _orderBlocks.Where(ob => ob.Direction == Direction.Up).ToList();
        }
        
        /// <summary>
        /// Get bearish Order Blocks
        /// </summary>
        public List<Level> GetBearishOrderBlocks()
        {
            return _orderBlocks.Where(ob => ob.Direction == Direction.Down).ToList();
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
        
        /// <summary>
        /// Updates the visibility of all Order Blocks based on the showOrderBlock parameter
        /// </summary>
        public void UpdateOrderBlockVisibility(bool showOrderBlock)
        {
            if (_chart == null)
                return;
                
            if (showOrderBlock)
            {
                // Draw all existing Order Blocks
                foreach (var ob in _orderBlocks)
                {
                    DrawOrderBlock(ob);
                }
            }
            else
            {
                // Remove all Order Block visualization elements
                foreach (var ob in _orderBlocks)
                {
                    string id = $"ob-{ob.Direction}-{ob.Index}";
                    _chart.RemoveObject(id); // Remove the rectangle
                }
            }
        }
    }
}