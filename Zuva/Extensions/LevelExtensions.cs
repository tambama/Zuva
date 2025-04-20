using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using Zuva.Models;

namespace Zuva.Extensions;

public static class LevelExtensions
{
    public static (bool success, Level fvg) FindFairValueGap(this List<Level> levels, Bar barOne, Bar barThree, int index, DateTime midTime)
    {
        // Bullish FVG
        if (barThree.High < barOne.Low)
        {
            var fvg = new Level(LevelType.FairValueGap, barThree.High, barOne.Low, barThree.OpenTime, barOne.OpenTime, midTime, Direction.Up, index: index);
            levels.Add(fvg);
            
            return (true, fvg);
        }
        else if (barThree.Low > barOne.High)
        {
            var ce = (barThree.Low + barOne.High) / 2;
            var fvg = new Level(LevelType.FairValueGap, barOne.High, barThree.Low, barOne.OpenTime, barThree.OpenTime, midTime, Direction.Down);
            levels.Add(fvg);
        
            return (true, fvg);
        }
        
        return (false, null);
    }
    
    public static (bool success, Level orderBlock) FindOrderBlock(this List<Level> levels, List<SwingPoint> points, int index)
    {
        var point = points.FirstOrDefault(p => p.Index == index);
        if (point == null) return default;
        
        point.IsOrderBlock = true;
        var ob = new Level(LevelType.OrderBlock, point.Bar.Low, point.Bar.High, point.Time, point.Time, direction: point.Direction == Direction.Up ? Direction.Down : Direction.Up);
        levels.Add(ob);
            
        return (true, ob);
    }

    public static (bool success, Level orderBlock) FindOrderBlock(this List<Level> levels, Bar barFour, Bar barThree,
        Direction direction)
    {
        if (direction == Direction.Down)
        {
            if (barThree.High > barFour.High)
            {
                var ob = new Level(LevelType.OrderBlock, barThree.Low, barThree.High, barThree.OpenTime, barThree.OpenTime, direction: Direction.Down);
                levels.Add(ob);
                return (true, ob);
            }
        }
        
        if (direction == Direction.Up)
        {
            if (barThree.Low < barFour.Low)
            {
                var ob = new Level(LevelType.OrderBlock, barThree.Low, barThree.High, barThree.OpenTime, barThree.OpenTime, direction: Direction.Up);
                levels.Add(ob);
                return (true, ob);
            }
        }

        return default;
    }

    public static (bool yes, Level level) IsInOrderBlock(this List<Level> orderBlocks, SwingPoint swingPoint, Chart chart = null, bool showActivation = false)
    {
        if (orderBlocks == null || orderBlocks.Count == 0)
            return (false, null);
        
        if (swingPoint.Direction == Direction.Up)
        {
            var bearishOb = orderBlocks.OrderByDescending(o => o.Index).FirstOrDefault(o =>
                !o.Activated && !o.IsInverted &&
                o.Direction == Direction.Down &&
                swingPoint.Price < o.High && swingPoint.Price > o.Low &&
                swingPoint.Price >= o.Mid && swingPoint.Bar.Open < o.Mid);

            if (bearishOb == null) return default;
            
            bearishOb.Activated = true;
            
            swingPoint.ActivatedOrderBlock = true;
            swingPoint.ActivatedOrderBlockLevel = bearishOb;
            
            // Draw visualization if requested
            if (chart != null && showActivation)
            {
                chart.DrawActivationRectangle(bearishOb, swingPoint, "ob-activation");
            }
            
            return (true, bearishOb);
        }
        
        var bullishOb = orderBlocks.OrderByDescending(o => o.Index).FirstOrDefault(o => 
            !o.Activated && !o.IsInverted &&
            o.Direction == Direction.Up &&
            swingPoint.Price > o.Low && swingPoint.Price < o.High &&
            swingPoint.Price <= o.Mid && swingPoint.Bar.Open < o.Mid);

        if (bullishOb == null) return default;
        
        bullishOb.Activated = true;
        
        swingPoint.ActivatedOrderBlock = true;
        swingPoint.ActivatedOrderBlockLevel = bullishOb;
        
        // Draw visualization if requested
        if (chart != null && showActivation)
        {
            chart.DrawActivationRectangle(bullishOb, swingPoint, "ob-activation");
        }
        
        return (true, bullishOb);
    }
    
