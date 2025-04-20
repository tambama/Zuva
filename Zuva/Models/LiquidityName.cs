using System.ComponentModel;

namespace Zuva.Models;

public enum LiquidityName
{
    [Description("Normal")] N,
    [Description("Asian High")] AH,
    [Description("Asian Low")] AL,
    [Description("London High")] LH,
    [Description("London Low")] LL,
    [Description("AM High")] NAH,
    [Description("AM Low")] NAL,
    [Description("PM High")] NPH,
    [Description("PM Low")] NPL,
    [Description("PD High")] PDH,
    [Description("PD Low")] PDL
}