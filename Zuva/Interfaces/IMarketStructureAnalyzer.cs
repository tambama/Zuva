using System.Collections.Generic;
using Zuva.Models;

namespace Zuva.Interfaces
{
    public interface IMarketStructureAnalyzer
    {
        void Initialize(List<SwingPoint> swingPoints);
        void ProcessSwingPoint(SwingPoint swingPoint);
        Direction GetBias();
        List<SwingPoint> GetExternalLiquidityPoints();
    }
}