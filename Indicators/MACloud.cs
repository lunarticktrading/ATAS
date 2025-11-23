using ATAS.Indicators;
using ATAS.Indicators.Drawing;
using ATAS.Indicators.Technical;
using LunarTick.ATAS.Indicators.Helpers;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using FilterColor = ATAS.Indicators.FilterColor;

namespace LunarTick.ATAS.Indicators
{
    [DisplayName("MACloud")]
    [Category("LunarTick-ATAS-Indicators")]
    public class MACloud : Indicator
    {
        #region Enums

        public enum MACloudDataSeriesIndexEnum
        {
            BullishCloudRangeDataSeries,
            BearishCloudRangeDataSeries,
            FastMAValueDataSeries,
            SlowMAValueDataSeries,
            BuySignalsValueDataSeries,
            SellSignalsValueDataSeries
        }

        public enum MATypeEnum
        {
            SMA,
            EMA
        }

        #endregion

        #region Constants

        const int DefaultSignalWidth = 2;
        const int DefaultSignalOffset = 1;

        #endregion

        #region Members

        private MATypeEnum _maType = MATypeEnum.EMA;
        private int _fastPeriod = 9;
        private int _slowPeriod = 21;
        private int _signalWidth = DefaultSignalWidth;
        private int _signalOffset = DefaultSignalOffset;
        private SMA _smaFast = new();
        private SMA _smaSlow = new();
        private EMA _emaFast = new();
        private EMA _emaSlow = new();
        private readonly RangeDataSeries _bullishCloudSeries = new("BullishCloud", "Bullish Cloud")
        {
            RangeColor = DefaultColors.Green.GetWithTransparency(60).Convert(),
            DrawAbovePrice = false
        };
        private readonly RangeDataSeries _bearishCloudSeries = new("BearishCloud", "Bearish Cloud")
        {
            RangeColor = DefaultColors.Red.GetWithTransparency(60).Convert(),
            DrawAbovePrice = false
        };
        private readonly ValueDataSeries _fastMASeries = new("FastMA", "Fast MA")
        {
            VisualType = VisualMode.Hide,
            Color = DefaultColors.Green.Convert(),
            Width = 1,
            DrawAbovePrice = false
        };
        private readonly ValueDataSeries _slowMASeries = new("SlowMA", "Slow MA")
        {
            VisualType = VisualMode.Hide,
            Color = DefaultColors.Red.Convert(),
            Width = 1,
            DrawAbovePrice = false
        };
        private readonly ValueDataSeries _buySignalsSeries = new("BuySignal", "Buy Signal")
        {
            VisualType = VisualMode.UpArrow,
            Color = DefaultColors.Green.Convert(),
            Width = DefaultSignalWidth,
            ShowTooltip = false
        };
        private readonly ValueDataSeries _sellSignalsSeries = new("SellSignal", "Sell Signal")
        {
            VisualType = VisualMode.DownArrow,
            Color = DefaultColors.Red.Convert(),
            Width = DefaultSignalWidth,
            ShowTooltip = false
        };
        private int _lastBar = 0;

        #endregion

        #region Properties

        [OFT.Attributes.Parameter]
        [Display(Name = "MA Type", GroupName = "MACloud - Settings", Description = "The type of Moving Average.", Order = 1001)]
        public MATypeEnum MAType
        {
            get => _maType;

            set
            {
                _maType = value;
                _fastMASeries.Name = $"{_fastPeriod} {_maType}";
                _slowMASeries.Name = $"{_slowPeriod} {_maType}";
                RecalculateValues();
            }
        }

        [OFT.Attributes.Parameter]
        [Display(Name = "Fast Period", GroupName = "MACloud - Settings", Description = "The period of the fast Moving Average.", Order = 1002)]
        [Range(1, 1000)]
        public int FastPeriod
        {
            get => _fastPeriod;

            set
            {
                _fastPeriod = value;
                _smaFast.Period = value;
                _emaFast.Period = value;
                _fastMASeries.Name = $"{_fastPeriod} {_maType}";
                RecalculateValues();
            }
        }

