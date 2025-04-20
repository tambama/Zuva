using cAlgo.API;
using Zuva.Models;

namespace Zuva.Extensions;

public static class StructureExtensions
{
    public static void UpdateBias(this Chart chart, Direction bias)
    {
        chart.RemoveObject("bias");
        var text = bias == Direction.Down ? "Bearish" : "Bullish";
        chart.DrawStaticText("bias", text, VerticalAlignment.Bottom, HorizontalAlignment.Left, Color.Wheat);
    }
}