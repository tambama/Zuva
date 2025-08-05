using cAlgo.API;
using Zuva.Services;
using Zuva.Models;
using System.Collections.Generic;
using Zuva.Extensions;
using System;
using System.Linq;
using cAlgo.API.Internals;
using Zuva.Configuration;
using Zuva.Factories;
using Zuva.Interfaces;

namespace Zuva
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Zuva : Indicator
    {
        private IndicatorConfiguration _config;
        private ServiceCoordinator _serviceCoordinator;
        private ISwingPointDetector _swingDetector;
        private IMarketStructureAnalyzer _marketStructureAnalyzer;
        private IPdArrayAnalyzer _pdArrayAnalyzer;
        private ITimeManager _timeManager;
        private INotificationService _notificationService;
        private IMarketDataProvider _marketDataProvider;
        private readonly List<StandardDeviation> _standardDeviations = new List<StandardDeviation>();
        [Parameter("Swing Points", DefaultValue = true)]
        public bool ShowSwingPoints { get; set; }


        [Parameter("UTC Offset", Group = "Time Management", DefaultValue = -4)]
        public int UtcOffset { get; set; }

        [Parameter("Macro Times", Group = "Time Management", DefaultValue = true)]
        public bool ShowMacros { get; set; }
        [Parameter("Macro Filter", Group = "Time Management", DefaultValue = false)]
        public bool MacroFilter { get; set; }

        [Parameter("Market Structure", Group = "Market Structure", DefaultValue = true)]
        public bool ShowMarketStructure { get; set; }

        [Parameter("Structure", Group = "Market Structure", DefaultValue = true)]
        public bool ShowStructure { get; set; }

        [Parameter("CHOCH", Group = "Market Structure", DefaultValue = true)]
        public bool ShowChoch { get; set; }

        [Parameter("CISD", Group = "Market Structure", DefaultValue = false)]
        public bool ShowCISD { get; set; }

        [Parameter("Max CISD", Group = "Market Structure", DefaultValue = 2)]
        public int MaxCisdsPerDirection { get; set; }

        [Parameter("Order Flow", Group = "PD Arrays", DefaultValue = false)]
        public bool ShowOrderFlow { get; set; }

        [Parameter("Fair Value Gaps", Group = "PD Arrays", DefaultValue = true)]
        public bool ShowFVG { get; set; }

        [Parameter("Order Blocks", Group = "PD Arrays", DefaultValue = true)]
        public bool ShowOrderBlock { get; set; }

        [Parameter("Rejection Blocks", Group = "PD Arrays", DefaultValue = false)]
        public bool ShowRejectionBlock { get; set; }
        [Parameter("Breaker Blocks", Group = "PD Arrays", DefaultValue = false)]
        public bool ShowBreakerBlock { get; set; }

        [Parameter("Unicorn", Group = "PD Arrays", DefaultValue = true)]
        public bool ShowUnicorn { get; set; }

        [Parameter("Gauntlets", Group = "PD Arrays", DefaultValue = false)]
        public bool ShowGauntlet { get; set; }

        [Parameter("Quadrants", Group = "PD Arrays", DefaultValue = false)]
        public bool ShowQuadrants { get; set; }

        [Parameter("Inside Key Level", Group = "PD Arrays", DefaultValue = false)]
        public bool ShowInsideKeyLevel { get; set; }

        [Parameter("Liquidity Sweeps", Group = "Liquidity", DefaultValue = true)]
        public bool ShowLiquiditySweep { get; set; }

        [Parameter("STDV", Group = "Liquidity", DefaultValue = true)]
        public bool ShowStdv { get; set; }

        [Parameter("Session Fib", Group = "Liquidity", DefaultValue = false)]
        public bool ShowFibonacciLevels { get; set; }

        [Parameter("SMT", Group = "SMT", DefaultValue = false)]
        public bool ShowSMT { get; set; }

        [Parameter("Pair", Group = "SMT", DefaultValue = "")]
        public string SMTPair { get; set; }
        [Parameter("Enable Log", Group = "Notifications", DefaultValue = false)]
        public bool EnableLog { get; set; }
        
        [Parameter("Enable Telegram", Group = "Notifications", DefaultValue = false)]
        public bool EnableTelegram { get; set; }

        [Output("Swing High", Color = Colors.White, PlotType = PlotType.Points, Thickness = 1)]
        public IndicatorDataSeries SwingHighs { get; set; }

        [Output("Swing Low", Color = Colors.White, PlotType = PlotType.Points, Thickness = 1)]
        public IndicatorDataSeries SwingLows { get; set; }


        // Market Structure Series
        [Output("Higher High", Color = Colors.Pink, PlotType = PlotType.Points, Thickness = 12)]
        public IndicatorDataSeries HigherHighs { get; set; }

        [Output("Lower High", Color = Colors.Pink, PlotType = PlotType.Points, Thickness = 12)]
        public IndicatorDataSeries LowerHighs { get; set; }

        [Output("Higher Low", Color = Colors.Pink, PlotType = PlotType.Points, Thickness = 12)]
        public IndicatorDataSeries HigherLows { get; set; }

        [Output("Lower Low", Color = Colors.Pink, PlotType = PlotType.Points, Thickness = 12)]
        public IndicatorDataSeries LowerLows { get; set; }


        protected override void Initialize()
        {
            Chart.RemoveAllObjects();
            InitializeConfiguration();
            InitializeServices();
        }

        private void InitializeConfiguration()
        {
            _config = new IndicatorConfiguration
            {
                ShowSwingPoints = ShowSwingPoints,
                UtcOffset = UtcOffset,
                ShowMacros = ShowMacros,
                MacroFilter = MacroFilter,
                ShowMarketStructure = ShowMarketStructure,
                ShowStructure = ShowStructure,
                ShowChoch = ShowChoch,
                ShowCISD = ShowCISD,
                MaxCisdsPerDirection = MaxCisdsPerDirection,
                ShowOrderFlow = ShowOrderFlow,
                ShowFVG = ShowFVG,
                ShowOrderBlock = ShowOrderBlock,
                ShowRejectionBlock = ShowRejectionBlock,
                ShowBreakerBlock = ShowBreakerBlock,
                ShowUnicorn = ShowUnicorn,
                ShowGauntlet = ShowGauntlet,
                ShowQuadrants = ShowQuadrants,
                ShowInsideKeyLevel = ShowInsideKeyLevel,
                ShowLiquiditySweep = ShowLiquiditySweep,
                ShowStdv = ShowStdv,
                ShowFibonacciLevels = ShowFibonacciLevels,
                ShowSMT = ShowSMT,
                SMTPair = SMTPair,
                EnableLog = EnableLog,
                EnableTelegram = EnableTelegram
            };
        }

        private void InitializeServices()
        {
            try
            {
                _notificationService = ServiceFactory.CreateNotificationService(
                    _config.EnableLog, _config.EnableTelegram,
                    _config.TelegramChatId, _config.TelegramToken,
                    Symbol.Name, _config.UtcOffset, Print);

                _swingDetector = ServiceFactory.CreateSwingPointDetector(SwingHighs, SwingLows);

                _marketDataProvider = new MarketDataProvider(Symbols, MarketData, TimeFrame, message => Print(message));

                _timeManager = ServiceFactory.CreateTimeManager(
                    Chart, Bars, _swingDetector, _notificationService,
                    _config.ShowMacros, _config.ShowFibonacciLevels, _config.UtcOffset);

                _marketStructureAnalyzer = ServiceFactory.CreateMarketStructureAnalyzer(
                    Chart, SwingHighs, SwingLows, HigherHighs, LowerHighs, LowerLows, HigherLows,
                    _config.ShowStructure, _config.ShowChoch, _config.ShowStdv, _standardDeviations, Print);

                _pdArrayAnalyzer = ServiceFactory.CreatePdArrayAnalyzer(
                    Chart, Bars, _config.ShowOrderFlow, _config.ShowLiquiditySweep, _config.ShowGauntlet,
                    _config.ShowFVG, _config.ShowOrderBlock, _config.ShowCISD, _config.ShowBreakerBlock,
                    _config.ShowUnicorn, _config.ShowQuadrants, _config.ShowInsideKeyLevel, _config.ShowRejectionBlock,
                    _config.MaxCisdsPerDirection, _swingDetector, _config.ShowSMT, _config.SMTPair,
                    _config.MacroFilter, _notificationService, _timeManager, Print);

                _serviceCoordinator = new ServiceCoordinator(
                    _config, _swingDetector, _marketStructureAnalyzer, _pdArrayAnalyzer,
                    _timeManager, _notificationService, _marketDataProvider, Print);
            }
            catch (Exception ex)
            {
                Print($"Error initializing services: {ex.Message}");
            }
        }

        public override void Calculate(int index)
        {
            if (index <= 1 || _serviceCoordinator == null)
                return;

            try
            {
                var currentBar = Bars[index];
                var previousBar = Bars[index - 1];
                _serviceCoordinator.ProcessBar(index, currentBar, previousBar, index - 1, Bars);
            }
            catch (Exception ex)
            {
                Print($"Error in Calculate: {ex.Message}");
            }
        }


        // Expose service data through ServiceCoordinator
        public List<SwingPoint> GetAllSwingPoints() => _serviceCoordinator?.GetAllSwingPoints() ?? new List<SwingPoint>();
        public SwingPoint GetLastSwingHigh() => _serviceCoordinator?.GetLastSwingHigh();
        public SwingPoint GetLastSwingLow() => _serviceCoordinator?.GetLastSwingLow();


        public Direction GetMarketBias() => _serviceCoordinator?.GetMarketBias() ?? Direction.Up;
        public List<SwingPoint> GetExternalLiquidityPoints() => _serviceCoordinator?.GetExternalLiquidityPoints() ?? new List<SwingPoint>();

        // Delegate PD Array methods to ServiceCoordinator
        public List<Level> GetPdArrays() => _serviceCoordinator?.GetPdArrays() ?? new List<Level>();
        public List<Level> GetBullishPdArrays() => _serviceCoordinator?.GetBullishPdArrays() ?? new List<Level>();
        public List<Level> GetBearishPdArrays() => _serviceCoordinator?.GetBearishPdArrays() ?? new List<Level>();
        public Level GetLastBullishPdArray() => _pdArrayAnalyzer?.GetLastBullishPdArray();
        public Level GetLastBearishPdArray() => _pdArrayAnalyzer?.GetLastBearishPdArray();

        // Delegate FVG methods
        public List<Level> GetAllFVGs() => _serviceCoordinator?.GetAllFVGs() ?? new List<Level>();
        public List<Level> GetBullishFVGs() => _pdArrayAnalyzer?.GetBullishFVGs() ?? new List<Level>();
        public List<Level> GetBearishFVGs() => _pdArrayAnalyzer?.GetBearishFVGs() ?? new List<Level>();

        // Delegate Order Block methods
        public List<Level> GetAllOrderBlocks() => _serviceCoordinator?.GetAllOrderBlocks() ?? new List<Level>();
        public List<Level> GetBullishOrderBlocks() => _pdArrayAnalyzer?.GetBullishOrderBlocks() ?? new List<Level>();
        public List<Level> GetBearishOrderBlocks() => _pdArrayAnalyzer?.GetBearishOrderBlocks() ?? new List<Level>();

        // Delegate liquidity methods
        public List<Level> GetLiquiditySweepLevels() => _pdArrayAnalyzer?.GetLiquiditySweepLevels() ?? new List<Level>();
        public List<Level> GetMultipleSweptLevels() => _pdArrayAnalyzer?.GetPdArrays().Where(l => l.SweptSwingPoints?.Count > 1).ToList() ?? new List<Level>();
        public List<SwingPoint> GetSweptSwingPoints()
        {
            var sweptPoints = new List<SwingPoint>();
            var levels = _pdArrayAnalyzer?.GetPdArrays();
            if (levels != null)
            {
                foreach (var level in levels.Where(l => l.SweptSwingPoints != null))
                {
                    sweptPoints.AddRange(level.SweptSwingPoints);
                }
            }
            return sweptPoints;
        }

        // Delegate Gauntlet methods
        public List<Level> GetGauntlets() => _pdArrayAnalyzer?.GetGauntlets() ?? new List<Level>();
        public List<Level> GetGauntlets(Direction direction) => _pdArrayAnalyzer?.GetGauntlets(direction) ?? new List<Level>();
        public Level GetLastGauntlet(Direction direction) => _pdArrayAnalyzer?.GetGauntlets(direction).OrderByDescending(g => g.Index).FirstOrDefault();


        // Delegate CISD and Time methods
        public List<Level> GetAllCISDLevels() => _pdArrayAnalyzer?.GetAllCISDLevels() ?? new List<Level>();
        public List<Level> GetActiveCISDLevels() => _pdArrayAnalyzer?.GetActiveCISDLevels() ?? new List<Level>();
        public List<Level> GetConfirmedCISDLevels() => _pdArrayAnalyzer?.GetConfirmedCISDLevels() ?? new List<Level>();
        public bool IsInMacroTime(DateTime time) => _serviceCoordinator?.IsInMacroTime(time) ?? false;
        public List<TimeRange> GetMacros() => _serviceCoordinator?.GetMacros() ?? new List<TimeRange>();

        // Delegate Breaker Block methods
        public List<Level> GetAllBreakerBlocks() => _pdArrayAnalyzer?.GetAllBreakerBlocks() ?? new List<Level>();
        public List<Level> GetBullishBreakerBlocks() => _pdArrayAnalyzer?.GetBullishBreakerBlocks() ?? new List<Level>();
        public List<Level> GetBearishBreakerBlocks() => _pdArrayAnalyzer?.GetBearishBreakerBlocks() ?? new List<Level>();

        // Delegate Unicorn methods
        public List<Level> GetAllUnicorns() => _pdArrayAnalyzer?.GetAllUnicorns() ?? new List<Level>();
        public List<Level> GetUnicorns(Direction direction) => _pdArrayAnalyzer?.GetUnicorns(direction) ?? new List<Level>();
        public Level GetLastUnicorn(Direction direction) => _pdArrayAnalyzer?.GetLastUnicorn(direction);

        // Delegate Active PD Array methods
        public List<Level> GetActivePdArrays() => _pdArrayAnalyzer?.GetActivePdArrays() ?? new List<Level>();
        public List<Level> GetActiveBullishPdArrays() => _pdArrayAnalyzer?.GetActivePdArrays(Direction.Up) ?? new List<Level>();
        public List<Level> GetActiveBearishPdArrays() => _pdArrayAnalyzer?.GetActivePdArrays(Direction.Down) ?? new List<Level>();
        public List<Level> GetActiveFVGs() => _pdArrayAnalyzer?.GetAllFVGs().Where(fvg => fvg.IsActive).ToList() ?? new List<Level>();
        public List<Level> GetActiveOrderBlocks() => _pdArrayAnalyzer?.GetAllOrderBlocks().Where(ob => ob.IsActive).ToList() ?? new List<Level>();
        public List<SwingPoint> GetSwingPointsThatSweptQuadrants() => GetAllSwingPoints().Where(sp => sp.InsideKeyLevel).ToList();
        public List<SwingPoint> GetSMTDivergencePoints() => _config?.ShowSMT == true ? _swingDetector?.GetAllSwingPoints().Where(sp => sp.HasSMT).ToList() ?? new List<SwingPoint>() : new List<SwingPoint>();
    }
}