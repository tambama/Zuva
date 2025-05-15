using cAlgo.API;
using Zuva.Models;
using System.Collections.Generic;
using System;
using Zuva.Extensions;

namespace Zuva.Services
{
    /// <summary>
    /// Manages time-related functionality, including ICT macro time periods, 
    /// daily high/low levels, and session high/low levels
    /// </summary>
    public class TimeManager
    {
        private readonly Chart _chart;
        private readonly List<TimeRange> _macros;
        private readonly bool _showMacros;
        private readonly int _utcOffset;
        private readonly Bars _bars;
        private readonly SwingPointDetector _swingPointDetector;

        // Keep track of which macro times we've already drawn lines for to avoid duplication
        private readonly HashSet<string> _drawnMacroTimes = new HashSet<string>();

        // Daily level tracking
        private DateTime _lastDayStartTime = DateTime.MinValue;
        private bool _processingDailyLevels = false;

        // Session trackers
        private readonly Dictionary<SessionType, SessionTracker> _sessionTrackers;
        private SessionType _lastProcessedSession = SessionType.None;

        // Dictionary to map hour to session
        private readonly Dictionary<int, SessionType> _hourToSessionMap;

        /// <summary>
        /// Creates a new instance of the TimeManager
        /// </summary>
        public TimeManager(
            Chart chart,
            Bars bars,
            SwingPointDetector swingPointDetector,
            bool showMacros = true,
            int utcOffset = -4)
        {
            _chart = chart;
            _showMacros = showMacros;
            _utcOffset = utcOffset;
            _macros = InitializeMacros();
            _bars = bars;
            _swingPointDetector = swingPointDetector;

            _swingPointDetector.LiquiditySwept += OnLiquiditySwept;

            // Initialize session trackers
            _sessionTrackers = new Dictionary<SessionType, SessionTracker>();
            foreach (SessionType sessionType in Enum.GetValues(typeof(SessionType)))
            {
                if (sessionType != SessionType.None)
                {
                    _sessionTrackers[sessionType] = new SessionTracker();
                }
            }

            // Initialize hour to session map based on provided session times
            _hourToSessionMap = new Dictionary<int, SessionType>
            {
                // Asia: 18:00 - 23:59
                { 18, SessionType.Asia },
                { 19, SessionType.Asia },
                { 20, SessionType.Asia },
                { 21, SessionType.Asia },
                { 22, SessionType.Asia },
                { 23, SessionType.Asia },

                // London Pre: 00:00 - 00:59
                { 0, SessionType.LondonPre },

                // London: 01:00 - 04:59
                { 1, SessionType.London },
                { 2, SessionType.London },
                { 3, SessionType.London },
                { 4, SessionType.London },

                // London Lunch: 05:00 - 06:59
                { 5, SessionType.LondonLunch },
                { 6, SessionType.LondonLunch },

                // New York Pre: 07:00 - 09:29
                { 7, SessionType.NewYorkPre },
                { 8, SessionType.NewYorkPre },
                { 9, SessionType.NewYorkPre }, // Note: This will be refined by minute check

                // New York AM: 09:30 - 11:29
                // (9 is handled in GetCurrentSession with minute check)
                { 10, SessionType.NewYorkAM },
                { 11, SessionType.NewYorkAM }, // Note: This will be refined by minute check

                // New York Lunch: 11:30 - 13:29
                // (11 is handled in GetCurrentSession with minute check)
                { 12, SessionType.NewYorkLunch },
                { 13, SessionType.NewYorkLunch }, // Note: This will be refined by minute check

                // New York PM Pre: 13:30 - 14:29
                // (13 is handled in GetCurrentSession with minute check)
                { 14, SessionType.NewYorkPMPre }, // Note: This will be refined by minute check

                // New York PM: 14:30 - 17:59
                // (14 is handled in GetCurrentSession with minute check)
                { 15, SessionType.NewYorkPM },
                { 16, SessionType.NewYorkPM },
                { 17, SessionType.NewYorkPM }
            };
        }