    public static (bool activated, Level level) IsInFVG(this List<Level> fvgLevels, SwingPoint swingPoint, Chart chart = null, bool showActivation = false)
    {
        if (fvgLevels == null || fvgLevels.Count == 0)
            return (false, null);
            
        // Get all FVGs that haven't been activated yet
        var fvgs = fvgLevels.Where(l => l.LevelType == LevelType.FairValueGap && !l.Activated).ToList();
        
        foreach (var fvg in fvgs)
        {
            // Check if this swing point activates the FVG
            bool isActivated = false;
            
            if (fvg.Direction == Direction.Down) // Bearish FVG
            {
                // A bearish FVG is activated when a bar's open is below the FVG's midpoint
                // AND its high is higher than the FVG's midpoint but close is lower than the midpoint
                if (swingPoint.Bar.Open < fvg.Mid && 
                    swingPoint.Bar.High > fvg.Mid && 
                    swingPoint.Bar.Close < fvg.Mid)
                {
                    isActivated = true;
                }
            }
            else // Bullish FVG
            {
                // A bullish FVG is activated when a bar's open is above the FVG's midpoint
                // AND its low is lower than the FVG's midpoint but close is higher than the midpoint
                if (swingPoint.Bar.Open > fvg.Mid && 
                    swingPoint.Bar.Low < fvg.Mid && 
                    swingPoint.Bar.Close > fvg.Mid)
                {
                    isActivated = true;
                }
            }
            
            if (isActivated)
            {
                // Mark the FVG as activated
                fvg.Activated = true;
                
                // Link the swing point to the FVG
                swingPoint.ActivatedFVG = true;
                swingPoint.ActivatedFVGLevel = fvg;
                
                // Draw visualization if requested
                if (chart != null && showActivation)
                {
                    chart.DrawActivationRectangle(fvg, swingPoint, "fvg-activation");
                }
                
                return (true, fvg);
            }
        }
        
        return (false, null);
    }
    
    public static (bool success, Level level) FindActivatedLevel(this List<Level> levels, SwingPoint swingPoint)
    {
        if (swingPoint.Direction == Direction.Up)
        {
            var highLevels = levels.Where(l => l.Direction == Direction.Down && l.Mid <= swingPoint.Price && l.Mid > swingPoint.Bar.Open)
                .ToList();

            if (highLevels.Count <= 0) return (false, default);
            
            var first = highLevels.OrderByDescending(o => o.Score).First();

            foreach (var level in highLevels)
            {
                level.PassCount += 1;
                level.Activated = true;
            }
                
            return (true, first);
        }
        else
        {
            var lowLevels = levels.Where(l => l.Direction == Direction.Up && l.Mid > swingPoint.Price && l.Mid < swingPoint.Bar.Open)
                .ToList();

            if (lowLevels.Count <= 0) return (false, default);
            
            var first = lowLevels.OrderByDescending(o => o.Score).First();

            foreach (var level in lowLevels)
            {
                level.PassCount += 1;
                level.Activated = true;
            }
                
            return (true, first);
        }
    }
    
