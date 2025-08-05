namespace Zuva.Constants
{
    /// <summary>
    /// Error codes for different types of exceptions
    /// </summary>
    public static class ErrorCodes
    {
        #region Trading Calculation Errors
        
        public const string SWING_CALC_ERROR = "SWING_CALC_ERROR";
        public const string FVG_DETECTION_ERROR = "FVG_DETECTION_ERROR";
        public const string MARKET_STRUCTURE_ERROR = "MARKET_STRUCTURE_ERROR";
        public const string STDDEV_ERROR = "STDDEV_ERROR";
        public const string LEVEL_CREATION_ERROR = "LEVEL_CREATION_ERROR";
        public const string LIQUIDITY_SWEEP_ERROR = "LIQUIDITY_SWEEP_ERROR";

        #endregion

        #region Configuration Errors

        public const string CONFIG_ERROR = "CONFIG_ERROR";
        public const string PROFILE_ERROR = "PROFILE_ERROR";
        public const string SECURE_CONFIG_ERROR = "SECURE_CONFIG_ERROR";
        public const string NOTIFICATION_CONFIG_ERROR = "NOTIFICATION_CONFIG_ERROR";

        #endregion

        #region Data Management Errors

        public const string REPOSITORY_ERROR = "REPOSITORY_ERROR";
        public const string CACHE_ERROR = "CACHE_ERROR";
        public const string DATA_VALIDATION_ERROR = "DATA_VALIDATION_ERROR";
        public const string DATA_CONSISTENCY_ERROR = "DATA_CONSISTENCY_ERROR";

        #endregion

        #region Service Errors

        public const string SERVICE_INITIALIZATION_ERROR = "SERVICE_INIT_ERROR";
        public const string DEPENDENCY_INJECTION_ERROR = "DI_ERROR";
        public const string CHART_DRAWING_ERROR = "CHART_DRAW_ERROR";
        public const string NOTIFICATION_ERROR = "NOTIFICATION_ERROR";

        #endregion

        #region External Service Errors

        public const string TELEGRAM_ERROR = "TELEGRAM_ERROR";
        public const string SYMBOL_DATA_ERROR = "SYMBOL_DATA_ERROR";
        public const string TIME_ZONE_ERROR = "TIME_ZONE_ERROR";

        #endregion
    }
}