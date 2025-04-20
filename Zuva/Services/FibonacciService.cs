using System;
using System.Collections.Generic;
using Zuva.Models;

namespace Zuva.Services;

public class FibonacciService
{
    public List<Level> MarkFibRetracementLevels(SwingPoint highPoint, SwingPoint lowPoint)
    {
        var levels = new List<Level>();
        
        var diff = Math.Abs(highPoint.Price - lowPoint.Price);
        var premium = new Level(LevelType.Premium, highPoint.Price - (diff * 0.8860), highPoint.Price, highPoint.Time, lowPoint.Time);
        var discount = new Level(LevelType.Discount, lowPoint.Price, highPoint.Price - (diff * 0.1140), highPoint.Time, lowPoint.Time);
        levels.Add(premium);
        levels.Add(discount);
        
        return levels;
    }
}