    public static (bool success, Level unicorn) FindUnicorn(this Level breaker, Level cisd, SwingPoint currentSwingPoint, Bars bars, List<Level> levels)
    {
        if (breaker.Direction == Direction.Up)
        {
            if (currentSwingPoint.Index - cisd.IndexLow < 2) return (false, default);
            
            // look for fvg
            for (var i = currentSwingPoint.Index; i >= cisd.IndexLow; i--)
            {
                if (i <= cisd.IndexLow + 1)
                {
                    break;
                }
                    
                var barOne = bars[i];
                var barThree = bars[i - 2];
                if (barOne.Low > barThree.High && barOne.Low > breaker.Low && barOne.Low < breaker.High)
                {
                    breaker.LevelType = LevelType.Unicorn;
                    breaker.Score += 1;
                    breaker.Entry = barOne.Low > cisd.High ? cisd.High : barOne.Low;
                    breaker.StretchTo = currentSwingPoint.Bar.OpenTime;

                    levels.Remove(cisd);

                    return (true, breaker);
                }
            }
        }
        else
        {
            if (currentSwingPoint.Index - cisd.IndexHigh < 2) return (false, default);
            
            // look for fvg
            for (var i = currentSwingPoint.Index; i >= cisd.IndexHigh; i--)
            {
                if (i <= cisd.IndexHigh + 1)
                {
                    break;
                }
                    
                var barOne = bars[i];
                var barThree = bars[i - 2];
                if (barOne.High < barThree.Low && barOne.High < breaker.High && barOne.High > breaker.Low)
                {
                    breaker.LevelType = LevelType.Unicorn;
                    breaker.Score += 1;
                    breaker.Entry = barOne.High > cisd.Low ? cisd.Low : barOne.High;
                    breaker.StretchTo = currentSwingPoint.Bar.OpenTime;

                    levels.Remove(cisd);

                    return (true, breaker);
                }
            }
        }
        
        return (false, default);
    }

    public static (bool success, Level unicorn) FindUnicorn(this Level breaker, Level cisd, Level fvg, List<Level> levels)
    {
        if (breaker.Direction == Direction.Up && fvg.Direction == Direction.Up)
        {
            if (fvg.High < breaker.High || fvg.Low > breaker.Low)
            {
                breaker.LevelType = LevelType.Unicorn;
                breaker.Score += 1;
                breaker.Entry = fvg.High > cisd.High ? cisd.High : fvg.High;
                breaker.StretchTo = fvg.HighTime;

                levels.Remove(cisd);
                
                return (true, breaker);
            }
        }
        else if (breaker.Direction == Direction.Down && fvg.Direction == Direction.Down)
        {
            if (fvg.Low > breaker.Low || fvg.High > breaker.High)
            {
                breaker.LevelType = LevelType.Unicorn;
                breaker.Score += 1;
                breaker.Entry = fvg.Low < cisd.Low ? cisd.Low : fvg.Low;
                breaker.StretchTo = fvg.LowTime;

                levels.Remove(cisd);
                
                return (true, breaker);
            }
        }
        return (false, default);
    }

    public static (bool success, Level breaker) FindPotentialBreaker(this Level orderFlow, Bars bars)
    {
        if (orderFlow.Direction == Direction.Up)
        {
            List<int> bulls = new();
            // find last consecutive bullish candles from Index of High
            for (var i = orderFlow.IndexHigh; i >= orderFlow.IndexLow; i--)
            {
                var bar = bars[i];
                var direction = bar.GetCandleDirection();
                if (direction == Direction.Up && bulls.Count == 0)
                {
                    bulls.Add(i);
                }

                if (bulls.Count != 0 && direction == Direction.Down)
                {
                    break;
                }

                if (direction == Direction.Up)
                {
                    bulls.Add(i);
                }
            }
            
            if (bulls.Count == 0) return (false, default);
            
            var lowIndex = bulls.Min();
            var highIndex = bulls.Max();
            var lowBar = bars[lowIndex];
            var highBar = bars[highIndex];

            var breaker = new Level(LevelType.BreakerBlock, lowBar.Low, highBar.High, lowBar.OpenTime, highBar.OpenTime,
                direction: Direction.Up, index: orderFlow.IndexLow, indexLow: orderFlow.IndexLow, indexHigh: orderFlow.IndexHigh,
                stretchTo: highBar.OpenTime, isConfirmed: false);

            return (true, breaker);
        }
        else
        {
            List<int> bears = new();
            // find last consecutive bearish candles from Index of Low
            for (var i = orderFlow.IndexLow; i >= orderFlow.IndexHigh; i--)
            {
                var bar = bars[i];
                var direction = bar.GetCandleDirection();
                if (direction == Direction.Down && bears.Count == 0)
                {
                    bears.Add(i);
                }

                if (bears.Count != 0 && direction == Direction.Up)
                {
                    break;
                }

                if (direction == Direction.Down)
                {
                    bears.Add(i);
                }
            }
            
            if (bears.Count == 0) return (false, default);
            
            var lowIndex = bears.Max();
            var highIndex = bears.Min();
            var lowBar = bars[lowIndex];
            var highBar = bars[highIndex];

            var breaker = new Level(LevelType.BreakerBlock, lowBar.Low, highBar.High, lowBar.OpenTime, highBar.OpenTime,
                direction: Direction.Down, index: orderFlow.IndexHigh, indexLow: orderFlow.IndexLow, indexHigh: orderFlow.IndexHigh,
                stretchTo: lowBar.OpenTime, isConfirmed: false);

            return (true, breaker);
        }
    }
    
