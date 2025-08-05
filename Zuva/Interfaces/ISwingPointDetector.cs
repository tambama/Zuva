using System;
using System.Collections.Generic;
using Zuva.Models;

namespace Zuva.Interfaces
{
    public delegate void LiquiditySweptEventHandler(SwingPoint sweptPoint, int sweepingCandleIndex, Candle sweepingCandle);

    public interface ISwingPointDetector
    {
        event Action<SwingPoint> SwingPointRemoved;
        event LiquiditySweptEventHandler LiquiditySwept;
        
        void ProcessBar(int index, Candle bar);
        List<SwingPoint> GetSwingPointsAtIndex(int index);
        List<SwingPoint> GetAllSwingPoints();
        SwingPoint GetLastSwingHigh();
        SwingPoint GetLastSwingLow();
        void CheckForSweptLiquidity(Candle bar, int index);
        void UpdateSwingPointRelationships();
        void AddSpecialSwingPoint(SwingPoint swingPoint);
        SwingPoint GetSwingPointAtIndex(int index);
    }
}