        // Add the event handler method
        private void OnLiquiditySwept(SwingPoint sweptPoint, int sweepingCandleIndex, Candle sweepingCandle)
        {
            if (_chart == null)
                return;

            // Get the label based on the liquidity name
            string label = GetLiquidityLabel(sweptPoint);
            if (string.IsNullOrEmpty(label))
                return;

            // Create unique IDs for the original and extended lines
            string originalLineId = $"{label.ToLower()}-{sweptPoint.Time.Ticks}";
            string extendedLineId = 
                $"{label.ToLower()}-extended-{sweptPoint.Time.Ticks}-{sweepingCandle.Time.Ticks}";

            // Remove the original line
            _chart.RemoveObject(originalLineId);

            // Draw the extended line
            _chart.DrawStraightLine(
                extendedLineId,
                sweptPoint.Time,
                sweptPoint.Price,
                sweepingCandle.Time,
                sweptPoint.Price,
                label,  // Use the same label when extending
                LineStyle.Solid,
                Color.FromArgb(75, Color.Wheat),
                true, // Show label
                true, // Remove existing
                labelOnRight:true
            );
        }

// Helper method to get a label from liquidity type
        private string GetLiquidityLabel(SwingPoint sweptPoint)
        {
            // Use the LiquidityName directly as it's already the short code we want
            return sweptPoint.LiquidityName.ToString();
        }

        /// <summary>
        /// Initializes the list of ICT macro time ranges
        /// </summary>
        public static List<TimeRange> InitializeMacros()
        {
            var macros = new List<TimeRange>
            {
                new TimeRange(new TimeSpan(1, 50, 0), new TimeSpan(2, 10, 0)),
                new TimeRange(new TimeSpan(2, 50, 0), new TimeSpan(3, 10, 0)),
                new TimeRange(new TimeSpan(3, 50, 0), new TimeSpan(4, 10, 0)),
                new TimeRange(new TimeSpan(4, 50, 0), new TimeSpan(5, 10, 0)),
                new TimeRange(new TimeSpan(5, 50, 0), new TimeSpan(6, 10, 0)),
                new TimeRange(new TimeSpan(6, 50, 0), new TimeSpan(7, 10, 0)),
                new TimeRange(new TimeSpan(7, 50, 0), new TimeSpan(8, 10, 0)),
                new TimeRange(new TimeSpan(8, 50, 0), new TimeSpan(9, 10, 0)),
                new TimeRange(new TimeSpan(9, 50, 0), new TimeSpan(10, 10, 0)),
                new TimeRange(new TimeSpan(10, 50, 0), new TimeSpan(11, 10, 0)),
                new TimeRange(new TimeSpan(11, 10, 0), new TimeSpan(12, 10, 0)),
                new TimeRange(new TimeSpan(12, 50, 0), new TimeSpan(13, 10, 0)),
                new TimeRange(new TimeSpan(13, 50, 0), new TimeSpan(14, 10, 0)),
                new TimeRange(new TimeSpan(14, 50, 0), new TimeSpan(15, 10, 0)),
                new TimeRange(new TimeSpan(15, 45, 0), new TimeSpan(16, 00, 0))
            };

            return macros;
        }

        /// <summary>
        /// Process a bar to check if it matches the start time of any macro, session boundary, or daily boundary
        /// </summary>
        public void ProcessBar(int index, DateTime time)
        {
            if (_chart == null || _bars == null || index >= _bars.Count)
                return;

            // Adjust time for UTC offset to get the market time
            DateTime marketTime = time.AddHours(_utcOffset);
            DateTime dateOnly = marketTime.Date;
            TimeSpan timeOfDay = marketTime.TimeOfDay;

            // Process macro times if enabled
            if (_showMacros)
            {
                // Check if this bar's market time matches any macro start time
                foreach (var macro in _macros)
                {
                    // Create a unique identifier for this macro on this date
                    string macroKey = $"{dateOnly:yyyyMMdd}-{macro.StartTime.Hours:D2}{macro.StartTime.Minutes:D2}";

                    // See if this bar's market time is close to a macro start time (within 1 minute)
                    bool closeToMacroStart = Math.Abs((timeOfDay - macro.StartTime).TotalMinutes) < 1;

                    // If this is a macro start time and we haven't drawn it yet
                    if (closeToMacroStart && !_drawnMacroTimes.Contains(macroKey))
                    {
                        // Draw both the start and end lines for this macro
                        DrawMacroLine(time, "start");

                        // Calculate the end time in chart time
                        TimeSpan macroDuration = macro.EndTime - macro.StartTime;
                        DateTime endTime = time.Add(macroDuration);

                        DrawMacroLine(endTime, "end");

                        // Mark this macro as drawn
                        _drawnMacroTimes.Add(macroKey);
                    }
                }
            }

            // Handle daily boundaries (18:00)
            if (marketTime.Hour == 18 && marketTime.Minute == 0)
            {
                ProcessDailyLevels(index);
            }

            // Session tracking
            SessionType currentBarSession = GetCurrentSession(marketTime);

            // Handle session transitions
            if (_lastProcessedSession != currentBarSession && _lastProcessedSession != SessionType.None)
            {
                // We've transitioned to a new session, process the previous one
                ProcessSessionLevels(index, _lastProcessedSession);
            }

            // Update session high/low tracking
            if (currentBarSession != SessionType.None)
            {
                UpdateSessionTracker(currentBarSession, index, _bars[index].High, _bars[index].Low);
            }

            // Store the current session for the next bar
            _lastProcessedSession = currentBarSession;
        }