        [OFT.Attributes.Parameter]
        [Display(Name = "Slow Period", GroupName = "MACloud - Settings", Description = "The period of the slow Moving Average.", Order = 1003)]
        [Range(1, 1000)]
        public int SlowPeriod
        {
            get => _slowPeriod;

            set
            {
                _slowPeriod = value;
                _smaSlow.Period = value;
                _emaSlow.Period = value;
                _slowMASeries.Name = $"{_slowPeriod} {_maType}";
                RecalculateValues();
            }
        }

        [Display(Name = "Buy Signal", GroupName = "MACloud - Signals", Description = "When enabled, buy signals will be highlighted in the specified color.", Order = 1101)]
        public FilterColor BuySignalColor { get; set; }

        [Display(Name = "Sell Signal", GroupName = "MACloud - Signals", Description = "When enabled, sell signals will be highlighted in the specified color.", Order = 1102)]
        public FilterColor SellSignalColor { get; set; }

        [Display(Name = "Width", GroupName = "MACloud - Signals", Description = "Controls the size of the signals on the chart.", Order = 1103)]
        [Range(0, 1000)]
        public int SignalWidth
        {
            get => _signalWidth;

            set
            {
                _signalWidth = value;
                _buySignalsSeries.Width = value;
                _sellSignalsSeries.Width = value;
                RecalculateValues();
            }
        }

        [Display(Name = "Offset", GroupName = "MACloud - Signals", Description = "The vertical offset between signal and bar.", Order = 1104)]
        [Range(0, 1000)]
        public int SignalOffset
        {
            get => _signalOffset;

            set
            {
                _signalOffset = value;
                RecalculateValues();
            }
        }

        [Display(Name = "Buy Alerts", GroupName = "MACloud - Alerts", Description = "When enabled, an alert is triggered when price crosses and closes above the Moving Average, using the specified sound file.", Order = 1201)]
        public FilterString BuyAlertFilter { get; set; }

        [Display(Name = "Sell Alerts", GroupName = "MACloud - Alerts", Description = "When enabled, an alert is triggered when price crosses and closes below the Moving Average, using the specified sound file.", Order = 1202)]
        public FilterString SellAlertFilter { get; set; }

        [Display(Name = "Alert Sounds Path", GroupName = "MACloud - Alerts", Description = "Location of alert audio files.", Order = 1203)]
        public string AlertSoundsPath { get; set; }

        #endregion

        #region Constructor

        public MACloud()
        {
            DenyToChangePanel = true;

            // NOTE: The DataSeries must match the order found in MACloudDataSeriesIndexEnum.
            DataSeries[0] = _bullishCloudSeries;
            DataSeries.Add(_bearishCloudSeries);
            DataSeries.Add(_fastMASeries);
            DataSeries.Add(_slowMASeries);
            DataSeries.Add(_buySignalsSeries);
            DataSeries.Add(_sellSignalsSeries);

            MAType = MATypeEnum.EMA;
            FastPeriod = 9;
            SlowPeriod = 21;

            BuySignalColor = new(true) { Enabled = false, Value = DefaultColors.Green.Convert() };
            SellSignalColor = new(true) { Enabled = false, Value = DefaultColors.Red.Convert() };
            SignalWidth = DefaultSignalWidth;
            SignalOffset = DefaultSignalOffset;

            BuySignalColor.PropertyChanged += BuySignalFilterPropertyChanged;
            SellSignalColor.PropertyChanged += SellSignalFilterPropertyChanged;

            BuyAlertFilter = new(true) { Enabled = false, Value = "BuySignal.wav" };
            SellAlertFilter = new(true) { Enabled = false, Value = "SellSignal.wav" };
            AlertSoundsPath = SoundPackHelper.DefaultAlertFilePath();

            Add(_smaFast);
            Add(_smaSlow);
            Add(_emaFast);
            Add(_emaSlow);
        }

