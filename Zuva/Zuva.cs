using cAlgo.API;
using Zuva.Services;
using Zuva.Models;
using System.Collections.Generic;
using Zuva.Extensions;
using System;
using Mwenje.Extensions;

namespace Zuva
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Zuva : Indicator
    {
        [Parameter("Show Swing Points", DefaultValue = true)]
        public bool ShowSwingPoints { get; set; }

        [Parameter("Show HTF Swing Points", DefaultValue = false)]
        public bool ShowHtfSwingPoints { get; set; }

        [Parameter("HTF", DefaultValue = "H1")]
        public string HTF { get; set; }

        [Parameter("Show Market Structure", Group = "Market Structure", DefaultValue = true)]
        public bool ShowMarketStructure { get; set; }

        [Parameter("Show Structure", Group = "Market Structure", DefaultValue = true)]
        public bool ShowStructure { get; set; }

        [Parameter("Show CHOCH", Group = "Market Structure", DefaultValue = true)]
        public bool ShowChoch { get; set; }

        [Parameter("Show Order Flow", Group = "Order Flow", DefaultValue = false)]
        public bool ShowOrderFlow { get; set; }

        [Output("Swing High", Color = Colors.White, PlotType = PlotType.Points, Thickness = 1)]
        public IndicatorDataSeries SwingHighs { get; set; }

        [Output("Swing Low", Color = Colors.White, PlotType = PlotType.Points, Thickness = 1)]
        public IndicatorDataSeries SwingLows { get; set; }

        [Output("HTF Swing High", Color = Colors.Green, PlotType = PlotType.Points, Thickness = 8)]
        public IndicatorDataSeries HtfSwingHighs { get; set; }

        [Output("HTF Swing Low", Color = Colors.Red, PlotType = PlotType.Points, Thickness = 8)]
        public IndicatorDataSeries HtfSwingLows { get; set; }

        // Market Structure Series
        [Output("Higher High", Color = Colors.Pink, PlotType = PlotType.Points, Thickness = 12)]
        public IndicatorDataSeries HigherHighs { get; set; }

        [Output("Lower High", Color = Colors.Pink, PlotType = PlotType.Points, Thickness = 12)]
        public IndicatorDataSeries LowerHighs { get; set; }

        [Output("Higher Low", Color = Colors.Pink, PlotType = PlotType.Points, Thickness = 12)]
        public IndicatorDataSeries HigherLows { get; set; }

        [Output("Lower Low", Color = Colors.Pink, PlotType = PlotType.Points, Thickness = 12)]
        public IndicatorDataSeries LowerLows { get; set; }

        private SwingPointDetector _swingDetector;
        private List<SwingPoint> _swingPoints;
        private SwingPointDetector _htfSwingDetector;
        private List<SwingPoint> _htfSwingPoints;

        private TimeFrame _highTimeFrame;

        // Keep track of processed HTF bars to avoid duplicate processing
        private readonly Dictionary<DateTime, bool> _processedHtfBars = new Dictionary<DateTime, bool>();

        private Bar _currentBar;
        private int _currentBarIndex;
        
        private Bar _previousBar;
        private int _previousBarIndex;

        // Market structure analyzer
        private MarketStructureAnalyzer _marketStructureAnalyzer;

        // PD Array analyzer
        private PdArrayAnalyzer _pdArrayAnalyzer;

        // Flag to track if we have enough data for market structure analysis
        private bool _marketStructureInitialized = false;

        // Flag to track if PD Array analyzer is initialized
        private bool _pdArrayAnalyzerInitialized = false;

        protected override void Initialize()
        {
            // Initialize the swing detector
            _swingPoints = new List<SwingPoint>();
            _htfSwingPoints = new List<SwingPoint>();

            _swingDetector = new SwingPointDetector(SwingHighs, SwingLows);
            _htfSwingDetector = new SwingPointDetector(HtfSwingHighs, HtfSwingLows);

            _highTimeFrame = HTF.GetTimeFrameFromString();

            // Initialize PD Array analyzer
            try
            {
                _pdArrayAnalyzer = new PdArrayAnalyzer(Chart, ShowOrderFlow);
            }
            catch (Exception ex)
            {
                Print("Error initializing PD Array Analyzer: " + ex.Message);
                // Disable to prevent further errors
                ShowOrderFlow = false;
            }

            // Initialize market structure analyzer if enabled
            try
            {
                _marketStructureAnalyzer = new MarketStructureAnalyzer(
                    Chart,
                    SwingHighs,
                    SwingLows,
                    HigherHighs,
                    LowerHighs,
                    LowerLows,
                    HigherLows,
                    ShowStructure,
                    ShowChoch,
                    message => Print(message) // Pass Print method as logger
                );
            }
            catch (Exception ex)
            {
                Print("Error initializing Market Structure Analyzer: " + ex.Message);
                // Disable to prevent further errors
                ShowMarketStructure = false;
            }
        }

        public override void Calculate(int index)
        {
            // Need at least 2 bars to calculate
            if (index <= 1)
                return;

            _currentBar = Bars[index];
            _currentBarIndex = index;
            _previousBar = Bars[index - 1];
            _previousBarIndex = index - 1;

            if (ShowSwingPoints)
            {
                try
                {
                    // Create a new candle object from the previous bar
                    var candle = new Candle(_previousBar, _previousBarIndex);

                    // Pass the current bar properties to the regular swing detector
                    _swingDetector.ProcessBar(_previousBarIndex, candle);

                    // Get any newly identified swing point
                    var swingPointsAtIndex = _swingDetector.GetSwingPointsAtIndex(_previousBarIndex);
                    if (swingPointsAtIndex.Count > 0)
                    {
                        swingPointsAtIndex.OrderBy(s => s.Number);

                        // Process each swing point in the sorted order
                        foreach (var swingPoint in swingPointsAtIndex)
                        {
                            if (_currentBar.OpenTime == new DateTime(2025, 5, 8, 18, 39, 00))
                            {
                                Chart.DrawVerticalLine("test", _currentBar.OpenTime, Color.Red, 1, LineStyle.Dots);
                                Print($"Swing Point {swingPoint.Number}, Price: {swingPoint.Price}. Direction: {swingPoint.Direction}");
                            }
                            
                            // Process for market structure if enabled
                            if (ShowMarketStructure && _marketStructureAnalyzer != null)
                            {
                                // If we have enough swing points, initialize the market structure analyzer
                                var allSwingPoints = _swingDetector.GetAllSwingPoints();

                                if (!_marketStructureInitialized && allSwingPoints.Count >= 2)
                                {
                                    _marketStructureAnalyzer.Initialize(allSwingPoints);
                                    _marketStructureInitialized = true;
                                }
                                else if (_marketStructureInitialized)
                                {
                                    // Process the new swing point for market structure analysis
                                    _marketStructureAnalyzer.ProcessSwingPoint(swingPoint);
                                }
                            }

                            // Process for PD Array analysis
                            if (_pdArrayAnalyzer != null)
                            {
                                // If we have enough swing points, initialize the PD Array analyzer
                                var allSwingPoints = _swingDetector.GetAllSwingPoints();

                                if (!_pdArrayAnalyzerInitialized && allSwingPoints.Count >= 2)
                                {
                                    _pdArrayAnalyzer.Initialize(allSwingPoints);
                                    _pdArrayAnalyzerInitialized = true;
                                }
                                else if (_pdArrayAnalyzerInitialized)
                                {
                                    // Process the new swing point for PD Array analysis
                                    _pdArrayAnalyzer.ProcessSwingPoint(swingPoint);
                                }
                            }
                        }
                    }

                    // Update the relationships between swing points
                    if (index == Bars.Count - 1) // Only on the last bar for efficiency
                    {
                        _swingDetector.UpdateSwingPointRelationships();
                        _swingPoints = _swingDetector.GetAllSwingPoints();
                    }
                }
                catch (Exception ex)
                {
                    Print("Error in swing point processing: " + ex.Message);
                }
            }

            // High Timeframe Processing
            if (ShowHtfSwingPoints && _currentBar.OpenTime.IsStartOfHigherTimeframeBar(_highTimeFrame))
            {
                try
                {
                    // Get the previous HTF bar indices
                    var (startIndex, endIndex) =
                        Bars.GetPreviousHigherTimeframeBarRange(_currentBarIndex, _highTimeFrame);

                    if (startIndex >= 0 && endIndex >= 0)
                    {
                        // Create the HTF candle from the range of bars
                        var htfCandle = Bars.GetHigherTimeframeCandle(startIndex, endIndex);

                        // Check if we've already processed this HTF bar
                        if (htfCandle != null && !_processedHtfBars.ContainsKey(htfCandle.Time))
                        {
                            // Process the HTF candle using our specialized HTF swing point detector
                            _htfSwingDetector.ProcessHighTimeframeBar(htfCandle);

                            // Mark this HTF bar as processed
                            _processedHtfBars[htfCandle.Time] = true;

                            // Update HTF swing point relationships at the end of all calculations
                            if (index == Bars.Count - 1)
                            {
                                _htfSwingDetector.UpdateSwingPointRelationships();
                                _htfSwingPoints = _htfSwingDetector.GetAllSwingPoints();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Print("Error in HTF processing: " + ex.Message);
                }
            }
        }

        // Methods to expose swing points to other components
        public List<SwingPoint> GetAllSwingPoints()
        {
            return _swingPoints ?? new List<SwingPoint>();
        }

        public List<SwingPoint> GetAllHtfSwingPoints()
        {
            return _htfSwingPoints ?? new List<SwingPoint>();
        }

        public SwingPoint GetLastSwingHigh()
        {
            return _swingDetector?.GetLastSwingHigh();
        }

        public SwingPoint GetLastSwingLow()
        {
            return _swingDetector?.GetLastSwingLow();
        }

        public SwingPoint GetLastHtfSwingHigh()
        {
            return _htfSwingDetector?.GetLastSwingHigh();
        }

        public SwingPoint GetLastHtfSwingLow()
        {
            return _htfSwingDetector?.GetLastSwingLow();
        }

        // Get market structure information
        public Direction GetMarketBias()
        {
            return (ShowMarketStructure && _marketStructureAnalyzer != null)
                ? _marketStructureAnalyzer.GetBias()
                : Direction.Up;
        }

        public List<SwingPoint> GetExternalLiquidityPoints()
        {
            return (ShowMarketStructure && _marketStructureAnalyzer != null)
                ? _marketStructureAnalyzer.GetExternalLiquidityPoints()
                : new List<SwingPoint>();
        }

        // Get order flow information
        public List<Level> GetPdArrays()
        {
            return _pdArrayAnalyzer?.GetPdArrays() ?? new List<Level>();
        }

        public List<Level> GetBullishPdArrays()
        {
            return _pdArrayAnalyzer?.GetBullishPdArrays() ?? new List<Level>();
        }

        public List<Level> GetBearishPdArrays()
        {
            return _pdArrayAnalyzer?.GetBearishPdArrays() ?? new List<Level>();
        }

        public Level GetLastBullishPdArray()
        {
            return _pdArrayAnalyzer?.GetLastBullishPdArray();
        }

        public Level GetLastBearishPdArray()
        {
            return _pdArrayAnalyzer?.GetLastBearishPdArray();
        }
    }
}