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
        
        // Keep track of which days we've already drawn lines for to avoid duplication
        private readonly HashSet<DateTime> _drawnDates = new HashSet<DateTime>();
        
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
        /// Process a bar to check for macro time periods and draw vertical lines if needed
        /// </summary>
        public void ProcessBar(DateTime time)
        {
            if (!_showMacros)
                return;
                
            // Only process each date once
            DateTime dateOnly = time.Date;
            if (_drawnDates.Contains(dateOnly))
                return;
                
            // Draw lines for each macro time for this date
            foreach (var macro in _macros)
            {
                // Create DateTime for the start and end of the macro time on this date
                DateTime macroStartTime = dateOnly.Add(macro.StartTime);
                DateTime macroEndTime = dateOnly.Add(macro.EndTime);
                
                // Draw vertical lines at both the start and end of the macro time
                DrawMacroLine(macroStartTime, "start");
                DrawMacroLine(macroEndTime, "end");
            }
            
            // Mark this date as processed
            _drawnDates.Add(dateOnly);
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
                // Remove all macro lines
                foreach (var date in _drawnDates)
                {
                    foreach (var macro in _macros)
                    {
                        // Remove both start and end lines
                        DateTime macroStartTime = date.Add(macro.StartTime);
                        DateTime macroEndTime = date.Add(macro.EndTime);
                        
                        string startId = $"macro-start-{macroStartTime.Ticks}";
                        string endId = $"macro-end-{macroEndTime.Ticks}";
                        
                        _chart.RemoveObject(startId);
                        _chart.RemoveObject(endId);
                    }
                }
                
                // Clear the tracked dates
                _drawnDates.Clear();
            }
            else
            {
                // Redraw all lines (will be handled in the next ProcessBar calls)
                _drawnDates.Clear();
            }
        }
    }
}