        /// <summary>
        /// Draws a vertical line on the chart for a macro time
        /// </summary>
        private void DrawMacroLine(DateTime time, string lineType)
        {
            if (_chart == null)
                return;

            // Create a unique ID for this macro line, including whether it's a start or end line
            string id = $"macro-{lineType}-{time.Ticks}";

            // Draw a dotted light gray vertical line
            _chart.DrawVerticalLine(id, time, Color.Gray, 1, LineStyle.DotsRare);
        }

        /// <summary>
        /// Determines the current trading session based on market time
        /// </summary>
        private SessionType GetCurrentSession(DateTime marketTime)
        {
            int hour = marketTime.Hour;
            int minute = marketTime.Minute;

            // Handle special cases with minute checks
            if (hour == 9)
            {
                return minute < 30 ? SessionType.NewYorkPre : SessionType.NewYorkAM;
            }
            else if (hour == 11)
            {
                return minute < 30 ? SessionType.NewYorkAM : SessionType.NewYorkLunch;
            }
            else if (hour == 13)
            {
                return minute < 30 ? SessionType.NewYorkLunch : SessionType.NewYorkPMPre;
            }
            else if (hour == 14)
            {
                return minute < 30 ? SessionType.NewYorkPMPre : SessionType.NewYorkPM;
            }

            // Use the hour mapping for standard hours
            if (_hourToSessionMap.TryGetValue(hour, out SessionType session))
            {
                return session;
            }

            // Default case
            return SessionType.None;
        }

        /// <summary>
        /// Updates session high/low tracking for the current bar
        /// </summary>
        private void UpdateSessionTracker(SessionType session, int index, double high, double low)
        {
            // Skip if not a valid session
            if (session == SessionType.None)
                return;

            // Get tracker for this session
            var tracker = _sessionTrackers[session];

            // If this is the first bar of this session, store the start time
            if (tracker.StartTime == DateTime.MinValue)
                tracker.StartTime = _bars[index].OpenTime;

            // Always update the end time to the current bar
            tracker.EndTime = _bars[index].OpenTime;

            // Update high/low tracking
            if (high > tracker.High)
            {
                tracker.High = high;
                tracker.HighIndex = index;
                tracker.HighTime = _bars[index].OpenTime;
            }

            if (low < tracker.Low)
            {
                tracker.Low = low;
                tracker.LowIndex = index;
                tracker.LowTime = _bars[index].OpenTime;
            }
        }

        /// <summary>
        /// Processes daily high/low levels at the 18:00 boundary
        /// </summary>
        private void ProcessDailyLevels(int currentIndex)
        {
            // Avoid reentrance
            if (_processingDailyLevels)
                return;

            _processingDailyLevels = true;

            try
            {
                // If this is not the first day we're processing
                if (_lastDayStartTime != DateTime.MinValue)
                {
                    // Calculate day boundaries
                    DateTime dayStart = _lastDayStartTime;
                    DateTime dayEnd = _bars[currentIndex].OpenTime;

                    // Find min/max prices for the previous day
                    var minMax = _bars.GetMinMax(dayStart, dayEnd);

                    // Create candles for high and low
                    var highCandle = new Candle(_bars[minMax.maxIndex], minMax.maxIndex);
                    var lowCandle = new Candle(_bars[minMax.minIndex], minMax.minIndex);

                    // Check if there are already swing points at these indices and update them instead of creating new ones
                    CreateOrUpdateSpecialSwingPoint(
                        minMax.maxIndex,
                        minMax.max,
                        minMax.maxTime,
                        highCandle,
                        SwingType.H,
                        LiquidityType.PDH,
                        Direction.Up,
                        "PDH",
                        dayEnd);

                    CreateOrUpdateSpecialSwingPoint(
                        minMax.minIndex,
                        minMax.min,
                        minMax.minTime,
                        lowCandle,
                        SwingType.L,
                        LiquidityType.PDL,
                        Direction.Down,
                        "PDL",
                        dayEnd);
                }

                // Update the last day start time
                _lastDayStartTime = _bars[currentIndex].OpenTime;
            }
            finally
            {
                _processingDailyLevels = false;
            }
        }

