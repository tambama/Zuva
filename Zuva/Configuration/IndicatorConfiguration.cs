namespace Zuva.Configuration
{
    public class IndicatorConfiguration
    {
        public bool ShowSwingPoints { get; set; } = true;
        public int UtcOffset { get; set; } = -4;
        public bool ShowMacros { get; set; } = true;
        public bool MacroFilter { get; set; } = false;
        public bool ShowMarketStructure { get; set; } = true;
        public bool ShowStructure { get; set; } = true;
        public bool ShowChoch { get; set; } = true;
        public bool ShowCISD { get; set; } = false;
        public int MaxCisdsPerDirection { get; set; } = 2;
        public bool ShowOrderFlow { get; set; } = false;
        public bool ShowFVG { get; set; } = true;
        public bool ShowOrderBlock { get; set; } = true;
        public bool ShowRejectionBlock { get; set; } = false;
        public bool ShowBreakerBlock { get; set; } = false;
        public bool ShowUnicorn { get; set; } = true;
        public bool ShowGauntlet { get; set; } = false;
        public bool ShowQuadrants { get; set; } = false;
        public bool ShowInsideKeyLevel { get; set; } = false;
        public bool ShowLiquiditySweep { get; set; } = true;
        public bool ShowStdv { get; set; } = true;
        public bool ShowFibonacciLevels { get; set; } = false;
        public bool ShowSMT { get; set; } = false;
        public string SMTPair { get; set; } = "";
        public bool EnableLog { get; set; } = false;
        public bool EnableTelegram { get; set; } = false;
        public string TelegramChatId { get; set; } = "5631623580";
        public string TelegramToken { get; set; } = "7507336625:AAHM4oYlg_5XIjzzCNFCR_oyLu1Y69qkvns";
    }
}