    public static (bool success, Level breaker) FindBreakerBlock(this List<Level> levels, List<SwingPoint> swingPoints, SwingPoint swingPoint)
    {
        // Bullish Breaker
        if (swingPoint.Direction == Direction.Up)
        {
            var points = swingPoints.Where(p => p.SwingType == SwingType.HH && p.Price < swingPoint.Bar.Close && p.Price > swingPoint.Bar.Open &&
                                                p.BrokenCount == 0).ToList();
            foreach (var point in points)
            {
                point.IndexThatBrokeSwing = swingPoint.Index;
                point.BrokenCount += 1;
            }

            if (points.Count <= 0) return (false, default);
            {
                var point = points.OrderByDescending(p => p.Index).First();
                var level = new Level(LevelType.BreakerBlock, point.Bar.Low, point.Bar.High, point.Time, point.Time, direction: point.Direction, index:point.Index, indexHigh: point.Index);
                levels.Add(level);
                return (true, level);
            }
        }

        // Bearish Breaker
        if (swingPoint.Direction != Direction.Down) return (false, default);
        {
            var points = swingPoints.Where(p => p.SwingType == SwingType.LL && p.Price > swingPoint.Bar.Close && p.Price < swingPoint.Bar.Open && p.BrokenCount == 0).ToList();
            foreach (var point in points)
            {
                point.IndexThatBrokeSwing = swingPoint.Index;
                point.BrokenCount += 1;
            }

            if (points.Count <= 0) return (false, default);
            {
                var point = points.OrderByDescending(p => p.Index).First();
                var level = new Level(LevelType.BreakerBlock, point.Bar.Low, point.Bar.High, point.Time, point.Time, direction: point.Direction, index: point.Index, indexLow: point.Index);
                levels.Add(level);
                return (true, level);
            }
        }

    }

    public static (bool chocked, Level choch) CheckChoCh(this Level potentialChoCh, SwingPoint swingPoint)
    {
        if (potentialChoCh == null)
            return (false, null);

        if (swingPoint.Direction == Direction.Up)
        {
            if(potentialChoCh.Direction == Direction.Up)
                return (false, potentialChoCh);

            if (swingPoint.Bar.High > potentialChoCh.High)
                return (true, potentialChoCh);
            // do what you want with the result then clear potentialChoCh
        }
        else
        {
            if (potentialChoCh.Direction == Direction.Down)
                return (false, potentialChoCh);

            if (swingPoint.Bar.Low < potentialChoCh.Low)
                return (true, potentialChoCh);
            // do what you want with the result then clear potentialChoCh
        }
        
        return (false, potentialChoCh);
    }

    public static (bool success, Level potentialChoch) FindPotentialChoCh(this Level potentialChoCh, List<SwingPoint> swingPoints,
        SwingPoint swingPoint)
    {
        var orderedPoints = swingPoints.OrderByDescending(p => p.Index).ToList();
        if (swingPoint.Direction == Direction.Up)
        {
            var low = orderedPoints[1];
            var high = orderedPoints[2];
            var secondLow = orderedPoints[3];

            if (low.Price < secondLow.Price)
            {
                if (potentialChoCh != null && potentialChoCh.High > high.Price)
                    return (true, potentialChoCh);
                
                if (potentialChoCh == null || potentialChoCh.High < high.Price)
                {
                    potentialChoCh = new Level(LevelType.CISD, low.Price, high.Price, low.Time, high.Time,
                        direction: Direction.Down, index: high.Index, indexHigh: high.Index, indexLow: low.Index);
                    return (true, potentialChoCh);
                }
                
                if (potentialChoCh.Index == high.Index)
                    return (true, potentialChoCh);
            }
        }
        else
        {
            var high = orderedPoints[1];
            var low = orderedPoints[2];
            var secondHigh = orderedPoints[3];

            if (high.Price > secondHigh.Price)
            {
                if (potentialChoCh != null && potentialChoCh.Low < low.Price)
                    return (true, potentialChoCh);
                
                if (potentialChoCh == null || potentialChoCh.Low > low.Price)
                {
                    potentialChoCh = new Level(LevelType.CISD, low.Price, high.Price, low.Time, high.Time, 
                        direction: Direction.Up, index: low.Index, indexLow: low.Index, indexHigh: high.Index);
                    return (true, potentialChoCh);
                }

                if (potentialChoCh.Index == low.Index)
                    return (true, potentialChoCh);
            }
        }

        return (true, potentialChoCh);
    }

