using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using Zuva.Configuration;
using Zuva.Interfaces;
using Zuva.Models;

namespace Zuva.Services
{
    public class ServiceCoordinator
    {
        private readonly IndicatorConfiguration _config;
        private readonly ISwingPointDetector _swingDetector;
        private readonly IMarketStructureAnalyzer _marketStructureAnalyzer;
        private readonly IPdArrayAnalyzer _pdArrayAnalyzer;
        private readonly ITimeManager _timeManager;
        private readonly INotificationService _notificationService;
        private readonly IMarketDataProvider _marketDataProvider;
        private readonly Action<string> _logger;

        private bool _marketStructureInitialized = false;
        private bool _pdArrayAnalyzerInitialized = false;

        public ServiceCoordinator(
            IndicatorConfiguration config,
            ISwingPointDetector swingDetector,
            IMarketStructureAnalyzer marketStructureAnalyzer,
            IPdArrayAnalyzer pdArrayAnalyzer,
            ITimeManager timeManager,
            INotificationService notificationService,
            IMarketDataProvider marketDataProvider,
            Action<string> logger)
        {
            _config = config;
            _swingDetector = swingDetector;
            _marketStructureAnalyzer = marketStructureAnalyzer;
            _pdArrayAnalyzer = pdArrayAnalyzer;
            _timeManager = timeManager;
            _notificationService = notificationService;
            _marketDataProvider = marketDataProvider;
            _logger = logger ?? (_ => { });

            WireUpEvents();
            InitializeSMT();
        }

        private void WireUpEvents()
        {
            if (_swingDetector != null)
            {
                _swingDetector.SwingPointRemoved += OnSwingPointRemoved;
            }
        }

        private void InitializeSMT()
        {
            if (_config.ShowSMT && !string.IsNullOrEmpty(_config.SMTPair))
            {
                if (_marketDataProvider.InitializePairSymbol(_config.SMTPair))
                {
                    _pdArrayAnalyzer.PairDataProvider = _marketDataProvider.GetPairPrice;
                }
            }
        }

        public void ProcessBar(int index, Bar currentBar, Bar previousBar, int previousBarIndex, Bars bars)
        {
            if (index <= 1) return;

            try
            {
                ProcessTimeManager(index, currentBar.OpenTime);
                ProcessCisdActivation(previousBar, previousBarIndex);
                ProcessFVGDetection(bars, previousBarIndex);
                ProcessSwingPoints(previousBar, previousBarIndex);
            }
            catch (Exception ex)
            {
                _logger($"Error in ServiceCoordinator.ProcessBar: {ex.Message}");
            }
        }

        private void ProcessTimeManager(int index, DateTime time)
        {
            if (_timeManager != null)
            {
                try
                {
                    _timeManager.ProcessBar(index, time);
                }
                catch (Exception ex)
                {
                    _logger($"Error in macro time processing: {ex.Message}");
                }
            }
        }

        private void ProcessCisdActivation(Bar previousBar, int previousBarIndex)
        {
            if (_pdArrayAnalyzer != null)
            {
                try
                {
                    _pdArrayAnalyzer.CheckCisdActivationOnBar(previousBar, previousBarIndex);
                }
                catch (Exception ex)
                {
                    _logger($"Error in CISD activation check: {ex.Message}");
                }
            }
        }

        private void ProcessFVGDetection(Bars bars, int previousBarIndex)
        {
            if (_pdArrayAnalyzer != null)
            {
                try
                {
                    _pdArrayAnalyzer.DetectFVG(bars, previousBarIndex);
                }
                catch (Exception ex)
                {
                    _logger($"Error in FVG/OrderBlock detection: {ex.Message}");
                }
            }
        }

        private void ProcessSwingPoints(Bar previousBar, int previousBarIndex)
        {
            if (!_config.ShowSwingPoints || _swingDetector == null)
                return;

            try
            {
                var candle = new Candle(previousBar, previousBarIndex);
                _swingDetector.ProcessBar(previousBarIndex, candle);

                var swingPointsAtIndex = _swingDetector.GetSwingPointsAtIndex(previousBarIndex);
                if (swingPointsAtIndex.Count > 0)
                {
                    var sortedSwingPoints = swingPointsAtIndex.OrderBy(s => s.Number);
                    foreach (var swingPoint in sortedSwingPoints)
                    {
                        ProcessNewSwingPoint(swingPoint);
                    }
                }

                UpdateSwingPointRelationships();
            }
            catch (Exception ex)
            {
                _logger($"Error in swing point processing: {ex.Message}");
            }
        }

