using System.ComponentModel;

namespace Zuva.Models;

public enum SwingType
{
    [Description("High")] H,
    [Description("Low")] L,
    [Description("Higher High")] HH,
    [Description("Lower High")] LH,
    [Description("Higher Low")] HL,
    [Description("Lower Low")] LL
}