        /// <summary>
        /// Processes session high/low levels when transitioning between sessions
        /// </summary>
        private void ProcessSessionLevels(int currentIndex, SessionType session)
        {
            // Get the tracker for this session
            var tracker = _sessionTrackers[session];

            // Skip if we don't have valid session data
            if (tracker.HighIndex < 0 || tracker.LowIndex < 0)
                return;

            // Create candles for session high and low
            var highCandle = new Candle(_bars[tracker.HighIndex], tracker.HighIndex);
            var lowCandle = new Candle(_bars[tracker.LowIndex], tracker.LowIndex);

            // Map session type to LiquidityType
            LiquidityType highLiquidityType;
            LiquidityType lowLiquidityType;
            string highLabel;
            string lowLabel;

            // Set appropriate liquidity types and labels based on session
            switch (session)
            {
                case SessionType.Asia:
                    highLiquidityType = LiquidityType.PSH;
                    lowLiquidityType = LiquidityType.PSL;
                    highLabel = "AH"; // Asian Session High
                    lowLabel = "AL"; // Asian Session Low
                    break;
                case SessionType.LondonPre:
                    highLiquidityType = LiquidityType.PSH;
                    lowLiquidityType = LiquidityType.PSL;
                    highLabel = "LPH"; // London Pre-Session High
                    lowLabel = "LPL"; // London Pre-Session Low
                    break;
                case SessionType.London:
                    highLiquidityType = LiquidityType.PSH;
                    lowLiquidityType = LiquidityType.PSL;
                    highLabel = "LH"; // London Session High
                    lowLabel = "LL"; // London Session Low
                    break;
                case SessionType.LondonLunch:
                    highLiquidityType = LiquidityType.PSH;
                    lowLiquidityType = LiquidityType.PSL;
                    highLabel = "LLH"; // London Lunch Session High
                    lowLabel = "LLL"; // London Lunch Session Low
                    break;
                case SessionType.NewYorkPre:
                    highLiquidityType = LiquidityType.PSH;
                    lowLiquidityType = LiquidityType.PSL;
                    highLabel = "NYPH"; // NY Pre-Session High
                    lowLabel = "NYPL"; // NY Pre-Session Low
                    break;
                case SessionType.NewYorkAM:
                    highLiquidityType = LiquidityType.PSH;
                    lowLiquidityType = LiquidityType.PSL;
                    highLabel = "NYAMH"; // NY AM Session High
                    lowLabel = "NYAML"; // NY AM Session Low
                    break;
                case SessionType.NewYorkLunch:
                    highLiquidityType = LiquidityType.PSH;
                    lowLiquidityType = LiquidityType.PSL;
                    highLabel = "NYLH"; // NY Lunch Session High
                    lowLabel = "NYLL"; // NY Lunch Session Low
                    break;
                case SessionType.NewYorkPMPre:
                    highLiquidityType = LiquidityType.PSH;
                    lowLiquidityType = LiquidityType.PSL;
                    highLabel = "NYPPH"; // NY PM Pre-Session High
                    lowLabel = "NYPPL"; // NY PM Pre-Session Low
                    break;
                case SessionType.NewYorkPM:
                    highLiquidityType = LiquidityType.PSH;
                    lowLiquidityType = LiquidityType.PSL;
                    highLabel = "NYPMH"; // NY PM Session High
                    lowLabel = "NYPML"; // NY PM Session Low
                    break;
                default:
                    highLiquidityType = LiquidityType.PSH;
                    lowLiquidityType = LiquidityType.PSL;
                    highLabel = "SH"; // Generic Session High
                    lowLabel = "SL"; // Generic Session Low
                    break;
            }

            // Check if there are already swing points at these indices and update them instead of creating new ones
            CreateOrUpdateSpecialSwingPoint(
                tracker.HighIndex,
                tracker.High,
                tracker.HighTime,
                highCandle,
                SwingType.HH,
                highLiquidityType,
                Direction.Up,
                highLabel,
                tracker.EndTime);

            CreateOrUpdateSpecialSwingPoint(
                tracker.LowIndex,
                tracker.Low,
                tracker.LowTime,
                lowCandle,
                SwingType.LL,
                lowLiquidityType,
                Direction.Down,
                lowLabel,
                tracker.EndTime);

            // Reset the tracker for the next occurrence of this session
            _sessionTrackers[session] = new SessionTracker();
        }