        #endregion

        #region Indicator methods

        protected override void OnRecalculate()
        {
            base.OnRecalculate();

            DataSeries.ForEach(ds => ds.Clear());

            _smaFast.Period = FastPeriod;
            _smaSlow.Period = SlowPeriod;
            _emaFast.Period = FastPeriod;
            _emaSlow.Period = SlowPeriod;
        }

        protected override void OnCalculate(int bar, decimal value)
        {
            if (bar == 0)
                return;

            if (CurrentBar < Math.Max(_fastPeriod, _slowPeriod) || InstrumentInfo is null)
                return;

            // Alerts
            if ((_lastBar != bar) && (bar == CurrentBar - 1))
            {
                if (BuyAlertFilter.Enabled && _buySignalsSeries[bar - 1] != 0)
                {
                    string audioFile = SoundPackHelper.ResolveAlertFilePath(BuyAlertFilter.Value, AlertSoundsPath);
                    AddAlert(audioFile, InstrumentInfo.Instrument, $"BUY SIGNAL: {MAType} Cloud turned bullish", DefaultColors.Black.Convert(), _buySignalsSeries.ValuesColor.Convert());
                }
                if (SellAlertFilter.Enabled && _sellSignalsSeries[bar - 1] != 0)
                {
                    string audioFile = SoundPackHelper.ResolveAlertFilePath(SellAlertFilter.Value, AlertSoundsPath);
                    AddAlert(audioFile, InstrumentInfo.Instrument, $"SELL SIGNAL: {MAType} Cloud turned bearish", DefaultColors.Black.Convert(), _sellSignalsSeries.ValuesColor.Convert());
                }
            }

            decimal fastValue = 0m;
            decimal slowValue = 0m;
            if (MAType == MATypeEnum.EMA)
            {
                fastValue = _emaFast[bar];
                slowValue = _emaSlow[bar];
            }
            else
            {
                fastValue = _smaFast[bar];
                slowValue = _smaSlow[bar];
            }
            _fastMASeries[bar] = fastValue;
            _slowMASeries[bar] = slowValue;
            if (fastValue >= slowValue || (_fastMASeries[bar - 1] > _slowMASeries[bar - 1]))
            {
                // Bullish cloud
                _bullishCloudSeries[bar].Upper = fastValue;
                _bullishCloudSeries[bar].Lower = slowValue;
            }
            if (fastValue <= slowValue || (_fastMASeries[bar - 1] < _slowMASeries[bar - 1]))
            {
                // Bearish cloud
                _bearishCloudSeries[bar].Upper = slowValue;
                _bearishCloudSeries[bar].Lower = fastValue;
            }

            if (Visible)
            {
                // Signals
                var prevCandle = GetCandle(bar - 1);
                var currCandle = GetCandle(bar);

                bool buySignal = BuySignalColor.Enabled && ((_fastMASeries[bar] > _slowMASeries[bar]) && (_fastMASeries[bar - 1] <= _slowMASeries[bar - 1]));
                _buySignalsSeries[bar] = buySignal ? (currCandle.Low - (InstrumentInfo.TickSize * SignalOffset)) : 0;

                bool sellSignal = SellSignalColor.Enabled && ((_fastMASeries[bar] < _slowMASeries[bar]) && (_fastMASeries[bar - 1] >= _slowMASeries[bar - 1]));
                _sellSignalsSeries[bar] = sellSignal ? (currCandle.High + (InstrumentInfo.TickSize * SignalOffset)) : 0;
            }

            _lastBar = bar;
        }

        #endregion

        #region Private methods

        private void BuySignalFilterPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            _buySignalsSeries.Color = BuySignalColor.Value;
            RecalculateValues();
        }

        private void SellSignalFilterPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            _sellSignalsSeries.Color = SellSignalColor.Value;
            RecalculateValues();
        }

        #endregion
    }
}
