using System;
using System.Collections.Generic;
using cAlgo.API;
using Zuva.Models;

namespace Zuva.Interfaces
{
    public delegate double PairDataProviderDelegate(string pairSymbol, DateTime time, int index, Direction direction);

    public interface IPdArrayAnalyzer
    {
        PairDataProviderDelegate PairDataProvider { get; set; }
        
        void Initialize(List<SwingPoint> swingPoints);
        void ProcessSwingPoint(SwingPoint swingPoint);
        void DetectFVG(Bars bars, int index);
        void CheckCisdActivationOnBar(Bar bar, int index);
        void RemoveSwingPoint(SwingPoint removedPoint);
        
        List<Level> GetPdArrays();
        List<Level> GetBullishPdArrays();
        List<Level> GetBearishPdArrays();
        Level GetLastBullishPdArray();
        Level GetLastBearishPdArray();
        
        List<Level> GetAllFVGs();
        List<Level> GetBullishFVGs();
        List<Level> GetBearishFVGs();
        
        List<Level> GetAllOrderBlocks();
        List<Level> GetBullishOrderBlocks();
        List<Level> GetBearishOrderBlocks();
        
        List<Level> GetLiquiditySweepLevels();
        List<Level> GetGauntlets();
        List<Level> GetGauntlets(Direction direction);
        
        List<Level> GetAllCISDLevels();
        List<Level> GetActiveCISDLevels();
        List<Level> GetConfirmedCISDLevels();
        
        List<Level> GetAllBreakerBlocks();
        List<Level> GetBullishBreakerBlocks();
        List<Level> GetBearishBreakerBlocks();
        
        List<Level> GetAllUnicorns();
        List<Level> GetUnicorns(Direction direction);
        Level GetLastUnicorn(Direction direction);
        
        List<Level> GetActivePdArrays();
        List<Level> GetActivePdArrays(Direction direction);
    }
}