    public static (bool success, Level orderflow) FindOrderFlow(this List<Level> levels, List<SwingPoint> swingPoints, SwingPoint swingPoint)
    {
        // We are assuming that the most recent swing point has already been added to the list
        // So we skip it and start from the second item in the list
        var orderedPoints = swingPoints.OrderByDescending(p => p.Index).ToList();
        Level orderFlow;
        if (swingPoint.Direction == Direction.Up)
        {
            var low = orderedPoints[1];
            var high = orderedPoints[2];

            orderFlow = levels.FirstOrDefault(l => l.LevelType == LevelType.Orderflow && l.Direction == Direction.Down && l.Index == high.Index);

            if (orderFlow != null) return (true, orderFlow);
            
            orderFlow = new Level(LevelType.Orderflow, low.Price, high.Price, low.Time, high.Time, direction: Direction.Down, index: high.Index, indexLow: low.Index, indexHigh: high.Index, stretchTo:low.Time);
        }
        else
        {
            var low = orderedPoints[2];
            var high = orderedPoints[1];
            
            orderFlow = levels.FirstOrDefault(l => l.LevelType == LevelType.Orderflow && l.Direction == Direction.Up && l.Index == low.Index);

            if (orderFlow != null) return (true, orderFlow);
            
            orderFlow = new Level(LevelType.Orderflow, low.Price, high.Price, low.Time, high.Time, direction: Direction.Up, index: low.Index, indexLow: low.Index, indexHigh: high.Index, stretchTo:high.Time);
        }

        levels.Add(orderFlow);
        return (true, orderFlow);
    }

    public static void FindChangeOfCharacter(this List<StandardDeviation> standardDeviations,
        List<SwingPoint> swingPoints, SwingPoint swingPoint, List<Level> orderBlocks, Chart chart)
    {
        var orderedPoints = swingPoints.OrderByDescending(p => p.Index).ToList();
        if (swingPoint.Direction == Direction.Up)
        {
            var low = orderedPoints[1];
            var high = orderedPoints[2];
            var secondLow = orderedPoints[3];
            
            var isChoCh = swingPoint.Price > high.Price && low.Price < secondLow.Price;
            if (!isChoCh) return;
            
            var stdv = new StandardDeviation(low.Price, high.Price, high.Time);
            var two = orderBlocks.FirstOrDefault(o =>
                !o.Activated && o.Direction == Direction.Down &&
                stdv.MinusTwo < o.High && stdv.MinusTwo > o.Low);
            var four = orderBlocks.FirstOrDefault(o =>
                !o.Activated && o.Direction == Direction.Down &&
                stdv.MinusFour < o.High && stdv.MinusFour > o.Low);

            if (two == null)
                stdv.MinusTwo = 0;
                
            if (four == null)
                stdv.MinusFour = 0;

            if (two == null && four == null) return;
                
            standardDeviations.Add(stdv);
            chart.DrawStandardDeviation(stdv);
        }
        else
        {
            var high = orderedPoints[1];
            var low = orderedPoints[2];
            var secondHigh = orderedPoints[3];
            
            var isChoCH = swingPoint.Price < low.Price && high.Price > secondHigh.Price;
            if (isChoCH)
            {
                var stdv = new StandardDeviation(high.Price, low.Price, low.Time);
                
                var two = orderBlocks.FirstOrDefault(o =>
                    !o.Activated && o.Direction == Direction.Up &&
                    stdv.MinusTwo < o.High && stdv.MinusTwo > o.Low);
                var four = orderBlocks.FirstOrDefault(o =>
                    !o.Activated && o.Direction == Direction.Down &&
                    stdv.MinusFour < o.High && stdv.MinusFour > o.Low);

                if (two == null)
                    stdv.MinusTwo = 0;
                
                if (four == null)
                    stdv.MinusFour = 0;

                if (two == null && four == null) return;
                
                standardDeviations.Add(stdv);
                chart.DrawStandardDeviation(stdv);
            }
        }
    }

