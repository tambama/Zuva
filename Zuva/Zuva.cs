using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using Zuva.Extensions;
using Zuva.Helpers;
using Zuva.Models;
using Zuva.Services;
using StandardDeviation = Zuva.Models.StandardDeviation;

namespace Zuva
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.EasternStandardTime, AccessRights = AccessRights.FullAccess)]
    public class Zuva : Indicator
    {
        #region Fields

        // Bar Index
        private int _index;
        private int _startIndex;
        
        // Direction of the ZigZag
        private Direction _direction = Direction.Down;
        private Direction _bias = Direction.Down;
        
        // Price of recent High
        private double _high;

        // Price of recent Low
        private double _low;
        
        // Current Bar
        private Bar _currentBar;

        // Previous Bar
        private Bar _barOne;
        private int _barOneIndex;
        
        // Third Bar from Current
        private Bar _barTwo;
        private int _barTwoIndex;
        
        // Fourth Bar from Current
        private Bar _barThree;
        private int _barThreeIndex;
        
        // Fifth Bar from Current
        private Bar _barFour;
        private int _barFourIndex;
        
        // Liquidity
        private SwingPoint _swingPoint;
        private List<SwingPoint> _swingPoints;
        private List<SwingPoint> _orderedHighs;
        private List<SwingPoint> _orderedLows;
        private List<SwingPoint> _externalLiquidity;
        private List<int> _sweepers;
        
        // Premium/Discount Levels
        private List<Level> _pdArrays;
        private List<Level> _orderflow;
        private Level _potentialChoCh;
        
        // Change of Character
        private Level _potentialBullishChoCh;
        private Level _potentialBearishChoCh;
        
        // BOS/CHoCH
        private SwingPoint _lowBOS;
        private SwingPoint _highBOS;
        private SwingPoint _lowIND;
        private SwingPoint _highIND;
        private SwingPoint _highCHOCH;
        private SwingPoint _lowCHOCH;
        
        // Unicorn
        private Level _currentBreaker;
        private Level _currentCisd;
        
        // Standard Deviations
        private List<StandardDeviation> _standardDeviations;
        
        // Opening Price
        private double _trueOpeningPrice;
        
        // PDA
        private bool _firstPresentationSet;
        
        private bool _sweptDailyHigh = false;
        private bool _sweptDailyLow = false;
        
        private double _currentDayHigh;
        private double _currentDayLow;
        
        // Time
        public List<TimeRange> _macros;
        private List<TimeRange> _cycles;
        private bool _insideMacro;
        private bool _insideCycle;
        
        // Moving Averages
        private ExponentialMovingAverage _emaFast;
        private ExponentialMovingAverage _emaSlow;
        private Direction _emaDirection = Direction.Up;
        private bool _emaReady = false;
        
        // Services
        private TimeService _timeService;
        
        // Sounds
        private const string LiquiditySweepNotification = "/Users/peniel/Music/Music/Alerts/alert.wav";
        
        // Notifications
        private TelegramService _telegramService;
        private const string TelegramToken = "7507336625:AAHM4oYlg_5XIjzzCNFCR_oyLu1Y69qkvns";
        private const string TelegramChatId = "5631623580";

        #endregion

        #region Parameters
        
        [Parameter("Show Macros", Group = "Times")]
        public bool ShowMacros { get; set; }
        [Parameter("Show Cycles", Group = "Times")]
        public bool ShowCycles { get; set; }
        [Parameter("Trade Macros", Group = "Times")]
        public bool TradeMacros { get; set; }
        public bool AlertProjections { get; set; }
        [Parameter("Show Liquidity", Group = "Liquidity")]
        public bool ShowLiquidity { get; set; }
        [Parameter("Show Liquidity Sweep", Group = "Liquidity")]
        public bool ShowLiquiditySweep { get; set; }
        [Parameter("Session Levels", Group = "Liquidity")]
        public bool ShowSessionLevels { get; set; }
        [Parameter("Fibonacci Levels", Group = "Fibonacci")]
        public bool ShowFibs { get; set; }
        [Parameter("CHoCH STDV", Group = "Fibonacci")]
        public bool ShowChoChFibs { get; set; }
        
        [Output("highs", LineColor = "White", Thickness = 1, PlotType = PlotType.Points)]
        public IndicatorDataSeries highs { get; set; }
        
        [Output("lows", LineColor = "White", Thickness = 1, PlotType = PlotType.Points)]
        public IndicatorDataSeries lows { get; set; }
        
        [Output("Swing Highs", LineColor = "Pink", Thickness = 5, PlotType = PlotType.Points)]
        public IndicatorDataSeries Highs { get; set; }
        
        [Output("Swing Lows", LineColor = "Pink", Thickness = 5, PlotType = PlotType.Points)]
        public IndicatorDataSeries Lows { get; set; }
        
        [Parameter("Play Alert", Group = "Notification")]
        public bool PlayAlert { get; set; }
        [Parameter("Send Message", Group = "Notification")]
        public bool SendMessage { get; set; }
        [Parameter("Opening Range Gaps", Group = "Gaps")]
        public bool ShowOpeningRangeGap { get; set; }
        [Parameter("First Presented FVG", Group = "P/D Arrays")] 
        public bool ShowFirstPresentation { get; set; } = true;
        [Parameter("Fair Value Gaps", Group = "P/D Arrays")] 
        public bool ShowFVG { get; set; }
        [Parameter("Orderflow", Group = "P/D Arrays")]
        public bool ShowOrderflow { get; set; }
        [Parameter("Market Structure Shift", Group = "P/D Arrays")] 
        public bool ShowMSS { get; set; }
        [Parameter("CHoCH", Group = "P/D Arrays")] 
        public bool ShowChoCh { get; set; }
        
        [Parameter("Order Blocks", Group = "P/D Arrays")] 
        public bool ShowOrderBlocks { get; set; }
        [Parameter("Breaker Blocks", Group = "P/D Arrays")] 
        public bool ShowBreakerBlocks { get; set; }
        [Parameter("Unicorn", Group = "P/D Arrays")] 
        public bool ShowUnicorn { get; set; }
        [Parameter("Show FVG Activation", Group = "P/D Arrays")]
        public bool ShowFVGActivation { get; set; } = true;

        [Parameter("Show OB Activation", Group = "P/D Arrays")]
        public bool ShowOrderBlockActivation { get; set; } = true;
        
        [Parameter("Data Source", Group = "Moving Averages")]
        public DataSeries Price { get; set; }
        [Parameter("Slow Periods", DefaultValue = 200, Group = "Moving Averages")]
        public int SlowPeriods { get; set; }
        [Parameter("Fast Periods", DefaultValue = 50, Group = "Moving Averages")]
        public int FastPeriods { get; set; }
        
        [Parameter("Clear Chart", Group = "Chart")]
        public bool ClearChart { get; set; }

        #endregion

        protected override void Initialize()
        {
            //var result = System.Diagnostics.Debugger.Launch();
            //if (result is false)
            //{
            //    Print("Debugger launch failed!");
            //}

            if (ClearChart)
            {
                Chart.RemoveAllObjects();
            }

            _macros = TimeHelpers.InitializeMacros();
            _cycles = TimeHelpers.InitializeCycles();
            
            _telegramService = new TelegramService();
            _timeService = new TimeService();
            
            _swingPoints = new List<SwingPoint>();
            _orderedLows = new List<SwingPoint>();
            _orderedHighs = new List<SwingPoint>();
            _externalLiquidity = new List<SwingPoint>();
            _sweepers = new List<int>();
            _pdArrays = new List<Level>();
            _orderflow = new List<Level>();
            _standardDeviations = new List<StandardDeviation>();
            
            // initialize new instances of ExponentialMovingAverage Indicator class
            _emaFast = Indicators.ExponentialMovingAverage(Price, FastPeriods);
            // _emaSlow is the exponential moving average of the emaFast
            _emaSlow = Indicators.ExponentialMovingAverage(_emaFast.Result, SlowPeriods);
        }

        public override void Calculate(int index)
        {
            _index = index;
            _low = Bars.LowPrices.LastValue;
            _high = Bars.HighPrices.LastValue;
            _currentBar = Bars.LastBar;

            switch (Bars.Count)
            {
                case 1:
                    return;
                case 2:
                {
                    var startIndex = index - 1;
                    InitializeSwingPoints(startIndex, Bars[startIndex]);

                    return;
                }
            }

            _barOne = Bars[index - 1];
            _low = _barOne.Low;
            _high = _barOne.High;

            _barOneIndex = index - 1;
            
            // Time Check
            TimeCheck();
            
            // If the index is less than SlowPeriods don't calculate
            if(index > SlowPeriods)
            {
                _emaReady = true;
                if(_emaFast.Result.HasCrossedAbove(_emaSlow.Result,0))
                {
                    _emaDirection = Direction.Up;
                }

                if (_emaFast.Result.HasCrossedBelow(_emaSlow.Result, 0))
                {
                    _emaDirection = Direction.Down;
                }
            }
            
            ZigZag();
            
            // Check for CHoCH
            CheckForChoCHOnCurrentBar();
            
            // Liquidity Check
            //LiquidityCheck();

            if (Bars.Count > 4)
            {
                _barTwo = Bars[index - 2];
                _barThree = Bars[index - 3];
                
                _barOneIndex = index - 1;
                _barTwoIndex = index - 2;
                _barThreeIndex = index - 3;
                _barFour = Bars[index - 4];
                MarkFairValueGap();
            }
            
            _externalLiquidity.LiquidityCheck(Chart, MarketData, Bars, _currentBar, ShowSessionLevels, ShowLiquidity);
            _externalLiquidity.CheckLiquiditySweep(Chart, _barOne, _barOneIndex, _sweepers, ShowLiquiditySweep);
        }
        
        #region ZigZagMethods

        private void ZigZag()
        {
            // In case of a new high
            if (_barOne.High > _swingPoint.Bar.High && _barOne.Low < _swingPoint.Bar.Low)
            {
                // Bullish Candle: set low then high
                if (_barOne.Close > _barOne.Open)
                {
                    if (_swingPoint.Direction == Direction.Down)
                    {
                        MoveExtremum(_barOne.Low, Direction.Down);
                        SetExtremum(_high, Direction.Up);
                    }
                    else
                    {
                        SetExtremum(_low, Direction.Down);
                        SetExtremum(_high, Direction.Up);
                    }
                }
                else
                {
                    if (_swingPoint.Direction == Direction.Up)
                    {
                        MoveExtremum(_high, Direction.Up);
                        SetExtremum(_low, Direction.Down);
                    }
                    else
                    {
                        SetExtremum(_high, Direction.Up);
                        SetExtremum(_low, Direction.Down);
                    }
                }
            }
            else if (_barOne.Low < _swingPoint.Bar.Low && _barOne.High > _swingPoint.Bar.High)
            {
                // Bearish Candle: set high then low
                if (_barOne.Close < _barOne.Open)
                {
                    SetExtremum(_high, Direction.Up);
                    SetExtremum(_low, Direction.Down);
                }
            }
            else if (_barOne.High > _swingPoint.Bar.High)
            {
                if (_swingPoint.Direction == Direction.Up)
                {
                    MoveExtremum(_high, Direction.Up);
                }
                else
                {
                    SetExtremum(_high, Direction.Up);
                }
            }
            else if (_barOne.Low < _swingPoint.Bar.Low)
            {
                if (_swingPoint.Direction == Direction.Down)
                {
                    MoveExtremum(_low, Direction.Down);
                }
                else
                {
                    SetExtremum(_low, Direction.Down);
                }
            }
        }

        private void MoveExtremum(double price, Direction direction)
        {
            if (direction == Direction.Up)
            {
                highs[_swingPoint.Index] = double.NaN;
            }
            else
            {
                lows[_swingPoint.Index] = double.NaN;
            }

            var point = new SwingPoint();
            if (_swingPoints.Count > 0)
            {
                point = _swingPoints[_swingPoints.Count - 1];
                _swingPoints.Remove(point);
            }

            SetExtremum(price, direction, true, point);
        }

        private void SetExtremum(double price, Direction direction, bool isMove = false, SwingPoint swingPoint = null)
        {
            _swingPoint = new SwingPoint(_barOneIndex, price, _barOne.OpenTime, _barOne, direction == Direction.Down ? SwingType.L : SwingType.H, LiquidityType.Normal, direction);
            _orderedHighs = _swingPoints.Where(s => s.Direction == Direction.Up).OrderByDescending(s => s.Time).ToList();
            _orderedLows = _swingPoints.Where(s => s.Direction == Direction.Down).OrderByDescending(s => s.Time).ToList();
            
            // Check if the bar closes above the mid point of any bearish pd array
            if (_barOne.Close > _barOne.Open) // Bullish candle
            {
                var bearishPDArrays = _pdArrays.Where(l => l.Direction == Direction.Down && !l.IsInverted &&
                                                              _barOne.Close > l.Mid).ToList();
        
                // Remove all matching order blocks from the collection
                foreach (var orderBlock in bearishPDArrays)
                {
                    orderBlock.IsInverted = true;
                }
            }
            
            // Check if the bar closes below the mid point of any bullish order block
            if (_barOne.Close < _barOne.Open) // Bullish candle
            {
                var bullishPDArrays = _pdArrays.Where(l => l.Direction == Direction.Up && !l.IsInverted &&
                                                           _barOne.Close < l.Mid).ToList();
        
                // Remove all matching order blocks from the collection
                foreach (var orderBlock in bullishPDArrays)
                {
                    orderBlock.IsInverted = true;
                }
            }
            
            if (direction == Direction.Up)
            {
                var lastDown = _orderedLows.FirstOrDefault();
                if (lastDown != null)
                    _swingPoint.PreviousIndex = lastDown.Index;
                
                highs[_swingPoint.Index] = _swingPoint.Price;
            }
            else
            {
                var lastUp = _orderedHighs.FirstOrDefault();
                if (lastUp != null)
                    _swingPoint.PreviousIndex = lastUp.Index;
                
                lows[_swingPoint.Index] = _swingPoint.Price;
            }
            
            _swingPoints.Add(_swingPoint);
            
            _orderedHighs = _swingPoints.Where(s => s.Direction == Direction.Up).OrderByDescending(s => s.Time).ToList();
            _orderedLows = _swingPoints.Where(s => s.Direction == Direction.Down).OrderByDescending(s => s.Time).ToList();

            if (_swingPoints.Count >= 2)
            {
                MarkSwingPoints(_swingPoint);
                //MarkBreakerBlock(_swingPoint);
                //MarkOrderBlock(_swingPoint);
                //MarkInversionOrderBlock(_swingPoint);
            }

            if (_swingPoints.Count >= 3)
            {
                MarkOrderFlow(_swingPoint);
                ManagePremiumDiscountArrays(_swingPoint);
                ManageOrderBlocks(_swingPoint);
            }

            // if (_swingPoints.Count >= 4)
            // {
            //     CheckChoCh(_swingPoint);
            //     var choch = _potentialChoCh.FindPotentialChoCh(_swingPoints, _swingPoint);
            //     if (choch.success)
            //     {
            //         _potentialChoCh = choch.potentialChoch;
            //     }
            // }
        }

        private void InitializeSwingPoints(int index, Bar bar)
        {
            var direction = bar.GetCandleDirection();
            
            if (direction == Direction.Up)
            {
                _swingPoint = new SwingPoint(index, bar.Low, bar.OpenTime, bar, SwingType.L, LiquidityType.Normal, Direction.Down);
                _swingPoints.Add(_swingPoint);
                lows[_swingPoint.Index] = _swingPoint.Price;
                _lowBOS = _swingPoint;
                    
                _swingPoint = new SwingPoint(index, bar.High, bar.OpenTime, bar, SwingType.H, LiquidityType.Normal, Direction.Up);
                _swingPoints.Add(_swingPoint);
                highs[_swingPoint.Index] = _swingPoint.Price;
                _highBOS = _swingPoint;
            }
            else
            {
                _swingPoint = new SwingPoint(index, bar.High, bar.OpenTime, bar, SwingType.H, LiquidityType.Normal, Direction.Up);
                _swingPoints.Add(_swingPoint);
                highs[_swingPoint.Index] = _swingPoint.Price;
                _highBOS = _swingPoint;
                    
                _swingPoint = new SwingPoint(index, bar.Low, bar.OpenTime, bar, SwingType.L, LiquidityType.Normal, Direction.Down);
                _swingPoints.Add(_swingPoint);
                lows[_swingPoint.Index] = _swingPoint.Price;
                _lowBOS = _swingPoint;
            }
            
            _bias = direction;
        }

        private void MarkSwingPoints(SwingPoint swingPoint)
        {
            if (swingPoint.Direction == Direction.Up)
            {
                if (swingPoint.Price > _highBOS.Price)
                {
                    _highBOS = swingPoint;
                    _lowCHOCH = _lowBOS;
                    Lows[_lowBOS.Index] = _lowBOS.Price;
                    
                    var low = _orderedLows.First(p => p.Index == _lowBOS.Index);
                    low.SwingType = SwingType.LL;
                    var liquidity = _externalLiquidity.Any(l => l.Index == _lowBOS.Index);
                    if (!liquidity)
                    {
                        _externalLiquidity.Add(low);
                    }
                    

                    if (_bias == Direction.Up)
                    {
                        _highIND = _orderedLows[0];
                    }
                    
                    var point = _swingPoints.FirstOrDefault(s => s.Index == _highBOS.Index);
                    if (point != null)
                    {
                        point.Swept = true;
                    }
                }
                
                // Mark Low point after taking out inducement in a downtrend
                if (_bias == Direction.Down && _lowIND != null && swingPoint.Bar.Close > _lowIND.Price)
                {
                    var point = _swingPoints.FirstOrDefault(s => s.Index == _lowBOS.Index);
                    point.SwingType = SwingType.LL;
                    Lows[point.Index] = point.Price;
                    
                    var liquidity = _externalLiquidity.Any(l => l.Index == point.Index);
                    if (!liquidity)
                    {
                        _externalLiquidity.Add(point);
                    }
                    
                    _highIND = null;
                    _highBOS = swingPoint;
                    
                    // TODO: Draw 
                    _lowIND = null;
                }

                // Change of Character
                if (_highCHOCH != null && swingPoint.Price > _highCHOCH.Price)
                {
                    var point = _swingPoints.FirstOrDefault(s => s.Index == _highCHOCH.Index);
                    point.Swept = true;
                    _highBOS = swingPoint;
                    _highIND = _orderedLows[0];
                    _highCHOCH = null;
                    _bias = Direction.Up;
                    Lows[_lowCHOCH.Index] = _lowCHOCH.Price;
                    
                    var low = _swingPoints.FirstOrDefault(s => s.Index == _lowCHOCH.Index);
                    if (low != null)
                    {
                        low.SwingType = SwingType.LL;
                    }
                    
                    var liquidity = _externalLiquidity.Any(l => l.Index == _lowCHOCH.Index);
                    if (!liquidity)
                    {
                        _externalLiquidity.Add(low);
                    }
                    // Mark CHoCH
                }
            }
            else
            {
                if (swingPoint.Price < _lowBOS.Price)
                {
                    _lowBOS = swingPoint;
                    _highCHOCH = _highBOS;
                    Highs[_highBOS.Index] = _highBOS.Price;
                    
                    var high = _orderedHighs.First(p => p.Index == _highBOS.Index);
                    high.SwingType = SwingType.HH;
                    
                    var liquidity = _externalLiquidity.Any(l => l.Index == _lowBOS.Index);
                    if (!liquidity)
                    {
                        _externalLiquidity.Add(high);
                    }

                    if (_bias == Direction.Down)
                    {
                        _lowIND = _orderedHighs[0];
                    }
                    
                    var point = _swingPoints.FirstOrDefault(s => s.Index == _lowBOS.Index);
                    if (point != null)
                    {
                        point.Swept = true;
                    }
                }
                
                // Mark High point after taking out inducement in a downtrend
                if (_bias == Direction.Up && _highIND != null && swingPoint.Bar.Close < _highIND.Price)
                {
                    var point = _swingPoints.FirstOrDefault(s => s.Index == _highBOS.Index);
                    point.SwingType = SwingType.HH;
                    Highs[point.Index] = point.Price;
                    
                    var liquidity = _externalLiquidity.Any(l => l.Index == point.Index);
                    if (!liquidity)
                    {
                        _externalLiquidity.Add(point);
                    }
                    
                    _lowIND = null;
                    _lowBOS = swingPoint;
                    
                    // TODO: Draw
                    _highIND = null;
                }
                
                // Change of Character
                if (_lowCHOCH != null && swingPoint.Price < _lowCHOCH.Price)
                {
                    var point = _swingPoints.FirstOrDefault(s => s.Index == _lowCHOCH.Index);
                    point.Swept = true;
                    _lowBOS = swingPoint;
                    _lowIND = _orderedHighs[0];
                    _lowCHOCH = null;
                    _bias = Direction.Down;
                    Highs[_highBOS.Index] = _highBOS.Price;
                    
                    var high = _swingPoints.FirstOrDefault(s => s.Index == _highBOS.Index);
                    if (high != null) high.SwingType = SwingType.HH;
                    
                    var liquidity = _externalLiquidity.Any(l => l.Index == _highBOS.Index);
                    if (!liquidity)
                    {
                        _externalLiquidity.Add(_highBOS);
                    }
                    
                    // Mark Fibs
                }
            }
            
            DetectChangesOfCharacter(swingPoint);
            
            Chart.UpdateBias(_bias);
        }

        private void CheckChoCh(SwingPoint swingPoint)
        {
            var choch = _potentialChoCh.CheckChoCh(swingPoint);
            if (choch.chocked)
            {
                _potentialChoCh = null;
                
                var level = choch.choch;
                if (ShowChoCh)
                {
                    var point = _swingPoints.FirstOrDefault(s => s.Index == level.Index);
                    Chart.DrawTrendLine($"chock", point, swingPoint, LineType.CHOCH);
                }
                
                // Mark Fibs
            }
        }
        
        private void CheckForChoCHOnCurrentBar()
        {
            if (_swingPoints.Count < 4)
                return;
                
            // Check if current bar completes a CHoCH pattern without creating a swing point
            var (chochDetected, chochLevel, direction) = _swingPoints.DetectChoCh(
                _barOne, 
                Chart, 
                ShowChoCh, 
                ref _externalLiquidity, 
                _index);
            
            if (chochDetected)
            {
                // Add to our levels collection
                _pdArrays.Add(chochLevel);
                
                // Update market bias based on CHoCH direction
                //_bias = direction;
                
                // Reset the corresponding potential CHoCH since it's been confirmed
                if (direction == Direction.Up)
                {
                    _potentialBullishChoCh = null;
                    
                    // Mark CHoCH detected with Fibonacci if enabled
                    if (ShowChoChFibs)
                    {
                        var stdDev = new StandardDeviation(chochLevel.Low, chochLevel.High, chochLevel.HighTime);
                        _standardDeviations.Add(stdDev);
                        Chart.DrawStandardDeviation(stdDev);
                    }
                }
                else
                {
                    _potentialBearishChoCh = null;
                    
                    // Mark CHoCH detected with Fibonacci if enabled
                    if (ShowChoChFibs)
                    {
                        var stdDev = new StandardDeviation(chochLevel.High, chochLevel.Low, chochLevel.LowTime);
                        _standardDeviations.Add(stdDev);
                        Chart.DrawStandardDeviation(stdDev);
                    }
                }
                
                // Optional: Send alert or notification for CHoCH detection
                if (SendMessage)
                {
                    Alert($"{SymbolName} - CHoCH Detected - {direction}");
                }
            }
        }

        private void DetectChangesOfCharacter(SwingPoint swingPoint)
        {
            // Step 1: Check if current bar completes a CHoCH pattern
            // We're passing the current bar directly instead of relying on swing point
            var (chochDetected, chochLevel, direction) = _swingPoints.DetectChoCh(swingPoint.Bar, Chart, ShowChoCh, ref _externalLiquidity, swingPoint.Index);
            
            if (chochDetected)
            {
                // Add to our levels collection
                _pdArrays.Add(chochLevel);
                
                // Update market bias based on CHoCH direction
                _bias = direction;
                
                // Reset the corresponding potential CHoCH since it's been confirmed
                if (direction == Direction.Up)
                {
                    _potentialBullishChoCh = null;
                    
                    // Mark CHoCH detected with Fibonacci if enabled
                    if (ShowChoChFibs)
                    {
                        var stdDev = new StandardDeviation(chochLevel.Low, chochLevel.High, chochLevel.HighTime);
                        _standardDeviations.Add(stdDev);
                        Chart.DrawStandardDeviation(stdDev);
                    }
                }
                else
                {
                    _potentialBearishChoCh = null;
                    
                    // Mark CHoCH detected with Fibonacci if enabled
                    if (ShowChoChFibs)
                    {
                        var stdDev = new StandardDeviation(chochLevel.High, chochLevel.Low, chochLevel.LowTime);
                        _standardDeviations.Add(stdDev);
                        Chart.DrawStandardDeviation(stdDev);
                    }
                }
                
                // Optional: Send alert or notification for CHoCH detection
                if (SendMessage)
                {
                    Alert($"{SymbolName} - CHoCH Detected - {direction}");
                }
            }
            else
            {
                // Step 2: Update potential CHoCH points based on new swing points
                if (_potentialBullishChoCh != null)
                {
                    _potentialBullishChoCh = _swingPoints.UpdatePotentialChoCh(_potentialBullishChoCh, swingPoint);
                }
                
                if (_potentialBearishChoCh != null)
                {
                    _potentialBearishChoCh = _swingPoints.UpdatePotentialChoCh(_potentialBearishChoCh, swingPoint);
                }
                
                // Look for new potential CHoCH setups
                if (swingPoint.Direction == Direction.Down)
                {
                    var orderedLows = _swingPoints.Where(s => s.Direction == Direction.Down).OrderByDescending(s => s.Index).ToList();
                    if (orderedLows.Count >= 2 && orderedLows[0].Price < orderedLows[1].Price)
                    {
                        // We have a lower low, look for the high that led to it
                        var highPoint = _swingPoints
                            .Where(s => s.Direction == Direction.Up && s.Index < orderedLows[0].Index && s.Index > orderedLows[1].Index)
                            .OrderByDescending(s => s.Index)
                            .FirstOrDefault();
                            
                        if (highPoint != null)
                        {
                            _potentialBullishChoCh = new Level(
                                LevelType.CISD,
                                orderedLows[0].Price,
                                highPoint.Price,
                                orderedLows[0].Time,
                                highPoint.Time,
                                direction: Direction.Up,
                                index: highPoint.Index,
                                indexLow: orderedLows[0].Index,
                                indexHigh: highPoint.Index);
                        }
                    }
                }
                else if (swingPoint.Direction == Direction.Up)
                {
                    var orderedHighs = _swingPoints.Where(s => s.Direction == Direction.Up).OrderByDescending(s => s.Index).ToList();
                    if (orderedHighs.Count >= 2 && orderedHighs[0].Price > orderedHighs[1].Price)
                    {
                        // We have a higher high, look for the low that led to it
                        var lowPoint = _swingPoints
                            .Where(s => s.Direction == Direction.Down && s.Index < orderedHighs[0].Index && s.Index > orderedHighs[1].Index)
                            .OrderByDescending(s => s.Index)
                            .FirstOrDefault();
                            
                        if (lowPoint != null)
                        {
                            _potentialBearishChoCh = new Level(
                                LevelType.CISD,
                                lowPoint.Price,
                                orderedHighs[0].Price,
                                lowPoint.Time,
                                orderedHighs[0].Time,
                                direction: Direction.Down,
                                index: lowPoint.Index,
                                indexLow: lowPoint.Index,
                                indexHigh: orderedHighs[0].Index);
                        }
                    }
                }
            }
        }

        #endregion

        #region ManagePDArrays

        private void ManagePremiumDiscountArrays(SwingPoint swingPoint)
        {
            if (swingPoint.Direction == Direction.Up && _orderedHighs.Count >= 2)
            {
                // Bearish Breaker Blocks
                {
                    var previousHigh = _orderedHighs[1];
                    if (!(swingPoint.Price > previousHigh.Price)) return;

                    var orderFlow = _pdArrays.FirstOrDefault(p => p.Index == previousHigh.Index);
                    if (orderFlow != null)
                    {
                        SearchForPossibleBreaker(orderFlow);
                    }
                }
                
                // Bullish CISD
                {
                    var bullishBreaker = _pdArrays.FirstOrDefault(p => p.LevelType == LevelType.BreakerBlock && p.Direction == Direction.Up && !p.IsConfirmed && swingPoint.Bar.Close > p.High && swingPoint.Bar.Open < p.High);
                    if (bullishBreaker != null)
                    {
                        var (isCisd, cisd) = _pdArrays.FindChangeInStateOfDelivery(_externalLiquidity, bullishBreaker, Bars, Chart, _sweepers, ShowFibs, _swingPoint.Bar.OpenTime);

                        if (isCisd)
                        {
                            if (TradeMacros && !_insideMacro) return;
                            
                            _currentCisd = cisd;
                            _currentBreaker = bullishBreaker;
                            var (isUnicorn, unicorn) = bullishBreaker.FindUnicorn(cisd, swingPoint, Bars, _pdArrays);
                            if (isUnicorn && ShowUnicorn)
                            {
                                Chart.DrawTrendLine("uni", unicorn, LineType.Unicorn);
                            }
                            else if (ShowMSS)
                            {
                                Chart.DrawTrendLine($"cisd", cisd, LineType.CISD);
                            }
                            
                            if (SendMessage)
                            {
                                if (TradeMacros && _insideMacro)
                                {
                                    _telegramService.SendTelegram(TelegramChatId, TelegramToken, $"{SymbolName} - CISD");
                                }
                                else if (!TradeMacros)
                                {
                                    _telegramService.SendTelegram(TelegramChatId, TelegramToken, $"{SymbolName} - CISD");
                                }
                            }
                        }
                    }

                }
            }
            else if (swingPoint.Direction == Direction.Down && _orderedLows.Count >= 2)
            {
                // Bullish Breaker Blocks
                {
                    var previousLow = _orderedLows[1];
                    if (swingPoint.Price < previousLow.Price)
                    {
                        var orderFlow = _pdArrays.FirstOrDefault(p => p.Index == previousLow.Index);
                        if (orderFlow != null)
                        {
                            SearchForPossibleBreaker(orderFlow);
                        }
                    }
                }
                
                // Bearish CISD
                {
                    var bearishBreaker = _pdArrays.FirstOrDefault(p => p.LevelType == LevelType.BreakerBlock && p.Direction == Direction.Down && !p.IsConfirmed && swingPoint.Bar.Close < p.Low && swingPoint.Bar.Open > p.Low);
                    if (bearishBreaker != null)
                    {
                        var (isCisd, cisd) = _pdArrays.FindChangeInStateOfDelivery(_externalLiquidity, bearishBreaker, Bars, Chart, _sweepers, ShowFibs, _swingPoint.Bar.OpenTime);

                        if (isCisd)
                        {
                            if (TradeMacros && !_insideMacro) return;
                            
                            _currentCisd = cisd;
                            _currentBreaker = bearishBreaker;
                            var (isUnicorn, unicorn) = bearishBreaker.FindUnicorn(cisd, swingPoint, Bars, _pdArrays);
                            if (isUnicorn && ShowUnicorn)
                            {
                                Chart.DrawTrendLine("uni", unicorn, LineType.Unicorn);
                            }
                            else if (ShowMSS)
                            {
                                Chart.DrawTrendLine($"cisd", cisd, LineType.CISD);
                            }
                            
                            if (SendMessage)
                            {
                                if (TradeMacros && _insideMacro)
                                {
                                    _telegramService.SendTelegram(TelegramChatId, TelegramToken, $"{SymbolName} - CISD");
                                }
                                else if (!TradeMacros)
                                {
                                    _telegramService.SendTelegram(TelegramChatId, TelegramToken, $"{SymbolName} - CISD");
                                }
                            }
                        }
                    }

                }
            }
            
            _standardDeviations.FindChangeOfCharacter(_swingPoints, swingPoint, _pdArrays.Where(p => p.LevelType == LevelType.OrderBlock).ToList(), Chart);
        }

        private void ManageOrderBlocks(SwingPoint swingPoint)
        {
            var ob = _pdArrays.Where(p => p.LevelType == LevelType.OrderBlock).ToList().IsInOrderBlock(swingPoint, Chart, true);
        }
    
        private void SearchForPossibleBreaker(Level orderFlow)
        {
            var breaker = orderFlow.FindPotentialBreaker(Bars);
            if (!breaker.success) return;
            if(ShowBreakerBlocks) Chart.DrawRectangle(breaker.breaker, "bb", opacity:50);
            
            // remove pending breakers
            var pending = _pdArrays.GetLevelsByTypeAndDirection(LevelType.BreakerBlock, orderFlow.Direction);
            if (pending.success)
            {
                foreach (var level in pending.levels)
                {
                    _pdArrays.Remove(level);
                }
            }
            
            _pdArrays.Add(breaker.breaker);
        }
        
        private void MarkOrderFlow(SwingPoint swingPoint)
        {
            var (success, orderFlow) = _pdArrays.FindOrderFlow(_swingPoints, swingPoint);
            if (!success) return;
            if (ShowOrderflow)
            {
                Chart.DrawRectangle(orderFlow, "of");
            }
        }

        private void MarkFairValueGap()
        {
            var fvg = _pdArrays.FindFairValueGap(_barOne, _barThree, _barThreeIndex, _barTwo.OpenTime);
            if (!fvg.success) return;
            
            if (!_firstPresentationSet &&  _barThree.OpenTime.TimeOfDay >= new TimeSpan(09, 30, 00) && ShowFirstPresentation)
            {
                var time1 = fvg.fvg.HighTime < fvg.fvg.LowTime ? fvg.fvg.HighTime : fvg.fvg.LowTime;
                var time2 = (fvg.fvg.HighTime > fvg.fvg.LowTime ? fvg.fvg.HighTime : fvg.fvg.LowTime).Date.AddDays(3).AddHours(9).AddMinutes(30);
                
                var box = Chart.DrawRectangle($"fp-{_barThree.OpenTime}", time1, fvg.fvg.High, time2, fvg.fvg.Low, Color.Wheat);
                var ce = Chart.DrawTrendLine($"fp-ce-{time1.AddMinutes(1)}", time1.AddMinutes(1), fvg.fvg.Mid, time2, fvg.fvg.Mid, Color.Wheat,1, LineStyle.Dots);
                
                _firstPresentationSet = true;
            }
            
            if (ShowFVG) Chart.DrawLevel(fvg.fvg, true);

            var ob = _pdArrays.FindOrderBlock(_swingPoints.Where(s => s.Direction != fvg.fvg.Direction).ToList(), _barThreeIndex);
            if (ob.success && ShowOrderBlocks) Chart.DrawRectangle(ob.orderBlock, "ob", opacity: 20);

            if (!ob.success)
            {
                if (fvg.fvg.Direction == Direction.Down)
                {
                    ob = _pdArrays.FindOrderBlock(_barFour, _barThree, Direction.Down);
                    if (ob.success && ShowOrderBlocks) Chart.DrawRectangle(ob.orderBlock, "ob", opacity: 20);
                }
                else
                {
                    ob = _pdArrays.FindOrderBlock(_barFour, _barThree, Direction.Up);
                    if (ob.success && ShowOrderBlocks) Chart.DrawRectangle(ob.orderBlock, "ob", opacity: 20);
                }
            }

            if (_currentBreaker == null || _currentCisd == null) return;
            
            var (success, unicorn) = _currentBreaker.FindUnicorn(_currentCisd, fvg.fvg, _pdArrays);
            if (!success || !ShowUnicorn) return;
            
            Chart.DrawRectangle(unicorn, "uni", opacity: 50);
            Chart.DrawTrendLine("uni", unicorn, LineType.Unicorn);
        }

        #endregion

        public void Alert(string message)
        {
            Print(message);

            if (PlayAlert)
            {
                Notifications.PlaySound(LiquiditySweepNotification);
            }

            if (SendMessage)
            {
                _telegramService.SendTelegram(TelegramChatId, TelegramToken, message);
            }
        }

        private void TimeCheck()
        {
            if (_currentBar.OpenTime.TimeOfDay == new TimeSpan(0, 0, 0))
            {
                _trueOpeningPrice = _currentBar.Open;
                _sweptDailyHigh = false;
                _sweptDailyLow = false;
                _currentDayHigh = 0;
                _currentDayLow = 0;
                _sweepers.Clear();
            }
            
            _insideMacro = _macros.InsideTimeRange(_currentBar.OpenTime.TimeOfDay);
            
            if (ShowMacros && _macros.IsStartOrEndTime(_currentBar.OpenTime.TimeOfDay))
            {
                Chart.DrawVerticalLine($"macro-{_currentBar.OpenTime}", _currentBar.OpenTime, Color.Gray, 1, LineStyle.Dots);
            }

            _insideCycle = _cycles.InsideTimeRange(_currentBar.OpenTime.TimeOfDay);
            if (ShowCycles && _cycles.IsStartOrEndTime(_currentBar.OpenTime.TimeOfDay))
            {
                Chart.DrawVerticalLine($"macro-{_currentBar.OpenTime}", _currentBar.OpenTime, Color.Azure, 1, LineStyle.Dots);
            }

            if (_barOne.OpenTime.TimeOfDay == new TimeSpan(9, 30, 0))
            {
                _firstPresentationSet = false;
                
                Chart.DrawStraightLine("ny-open", _barOne.OpenTime, _barOne.Open, _barOne.OpenTime.EndOfDay(), _barOne.Open, "09:30AM", LineStyle.Dots, Color.Beige, true, true, true);

                if (ShowOpeningRangeGap)
                {
                    _timeService.GetOpeningRangeGap(MarketData, Chart, _currentBar);
                }
            }

            if (_currentBar.OpenTime.TimeOfDay == new TimeSpan(10, 00, 00))
            {
                var nineHour = _currentBar.OpenTime.AddMinutes(-30);
                var range = Bars.GetMinMax(nineHour, _currentBar.OpenTime);

                var min = Bars[range.minIndex];
                var max = Bars[range.maxIndex];

                Chart.DrawTrendLine($"nine-{min.OpenTime}", min.OpenTime, min.Low, nineHour.AddHours(2), min.Low,
                    Color.Pink);
                
                Chart.DrawTrendLine($"nine-{max.OpenTime}", max.OpenTime, max.High, nineHour.AddHours(2), max.High,
                    Color.Pink);
            }

        }
    }
}