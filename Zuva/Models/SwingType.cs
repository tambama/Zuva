using System.ComponentModel;

namespace Zuva.Models;

public enum SwingType
{
    [Description("High")] H,
    [Description("Low")] L,
    [Description("Higher High")] HH,
    [Description("Higher Low")] HL,
    [Description("Lower High")] LH,
    [Description("Lower Low")] LL,
    [Description("Inducement")] IND
}