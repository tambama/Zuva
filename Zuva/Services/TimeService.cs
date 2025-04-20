using cAlgo.API;
using cAlgo.API.Internals;
using Zuva.Extensions;
using Zuva.Models;

namespace Zuva.Services;

public class TimeService
{
    public void GetOpeningRangeGap(MarketData marketData, Chart chart, Bar _currentBar)
    {
        var bars = marketData.GetBars(TimeFrame.Minute);
                var settlementIndex = bars.OpenTimes.GetIndexByTime(_currentBar.OpenTime.Date.AddDays(-1)
                    .AddHours(16)
                    .AddMinutes(14));
                var settlementBar = bars[settlementIndex];
                
                if (settlementIndex <= -1) return;
                
                var index = bars.OpenTimes.GetIndexByTime(_currentBar.OpenTime);
                var bar = bars[index];
                var high = bar.Open > settlementBar.Close ? bar.Open : settlementBar.Close;
                var low = bar.Open < settlementBar.Close ? bar.Open : settlementBar.Close;
                var ce = (high + low) / 2;
                var direction = bar.Open < settlementBar.Close ? Direction.Up : Direction.Down;
                var highIndex = bar.Open < settlementBar.Close ? settlementIndex : index;
                var lowIndex = bar.Open > settlementBar.Close ? index : settlementIndex;
                var highBar = bars[highIndex];
                var lowBar = bars[lowIndex];

                var tomorrow = _currentBar.OpenTime.Date.AddDays(1).Date;

                chart.DrawStraightLine("org-high", _currentBar.OpenTime, high, tomorrow, high, "ORG High",
                    color: Color.Aqua, hasLabel: true);

                chart.DrawStraightLine("org-ce", _currentBar.OpenTime, ce, tomorrow, ce, "ORG C.E",
                    color: Color.Aqua, hasLabel: true, lineStyle: LineStyle.Dots);
                    
                chart.DrawStraightLine("org-low", _currentBar.OpenTime, low, tomorrow, low, "ORG Low",
                    color: Color.Aqua, hasLabel: true);
    }
}