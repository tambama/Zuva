using cAlgo.API;
using Zuva.Models;

namespace Mwenje.Extensions;

public static class EnumExtensions
{
    public static (Color color, LineStyle style) GetLineStyle(this LineType lineType)
    {
        return lineType switch
        {
            LineType.IND => (Color.Wheat, LineStyle.Dots),
            LineType.BOS => (Color.Wheat, LineStyle.Dots),
            LineType.CHOCH => (Color.Red, LineStyle.Solid),
            LineType.CISD => (Color.Pink, LineStyle.Solid),
            LineType.Unicorn => (Color.Red, LineStyle.Solid),
            LineType.OF => (Color.Aqua, LineStyle.Dots),
            _ => (Color.Gray, LineStyle.Solid)
        };
    }
}