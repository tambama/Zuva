namespace Zuva.Constants
{
    /// <summary>
    /// Constants for trading calculations and thresholds
    /// </summary>
    public static class TradingConstants
    {
        #region ICT Macro Time Windows (in minutes)
        
        /// <summary>
        /// Duration of each ICT macro time window in minutes
        /// </summary>
        public const int MACRO_TIME_WINDOW_MINUTES = 20;
        
        /// <summary>
        /// Number of ICT macro time periods per day
        /// </summary>
        public const int MACRO_TIME_PERIODS_PER_DAY = 15;

        #endregion

        #region Standard Deviation Levels

        /// <summary>
        /// First standard deviation multiplier
        /// </summary>
        public const double STANDARD_DEVIATION_MINUS_TWO = -2.0;

        /// <summary>
        /// Second standard deviation multiplier
        /// </summary>
        public const double STANDARD_DEVIATION_MINUS_FOUR = -4.0;

        #endregion

        #region Fibonacci Retracement Levels

        /// <summary>
        /// Standard Fibonacci retracement levels
        /// </summary>
        public static readonly double[] FIBONACCI_LEVELS = new[]
        {
            -2.0, -1.618, -1.272, -1.0, -0.786, -0.618, -0.5, -0.382, -0.236,
            0.0, 0.236, 0.382, 0.5, 0.618, 0.786, 1.0, 1.272, 1.618, 2.0, 2.618, 3.0
        };

        /// <summary>
        /// Premium zone threshold (above 50% retracement)
        /// </summary>
        public const double PREMIUM_ZONE_THRESHOLD = 0.5;

        /// <summary>
        /// Discount zone threshold (below 50% retracement)
        /// </summary>
        public const double DISCOUNT_ZONE_THRESHOLD = 0.5;

        /// <summary>
        /// Equilibrium level (50% retracement)
        /// </summary>
        public const double EQUILIBRIUM_LEVEL = 0.5;

        #endregion

        #region Swing Point Detection

        /// <summary>
        /// Minimum number of bars to confirm a swing point
        /// </summary>
        public const int SWING_POINT_CONFIRMATION_BARS = 2;

        /// <summary>
        /// Maximum lookback bars for swing point detection
        /// </summary>
        public const int SWING_POINT_MAX_LOOKBACK = 50;

        #endregion

        #region FVG (Fair Value Gap) Requirements

        /// <summary>
        /// Minimum gap size ratio for FVG validation
        /// </summary>
        public const double FVG_MINIMUM_GAP_RATIO = 0.0001;

        /// <summary>
        /// Number of bars required for FVG formation
        /// </summary>
        public const int FVG_FORMATION_BARS = 3;

        #endregion

        #region Order Block Settings

        /// <summary>
        /// Minimum body size ratio for order block validation
        /// </summary>
        public const double ORDER_BLOCK_MIN_BODY_RATIO = 0.3;

        /// <summary>
        /// Maximum bars lookback for order block detection
        /// </summary>
        public const int ORDER_BLOCK_MAX_LOOKBACK = 20;

        #endregion

        #region Quadrant Levels

        /// <summary>
        /// First quadrant percentage (25%)
        /// </summary>
        public const double QUADRANT_LEVEL_25 = 0.25;

        /// <summary>
        /// Second quadrant percentage (50% - Equilibrium)
        /// </summary>
        public const double QUADRANT_LEVEL_50 = 0.50;

        /// <summary>
        /// Third quadrant percentage (75%)
        /// </summary>
        public const double QUADRANT_LEVEL_75 = 0.75;

        /// <summary>
        /// Fourth quadrant percentage (100%)
        /// </summary>
        public const double QUADRANT_LEVEL_100 = 1.0;

        /// <summary>
        /// Quadrant percentages array
        /// </summary>
        public static readonly double[] QUADRANT_PERCENTAGES = new[] { 0.0, 0.25, 0.50, 0.75, 1.0 };

        #endregion

        #region CISD (Consecutive Inside Structure Down/Up) Settings

        /// <summary>
        /// Default maximum CISD levels per direction
        /// </summary>
        public const int DEFAULT_MAX_CISD_PER_DIRECTION = 2;

        /// <summary>
        /// Maximum allowed CISD levels per direction
        /// </summary>
        public const int MAX_CISD_PER_DIRECTION_LIMIT = 10;

        /// <summary>
        /// Minimum CISD levels per direction
        /// </summary>
        public const int MIN_CISD_PER_DIRECTION = 1;

        #endregion

        #region Session Times (GMT)

        /// <summary>
        /// Asian session start hour (GMT)
        /// </summary>
        public const int ASIAN_SESSION_START_HOUR = 0;

        /// <summary>
        /// Asian session end hour (GMT)
        /// </summary>
        public const int ASIAN_SESSION_END_HOUR = 9;

        /// <summary>
        /// London session start hour (GMT)
        /// </summary>
        public const int LONDON_SESSION_START_HOUR = 8;

        /// <summary>
        /// London session end hour (GMT)
        /// </summary>
        public const int LONDON_SESSION_END_HOUR = 17;

        /// <summary>
        /// New York session start hour (GMT)
        /// </summary>
        public const int NY_SESSION_START_HOUR = 13;

        /// <summary>
        /// New York session end hour (GMT)
        /// </summary>
        public const int NY_SESSION_END_HOUR = 22;

        /// <summary>
        /// Daily reset hour (GMT) - 6 PM New York time
        /// </summary>
        public const int DAILY_RESET_HOUR = 18;

        #endregion

        #region Notification Settings

        /// <summary>
        /// Default notification cooldown in seconds
        /// </summary>
        public const int NOTIFICATION_COOLDOWN_SECONDS = 10;

        /// <summary>
        /// Macro time notification cooldown in seconds (always shorter)
        /// </summary>
        public const int MACRO_NOTIFICATION_COOLDOWN_SECONDS = 5;

        /// <summary>
        /// Maximum notification message length
        /// </summary>
        public const int MAX_NOTIFICATION_MESSAGE_LENGTH = 1000;

        #endregion

        #region Chart Drawing

        /// <summary>
        /// Default line thickness for levels
        /// </summary>
        public const int DEFAULT_LINE_THICKNESS = 1;

        /// <summary>
        /// Thick line thickness for important levels
        /// </summary>
        public const int THICK_LINE_THICKNESS = 2;

        /// <summary>
        /// Very thick line thickness for major levels
        /// </summary>
        public const int VERY_THICK_LINE_THICKNESS = 3;

        /// <summary>
        /// Alpha transparency for filled areas (0-255)
        /// </summary>
        public const int DEFAULT_ALPHA_TRANSPARENCY = 80;

        /// <summary>
        /// Extension minutes for small lines when ShowChoch is false
        /// </summary>
        public const int SMALL_LINE_EXTENSION_MINUTES = 2;

        /// <summary>
        /// Standard deviation sweep line extension minutes
        /// </summary>
        public const int STDDEV_SWEEP_LINE_EXTENSION_MINUTES = 1;

        #endregion

        #region Performance Settings

        /// <summary>
        /// Default maximum swing point history to retain
        /// </summary>
        public const int DEFAULT_MAX_SWING_POINT_HISTORY = 1000;

        /// <summary>
        /// Cleanup interval in bars (how often to clean old data)
        /// </summary>
        public const int CLEANUP_INTERVAL_BARS = 1000;

        /// <summary>
        /// Default cache expiration in minutes
        /// </summary>
        public const int DEFAULT_CACHE_EXPIRATION_MINUTES = 30;

        /// <summary>
        /// Short-term cache expiration in minutes
        /// </summary>
        public const int SHORT_TERM_CACHE_EXPIRATION_MINUTES = 5;

        /// <summary>
        /// Long-term cache expiration in hours
        /// </summary>
        public const int LONG_TERM_CACHE_EXPIRATION_HOURS = 2;

        #endregion

        #region UTC Offset Limits

        /// <summary>
        /// Minimum UTC offset in hours
        /// </summary>
        public const int MIN_UTC_OFFSET = -12;

        /// <summary>
        /// Maximum UTC offset in hours
        /// </summary>
        public const int MAX_UTC_OFFSET = 14;

        /// <summary>
        /// Default UTC offset for New York trading
        /// </summary>
        public const int DEFAULT_UTC_OFFSET_NY = -4;

        #endregion

        #region Calculation Precision

        /// <summary>
        /// Default decimal places for price calculations
        /// </summary>
        public const int PRICE_DECIMAL_PLACES = 5;

        /// <summary>
        /// Minimum price difference to consider significant
        /// </summary>
        public const double MIN_SIGNIFICANT_PRICE_DIFFERENCE = 0.00001;

        /// <summary>
        /// Rounding precision for price comparisons
        /// </summary>
        public const double PRICE_COMPARISON_PRECISION = 0.0000001;

        #endregion

        #region Liquidity Sweep Settings

        /// <summary>
        /// Minimum volume ratio for significant liquidity sweep
        /// </summary>
        public const double MIN_LIQUIDITY_SWEEP_VOLUME_RATIO = 1.5;

        /// <summary>
        /// Maximum bars to look back for liquidity sweep validation
        /// </summary>
        public const int LIQUIDITY_SWEEP_MAX_LOOKBACK = 100;

        #endregion

        #region String Formatting

        /// <summary>
        /// Maximum logger name length for display
        /// </summary>
        public const int MAX_LOGGER_NAME_DISPLAY_LENGTH = 20;

        /// <summary>
        /// Maximum stack trace lines to display in logs
        /// </summary>
        public const int MAX_STACK_TRACE_LINES = 3;

        /// <summary>
        /// Configuration key separator
        /// </summary>
        public const string CONFIG_KEY_SEPARATOR = ":";

        /// <summary>
        /// Environment variable prefix
        /// </summary>
        public const string ENV_VAR_PREFIX = "ZUVA_";

        /// <summary>
        /// Encrypted value prefix
        /// </summary>
        public const string ENCRYPTED_VALUE_PREFIX = "enc:";

        #endregion
    }
}