using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using Zuva.Models;

namespace Zuva.Extensions;

public static class ActivationExtensions
{
    /// <summary>
    /// Checks if the current swing point activates any Order Blocks and returns the activated levels
    /// </summary>
    public static List<Level> CheckOrderBlockActivation(this SwingPoint swingPoint, List<Level> levels, Chart chart,
        bool drawActivation)
    {
        var activatedLevels = new List<Level>();

        if (levels == null || levels.Count == 0) return activatedLevels;

        // Get all Order Blocks that haven't been activated yet
        var orderBlocks = levels.Where(l => l.LevelType == LevelType.OrderBlock && !l.Activated).ToList();

        foreach (var ob in orderBlocks)
        {
            // Check if this swing point activates the Order Block
            bool isActivated = false;

            if (ob.Direction == Direction.Down) // Bearish Order Block
            {
                // A bearish Order Block is activated the same way as a bearish FVG
                if (swingPoint.Bar.Open < ob.Mid && swingPoint.Bar.High > ob.Mid && swingPoint.Bar.Close < ob.Mid)
                {
                    isActivated = true;
                }
            }
            else // Bullish Order Block
            {
                // A bullish Order Block is activated the same way as a bullish FVG
                if (swingPoint.Bar.Open > ob.Mid && swingPoint.Bar.Low < ob.Mid && swingPoint.Bar.Close > ob.Mid)
                {
                    isActivated = true;
                }
            }

            if (isActivated)
            {
                // Mark the Order Block as activated
                ob.Activated = true;

                // Link the swing point to the Order Block
                swingPoint.ActivatedOrderBlock = true;
                swingPoint.ActivatedOrderBlockLevel = ob;

                // Add to our list of activated levels
                activatedLevels.Add(ob);

                // Draw visualization if requested
                if (drawActivation)
                {
                    chart.DrawActivationRectangle(ob, swingPoint, "ob-activation");
                }
            }
        }

        return activatedLevels;
    }
}