        /// <summary>
        /// Creates a new swing point or updates an existing one at the same index
        /// </summary>
        private void CreateOrUpdateSpecialSwingPoint(
    int index,
    double price,
    DateTime time,
    Candle candle,
    SwingType swingType,
    LiquidityType liquidityType,
    Direction direction,
    string label,
    DateTime endTime)
{
    if (_swingPointDetector == null)
        return;

    // Convert string label to LiquidityName enum
    if (!Enum.TryParse(label, out LiquidityName liquidityName))
        liquidityName = LiquidityName.N; // Default to Normal if parse fails

    // Check if a swing point already exists at this index
    var existingPoint = _swingPointDetector.GetSwingPointAtIndex(index);

    if (existingPoint != null)
    {
        // If we're applying a PDH or PDL label and the existing point is not already a daily marker,
        // then we need to clean up any existing session labels
        if ((liquidityType == LiquidityType.PDH || liquidityType == LiquidityType.PDL) &&
            existingPoint.LiquidityType != LiquidityType.PDH &&
            existingPoint.LiquidityType != LiquidityType.PDL)
        {
            // Remove existing session lines and labels before updating to daily
            RemoveExistingSessionLabels(time, price);
        }

        // Update the existing swing point with the new liquidity type and name
        existingPoint.LiquidityType = liquidityType;
        existingPoint.LiquidityName = liquidityName;
    }
    else
    {
        // Create a new swing point with both liquidity type and name
        var swingPoint = new SwingPoint(
            index,
            price,
            time,
            candle,
            swingType,
            liquidityType,
            direction,
            liquidityName  // Pass the liquidity name to the constructor
        );

        // Add to the swing detector
        _swingPointDetector.AddSpecialSwingPoint(swingPoint);
    }

    // Draw the level line regardless of whether we created or updated the swing point
    if (_chart != null)
    {
        string id = $"{label.ToLower()}-{time.Ticks}";

        _chart.DrawStraightLine(
            id,
            time,
            price,
            endTime,
            price,
            label,
            LineStyle.Solid,
            Color.Wheat,
            true, // Show label
            true // Remove existing
        );
    }
}

// Helper method to remove all possible session labels at a given time and price
        private void RemoveExistingSessionLabels(DateTime time, double price)
        {
            if (_chart == null)
                return;

            // Common session label prefixes used in your code
            string[] sessionPrefixes =
            {
                "ah", "al", "lh", "ll", "lph", "lpl", "llh", "lll",
                "nyph", "nypl", "nyamh", "nyaml", "nylh", "nyll",
                "nypph", "nyppl", "nypmh", "nypml", "sh", "sl"
            };

            // Remove each possible session label
            foreach (var prefix in sessionPrefixes)
            {
                string lineId = $"{prefix}-{time.Ticks}";
                string labelId = $"{lineId}-label";

                // Remove both the line and its label
                _chart.RemoveObject(lineId);
                _chart.RemoveObject(labelId);
            }
        }

        /// <summary>
        /// Checks if a given time falls within any macro time period
        /// </summary>
        public bool IsInMacroTime(DateTime time)
        {
            // Adjust time for UTC offset to get the market time
            DateTime marketTime = time.AddHours(_utcOffset);
            TimeSpan timeOfDay = marketTime.TimeOfDay;

            foreach (var macro in _macros)
            {
                if (timeOfDay >= macro.StartTime && timeOfDay <= macro.EndTime)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets all macro time ranges
        /// </summary>
        public List<TimeRange> GetMacros()
        {
            return _macros;
        }
    }
}