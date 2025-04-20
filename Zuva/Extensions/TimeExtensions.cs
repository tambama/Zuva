using System;
using System.Collections.Generic;
using System.Linq;
using Zuva.Models;

namespace Zuva.Extensions;

public static class TimeExtensions
{
    public static DateTime StartOfDay(this DateTime date)
    {
        return new DateTime(date.Year, date.Month, date.Day, 00, 00, 00);
    }
    public static DateTime EndOfDay(this DateTime date)
    {
        return new DateTime(date.Year, date.Month, date.Day, 23, 59, 59);
    }
    public static bool InsideTimeRange(this List<TimeRange> cycles, TimeSpan time)
    {
        return cycles.Any(m => time >= m.StartTime && time <= m.EndTime);
    }
    public static bool IsStartOrEndTime(this List<TimeRange> range, TimeSpan time)
    {
        return range.Any(m => m.StartTime == time || m.EndTime == time);
    }
}