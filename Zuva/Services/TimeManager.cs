using cAlgo.API;
using Mwenje.Extensions;
using Zuva.Models;
using System.Collections.Generic;
using System;

namespace Zuva.Services
{
    /// <summary>
    /// Manages time-related functionality, including ICT macro time periods
    /// </summary>
    public class TimeManager
    {
        private readonly Chart _chart;
        private readonly List<TimeRange> _macros;
        private readonly bool _showMacros;
        
        // Keep track of which macro times we've already drawn lines for to avoid duplication
        private readonly HashSet<string> _drawnMacroTimes = new HashSet<string>();
        
        /// <summary>
        /// Creates a new instance of the TimeManager
        /// </summary>
        public TimeManager(Chart chart, bool showMacros = true)
        {
            _chart = chart;
            _showMacros = showMacros;
            _macros = InitializeMacros();
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
                new TimeRange(new TimeSpan(15, 50, 0), new TimeSpan(16, 10, 0))
            };

            return macros;
        }
        
        /// <summary>
        /// Process a bar to check if it matches the start time of any macro
        /// </summary>
        public void ProcessBar(DateTime time)
        {
            if (!_showMacros || _chart == null)
                return;
            
            DateTime dateOnly = time.Date;
            TimeSpan timeOfDay = time.TimeOfDay;
            
            // Check if this bar's time matches any macro start time
            foreach (var macro in _macros)
            {
                // Create the full datetime for this macro's start time on the current date
                DateTime macroStartTime = dateOnly.Add(macro.StartTime);
                DateTime macroEndTime = dateOnly.Add(macro.EndTime);
                
                // Create a unique identifier for this macro on this date
                string macroKey = $"{dateOnly.ToString("yyyyMMdd")}-{macro.StartTime.Hours:D2}{macro.StartTime.Minutes:D2}";
                
                // See if this bar's time is close to a macro start time (within 1 minute)
                bool closeToMacroStart = Math.Abs((timeOfDay - macro.StartTime).TotalMinutes) < 1;
                
                // If this is a macro start time and we haven't drawn it yet
                if (closeToMacroStart && !_drawnMacroTimes.Contains(macroKey))
                {
                    // Draw both the start and end lines for this macro
                    DrawMacroLine(macroStartTime, "start");
                    DrawMacroLine(macroEndTime, "end");
                    
                    // Mark this macro as drawn
                    _drawnMacroTimes.Add(macroKey);
                }
            }
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
        /// Checks if a given time falls within any macro time period
        /// </summary>
        public bool IsInMacroTime(DateTime time)
        {
            TimeSpan timeOfDay = time.TimeOfDay;
            
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
        
        /// <summary>
        /// Updates the visibility of macro lines on the chart
        /// </summary>
        public void UpdateMacroVisibility(bool showMacros)
        {
            if (_chart == null)
                return;
                
            if (!showMacros)
            {
                // Remove all macro lines that we've drawn
                foreach (var macroKey in _drawnMacroTimes)
                {
                    // Parse the date and time from the key
                    string dateStr = macroKey.Substring(0, 8);
                    string timeStr = macroKey.Substring(9, 4);
                    
                    if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime date)
                        && int.TryParse(timeStr.Substring(0, 2), out int hours)
                        && int.TryParse(timeStr.Substring(2, 2), out int minutes))
                    {
                        // Recreate the start and end times
                        DateTime startTime = date.AddHours(hours).AddMinutes(minutes);
                        
                        // Find the matching macro
                        var matchingMacro = _macros.Find(m => 
                            m.StartTime.Hours == hours && 
                            m.StartTime.Minutes == minutes);
                        
                        if (matchingMacro != null)
                        {
                            DateTime endTime = date.Add(matchingMacro.EndTime);
                            
                            // Remove the lines
                            string startId = $"macro-start-{startTime.Ticks}";
                            string endId = $"macro-end-{endTime.Ticks}";
                            
                            _chart.RemoveObject(startId);
                            _chart.RemoveObject(endId);
                        }
                    }
                }
                
                // Clear the tracking collection
                _drawnMacroTimes.Clear();
            }
            // If turning visibility on, we'll draw the lines as bars are processed
        }
    }
}