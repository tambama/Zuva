using System;
using System.Collections.Generic;
using cAlgo.API;
using Zuva.Interfaces;
using Zuva.Services;
using Zuva.Models;

namespace Zuva.Factories
{
    public class ServiceFactory
    {
        public static INotificationService CreateNotificationService(
            bool enableLog,
            bool enableTelegram,
            string telegramChatId,
            string telegramToken,
            string symbolName,
            int utcOffset,
            Action<string> printAction)
        {
            return new NotificationService(
                enableLog,
                enableTelegram,
                telegramChatId,
                telegramToken,
                symbolName,
                utcOffset,
                printAction);
        }

        public static ISwingPointDetector CreateSwingPointDetector(
            IndicatorDataSeries swingHighs,
            IndicatorDataSeries swingLows)
        {
            return new SwingPointDetector(swingHighs, swingLows);
        }

        public static IMarketStructureAnalyzer CreateMarketStructureAnalyzer(
            Chart chart,
            IndicatorDataSeries highs,
            IndicatorDataSeries lows,
            IndicatorDataSeries hhs,
            IndicatorDataSeries lhs,
            IndicatorDataSeries lls,
            IndicatorDataSeries hls,
            bool showStructure,
            bool showChoch,
            bool showStdv,
            List<StandardDeviation> standardDeviations,
            Action<string> logger)
        {
            return new MarketStructureAnalyzer(
                chart, highs, lows, hhs, lhs, lls, hls,
                showStructure, showChoch, showStdv, standardDeviations, logger);
        }

        public static ITimeManager CreateTimeManager(
            Chart chart,
            Bars bars,
            ISwingPointDetector swingDetector,
            INotificationService notificationService,
            bool showMacros,
            bool showFibonacciLevels,
            int utcOffset)
        {
            return new TimeManager(
                chart, bars, swingDetector, notificationService,
                showMacros, showFibonacciLevels, utcOffset);
        }

        public static IPdArrayAnalyzer CreatePdArrayAnalyzer(
            Chart chart,
            Bars bars,
            bool showOrderFlow,
            bool showLiquiditySweep,
            bool showGauntlet,
            bool showFVG,
            bool showOrderBlock,
            bool showCISD,
            bool showBreakerBlock,
            bool showUnicorn,
            bool showQuadrants,
            bool showInsideKeyLevel,
            bool showRejectionBlock,
            int maxCisdsPerDirection,
            ISwingPointDetector swingDetector,
            bool showSMT,
            string smtPair,
            bool macroFilter,
            INotificationService notificationService,
            ITimeManager timeManager,
            Action<string> logger)
        {
            return new PdArrayAnalyzer(
                chart, bars, showOrderFlow, showLiquiditySweep, showGauntlet,
                showFVG, showOrderBlock, showCISD, showBreakerBlock, showUnicorn,
                showQuadrants, showInsideKeyLevel, showRejectionBlock, maxCisdsPerDirection,
                swingDetector, showSMT, smtPair, macroFilter, notificationService, timeManager, logger);
        }
    }
}