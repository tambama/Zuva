using System;
using System.Collections.Generic;
using Zuva.Models;

namespace Zuva.Helpers;

public class TimeHelpers
{
    public static List<TimeRange> InitializeCycles()
    {
        var cycles = new List<TimeRange>
        {
            new TimeRange(new TimeSpan(23, 30, 0), new TimeSpan(1, 00, 0)),
            new TimeRange(new TimeSpan(1, 00, 0), new TimeSpan(2, 30, 0)),
            new TimeRange(new TimeSpan(2, 30, 0), new TimeSpan(4, 00, 0)),
            new TimeRange(new TimeSpan(4, 0, 0), new TimeSpan(5, 30, 0)),
            new TimeRange(new TimeSpan(5, 30, 0), new TimeSpan(7, 0, 0)),
            new TimeRange(new TimeSpan(7, 0, 0), new TimeSpan(8, 30, 0)),
            new TimeRange(new TimeSpan(8, 30, 0), new TimeSpan(10, 0, 0)),
            new TimeRange(new TimeSpan(10, 0, 0), new TimeSpan(11, 30, 0)),
            new TimeRange(new TimeSpan(10, 0, 0), new TimeSpan(11, 30, 0)),
            new TimeRange(new TimeSpan(11, 30, 0), new TimeSpan(13, 00, 0)),
            new TimeRange(new TimeSpan(13, 00, 0), new TimeSpan(14, 30, 0)),
            new TimeRange(new TimeSpan(14, 30, 0), new TimeSpan(16, 00, 0)),
            new TimeRange(new TimeSpan(16, 00, 0), new TimeSpan(17, 30, 0)),
            new TimeRange(new TimeSpan(17, 30, 0), new TimeSpan(19, 00, 0)),
            new TimeRange(new TimeSpan(19, 00, 0), new TimeSpan(20, 30, 0)),
            new TimeRange(new TimeSpan(20, 30, 0), new TimeSpan(22, 00, 0)),
            new TimeRange(new TimeSpan(22, 00, 0), new TimeSpan(23, 30, 0))
        };

        return cycles;
    }
    
    public static List<TimeRange> InitializeMacros()
    {
        var macros = new List<TimeRange>
        {
            new TimeRange(new TimeSpan(1, 50, 0), new TimeSpan(2, 10, 0)),
            new TimeRange(new TimeSpan(2, 50, 0), new TimeSpan(3, 10, 0)),
            new TimeRange(new TimeSpan(3, 50, 0), new TimeSpan(4, 10, 0)),
            new TimeRange(new TimeSpan(4, 50, 0), new TimeSpan(5, 10, 0)),
            new TimeRange(new TimeSpan(5, 50, 0), new TimeSpan(6, 10, 0)),
            new TimeRange(new TimeSpan(6, 50, 0), new TimeSpan(7, 10, 0)),
            new TimeRange(new TimeSpan(7, 50, 0), new TimeSpan(8, 10, 0)),
            new TimeRange(new TimeSpan(8, 50, 0), new TimeSpan(9, 10, 0)),
            new TimeRange(new TimeSpan(9, 50, 0), new TimeSpan(10, 10, 0)),
            new TimeRange(new TimeSpan(10, 50, 0), new TimeSpan(11, 10, 0)),
            new TimeRange(new TimeSpan(11, 10, 0), new TimeSpan(12, 10, 0)),
            new TimeRange(new TimeSpan(12, 50, 0), new TimeSpan(13, 10, 0)),
            new TimeRange(new TimeSpan(13, 50, 0), new TimeSpan(14, 10, 0)),
            new TimeRange(new TimeSpan(14, 50, 0), new TimeSpan(15, 10, 0)),
            new TimeRange(new TimeSpan(15, 50, 0), new TimeSpan(16, 10, 0))
        };

        return macros;
    }
}