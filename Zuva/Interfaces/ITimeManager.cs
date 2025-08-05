using System;
using System.Collections.Generic;
using Zuva.Models;

namespace Zuva.Interfaces
{
    public interface ITimeManager
    {
        void ProcessBar(int index, DateTime time);
        void CheckFibonacciSweep(SwingPoint swingPoint);
        bool IsInMacroTime(DateTime time);
        List<TimeRange> GetMacros();
    }
}