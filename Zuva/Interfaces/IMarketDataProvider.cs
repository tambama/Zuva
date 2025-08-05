using System;
using Zuva.Models;

namespace Zuva.Interfaces
{
    public interface IMarketDataProvider
    {
        double GetPairPrice(string pairSymbol, DateTime time, int index, Direction direction);
        bool InitializePairSymbol(string smtPair);
    }
}