    public static (bool success, Level cisd) FindChangeInStateOfDelivery(this List<Level> levels, List<SwingPoint> liquidity, Level breaker, Bars bars, Chart chart, List<int> sweepers, bool showFibs, DateTime? breakTime = null)
    {
        if (breaker.Direction == Direction.Up)
        {
            // find bearish order flow which has current bullish breaker high index as it's index
            var orderFlow = levels.FirstOrDefault(l => l.LevelType == LevelType.Orderflow && l.Direction == Direction.Down && l.Index == breaker.IndexHigh);
            
            if (orderFlow == null) return (false, default);

            // now scan through bearish order flow for first consecutive bearish candles, this is our bullish order block
            List<int> bears = new();
            for (var i = orderFlow.IndexHigh; i <= orderFlow.IndexLow; i++)
            {
                var bar = bars[i];
                var direction = bar.GetCandleDirection();
                if (direction == Direction.Down && bears.Count == 0)
                {
                    bears.Add(i);
                }

                if (bears.Count != 0 && direction == Direction.Up)
                {
                    break;
                }

                if (direction == Direction.Down)
                {
                    bears.Add(i);
                }
            }
            
            if (bears.Count == 0) return (false, default);
            
            var lowIndex = bears.Max();
            var highIndex = bears.Min();
            var lowBar = bars[lowIndex];
            var highBar = bars[highIndex];

            var cisd = new Level(LevelType.CISD, lowBar.Close, highBar.Open, lowBar.OpenTime, highBar.OpenTime,
                direction: Direction.Down, index: orderFlow.IndexHigh, indexLow: orderFlow.IndexLow, indexHigh: highIndex,
                stretchTo: breakTime?? lowBar.OpenTime, entry: highBar.Open);

            var isDiscount = orderFlow.IsInPremiumDiscount(liquidity, bars, chart, showFibs);
            if (isDiscount)
            {
                cisd.Zone = Zone.Discount;
            }
            else
            {
                var sweptSwingLow = sweepers.Any(sw => sw >= orderFlow.IndexLow && sw <= orderFlow.IndexHigh);
                if (sweptSwingLow)
                {
                    cisd.Zone = Zone.Discount;
                }
            }
            
            levels.Add(cisd);

            breaker.IsConfirmed = true;
            breaker.Score += 1;

            return (true, cisd);
            
        }
        else
        {
            // find bullish order flow which has current bearish breaker low index as it's index
            var orderFlow = levels.FirstOrDefault(l => l.LevelType == LevelType.Orderflow && l.Direction == Direction.Up && l.Index == breaker.IndexLow);
            
            if (orderFlow == null) return (false, default);

            // now scan through bullish order flow for first consecutive bullish candles, this is our bearish order block
            List<int> bulls = new();
            for (var i = orderFlow.IndexLow; i <= orderFlow.IndexHigh; i++)
            {
                var bar = bars[i];
                var direction = bar.GetCandleDirection();
                if (direction == Direction.Up && bulls.Count == 0)
                {
                    bulls.Add(i);
                }

                if (bulls.Count != 0 && direction == Direction.Down)
                {
                    break;
                }

                if (direction == Direction.Up)
                {
                    bulls.Add(i);
                }
            }
            
            if (bulls.Count == 0) return (false, default);
            
            var lowIndex = bulls.Min();
            var highIndex = bulls.Max();
            var lowBar = bars[lowIndex];
            var highBar = bars[highIndex];

            var cisd = new Level(LevelType.CISD, lowBar.Open, highBar.Close, lowBar.OpenTime, highBar.OpenTime,
                direction: Direction.Up, index: orderFlow.IndexLow, indexLow: orderFlow.IndexLow, indexHigh: highIndex,
                stretchTo: breakTime ?? highBar.OpenTime, entry: lowBar.Open);
            
            var isPremium = orderFlow.IsInPremiumDiscount(liquidity, bars, chart, showFibs);
            if (isPremium)
            {
                cisd.Zone = Zone.Premium;
            }
            else
            {
                var sweptSwingHigh = sweepers.Any(sw => sw >= orderFlow.IndexLow && sw <= orderFlow.IndexHigh);
                if (sweptSwingHigh)
                {
                    cisd.Zone = Zone.Premium;
                }
            }
            
            levels.Add(cisd);

            breaker.IsConfirmed = true;
            breaker.Score += 1;

            return (true, cisd);
        }
    }