        private void ProcessNewSwingPoint(SwingPoint swingPoint)
        {
            ProcessMarketStructure(swingPoint);
            ProcessLiquiditySweep(swingPoint);
            ProcessFibonacciSweep(swingPoint);
            ProcessPdArrayAnalysis(swingPoint);
        }

        private void ProcessMarketStructure(SwingPoint swingPoint)
        {
            if (!_config.ShowMarketStructure || _marketStructureAnalyzer == null)
                return;

            var allSwingPoints = _swingDetector.GetAllSwingPoints();

            if (!_marketStructureInitialized && allSwingPoints.Count >= 2)
            {
                _marketStructureAnalyzer.Initialize(allSwingPoints);
                _marketStructureInitialized = true;
            }
            else if (_marketStructureInitialized)
            {
                _marketStructureAnalyzer.ProcessSwingPoint(swingPoint);
            }
        }

        private void ProcessLiquiditySweep(SwingPoint swingPoint)
        {
            if (swingPoint.Bar != null && _swingDetector != null)
            {
                _swingDetector.CheckForSweptLiquidity(swingPoint.Bar, swingPoint.Index);
            }
        }

        private void ProcessFibonacciSweep(SwingPoint swingPoint)
        {
            if (_timeManager != null)
            {
                _timeManager.CheckFibonacciSweep(swingPoint);
            }
        }

        private void ProcessPdArrayAnalysis(SwingPoint swingPoint)
        {
            if (_pdArrayAnalyzer == null)
                return;

            var allSwingPoints = _swingDetector.GetAllSwingPoints();

            if (!_pdArrayAnalyzerInitialized && allSwingPoints.Count >= 2)
            {
                _pdArrayAnalyzer.Initialize(allSwingPoints);
                _pdArrayAnalyzerInitialized = true;
            }
            else if (_pdArrayAnalyzerInitialized)
            {
                _pdArrayAnalyzer.ProcessSwingPoint(swingPoint);
            }
        }

        private void UpdateSwingPointRelationships()
        {
            _swingDetector?.UpdateSwingPointRelationships();
        }

        private void OnSwingPointRemoved(SwingPoint removedPoint)
        {
            if (!_pdArrayAnalyzerInitialized || _pdArrayAnalyzer == null)
                return;

            try
            {
                _pdArrayAnalyzer.RemoveSwingPoint(removedPoint);
            }
            catch (Exception ex)
            {
                _logger($"Error handling swing point removal: {ex.Message}");
            }
        }

        // Expose service data through coordinator
        public List<SwingPoint> GetAllSwingPoints() => _swingDetector?.GetAllSwingPoints() ?? new List<SwingPoint>();
        public SwingPoint GetLastSwingHigh() => _swingDetector?.GetLastSwingHigh();
        public SwingPoint GetLastSwingLow() => _swingDetector?.GetLastSwingLow();
        public Direction GetMarketBias() => _marketStructureAnalyzer?.GetBias() ?? Direction.Up;
        public List<SwingPoint> GetExternalLiquidityPoints() => _marketStructureAnalyzer?.GetExternalLiquidityPoints() ?? new List<SwingPoint>();
        public List<Level> GetPdArrays() => _pdArrayAnalyzer?.GetPdArrays() ?? new List<Level>();
        public List<Level> GetBullishPdArrays() => _pdArrayAnalyzer?.GetBullishPdArrays() ?? new List<Level>();
        public List<Level> GetBearishPdArrays() => _pdArrayAnalyzer?.GetBearishPdArrays() ?? new List<Level>();
        public List<Level> GetAllFVGs() => _pdArrayAnalyzer?.GetAllFVGs() ?? new List<Level>();
        public List<Level> GetAllOrderBlocks() => _pdArrayAnalyzer?.GetAllOrderBlocks() ?? new List<Level>();
        public bool IsInMacroTime(DateTime time) => _timeManager?.IsInMacroTime(time) ?? false;
        public List<TimeRange> GetMacros() => _timeManager?.GetMacros() ?? new List<TimeRange>();
    }
}