    public static (bool success, List<Level> levels) GetLevelsByTypeAndDirection(this List<Level> levels, LevelType levelType, Direction direction)
    {
        levels = levels.Where(l => l.LevelType == levelType && l.Direction == direction).ToList();

        return levels.Count == 0 ? (false, default) : (true, levels.OrderByDescending(l => l.Index).ToList());
    }

    public static bool IsInPremiumDiscount(this Level orderFlow, List<SwingPoint> externalLiquidity, Bars bars, Chart chart, bool showFibs)
    {
        if (orderFlow.Direction == Direction.Up)
        {
            var nearestHigh = externalLiquidity.Where(l => l.Direction == Direction.Up && l.Index != orderFlow.IndexHigh).OrderByDescending(l => l.Index).First();
            var range = bars.GetMinMax(nearestHigh.Time, orderFlow.HighTime);

            var bar = bars[range.minIndex];
            
            var diff = Math.Abs(nearestHigh.Price - range.min);
            var premium1 = nearestHigh.Price - (diff * 0.236);
            var premium2 = nearestHigh.Price - (diff * 0.114);
            
            if (orderFlow.High > premium1 && orderFlow.Low < premium1)
            {
                if (showFibs)
                {
                    chart.RemoveObject("sell-fib");
                    var fib = chart.DrawFibonacciRetracement($"sell-fib", bar.OpenTime,
                        bar.Low, nearestHigh.Time, nearestHigh.Price, Color.Pink);
                    fib.IsInteractive = true;
                }
                return true;
            }

            if (orderFlow.High > premium2 && orderFlow.Low < premium2)
            {
                if (showFibs)
                {
                    chart.RemoveObject("sell-fib");
                    var fib = chart.DrawFibonacciRetracement($"sell-fib", bar.OpenTime,
                        bar.Low, nearestHigh.Time, nearestHigh.Price, Color.Pink);
                    fib.IsInteractive = true;
                }
                return true;
            }
        }
        else
        {
            var nearestLow = externalLiquidity.Where(l => l.Direction == Direction.Down && l.Index != orderFlow.IndexLow).OrderByDescending(l => l.Index).First();
            var range = bars.GetMinMax(nearestLow.Time, orderFlow.LowTime);

            var bar = bars[range.maxIndex];
            
            var diff = Math.Abs(nearestLow.Price - range.max);
            var discount1 = range.max - (diff * 0.786);
            var discount2 = range.max - (diff * 0.886);
            
            if (orderFlow.Low < discount1 && orderFlow.High > discount1)
            {
                if (showFibs)
                {
                    chart.RemoveObject("buy-fib");
                    var fib = chart.DrawFibonacciRetracement($"buy-fib", nearestLow.Time, nearestLow.Price, bar.OpenTime,
                        bar.High, Color.Green);
                    fib.IsInteractive = true;
                }
                return true;
            }

            if (orderFlow.Low < discount2 && orderFlow.High > discount2)
            {
                if (showFibs)
                {
                    chart.RemoveObject("buy-fib");
                    var fib = chart.DrawFibonacciRetracement($"buy-fib", nearestLow.Time, nearestLow.Price, bar.OpenTime,
                        bar.High, Color.Green);
                    fib.IsInteractive = true;
                }
                return true;
            }
        }
        
        return false;
    }
}