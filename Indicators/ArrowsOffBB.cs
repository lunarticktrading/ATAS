using ATAS.Indicators;
using ATAS.Indicators.Drawing;
using ATAS.Indicators.Technical;
using LunarTick.ATAS.Indicators.Helpers;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LunarTick.ATAS.Indicators
{
    [DisplayName("Arrows Off BB")]
    [Category("LunarTick-ATAS-Indicators")]
    public class ArrowsOffBB : Indicator
    {
        #region Enums

        public enum ArrowsOffBBDataSeriesIndexEnum
        {
            BuySignalValueDataSeries,
            SellSignalValueDataSeries
        }

        #endregion

        #region Constants

        const int DefaultSignalWidth = 2;
        const int DefaultSignalOffset = 1;

        #endregion

        #region Members

        private int _bbPeriod = 20;
        private decimal _bbStdDevMultiplier = 2.0M;
        private int _signalWidth = DefaultSignalWidth;
        private int _signalOffset = DefaultSignalOffset;
        private BollingerBands _bb = new();
        private readonly ValueDataSeries _buySignalsSeries = new("BuySignal", "Buy Signal")
        {
            VisualType = VisualMode.UpArrow,
            Color = DefaultColors.Aqua.Convert(),
            Width = DefaultSignalWidth,
            ShowTooltip = false
        };
        private readonly ValueDataSeries _sellSignalsSeries = new("SellSignal", "Sell Signal")
        {
            VisualType = VisualMode.DownArrow,
            Color = DefaultColors.Fuchsia.Convert(),
            Width = DefaultSignalWidth,
            ShowTooltip = false
        };
        private int _lastBar = 0;

        #endregion

        #region Properties

        [OFT.Attributes.Parameter]
        [Display(Name = "BB Period", GroupName = "Settings", Description = "Bollinger Bands period.", Order = 001)]
        [Range(1, 1000)]
        public int BBPeriod
        {
            get => _bbPeriod;

            set
            {
                _bbPeriod = value;
                RecalculateValues();
            }
        }

        [OFT.Attributes.Parameter]
        [Display(Name = "BB Std Dev Multiplier", GroupName = "Settings", Description = "Bollinger Bands std deviation multiplier.", Order = 002)]
        [Range(0, 10)]
        public decimal BBStdDevMultiplier
        {
            get => _bbStdDevMultiplier;

            set
            {
                _bbStdDevMultiplier = value;
                RecalculateValues();
            }
        }

        [Display(Name = "Width", GroupName = "Signals", Description = "Controls the size of the signals on the chart.", Order = 101)]
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

        [Display(Name = "Offset", GroupName = "Signals", Description = "The vertical offset between signal and bar.", Order = 102)]
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

        [Display(Name = "Buy Alerts", GroupName = "Alerts", Description = "When enabled, an alert is triggered when price crosses and closes above the Moving Average, using the specified sound file.", Order = 201)]
        public FilterString BuyAlertFilter { get; set; }

        [Display(Name = "Sell Alerts", GroupName = "Alerts", Description = "When enabled, an alert is triggered when price crosses and closes below the Moving Average, using the specified sound file.", Order = 202)]
        public FilterString SellAlertFilter { get; set; }

        [Display(Name = "Alert Sounds Path", GroupName = "Alerts", Description = "Location of alert audio files.", Order = 203)]
        public string AlertSoundsPath { get; set; }

        #endregion

        #region Constructor

        public ArrowsOffBB()
        {
            DenyToChangePanel = true;

            // NOTE: The DataSeries must match the order found in ArrowsOffBBDataSeriesIndexEnum.
            DataSeries[0] = _buySignalsSeries;
            DataSeries.Add(_sellSignalsSeries);

            BBPeriod = 20;
            BBStdDevMultiplier = 2.0M;
            SignalWidth = DefaultSignalWidth;
            SignalOffset = DefaultSignalOffset;

            BuyAlertFilter = new(true) { Enabled = false, Value = "BuySignal.wav" };
            SellAlertFilter = new(true) { Enabled = false, Value = "SellSignal.wav" };
            AlertSoundsPath = SoundPackHelper.DefaultAlertFilePath();
        }

        #endregion

        #region Indicator methods

        protected override void OnRecalculate()
        {
            base.OnRecalculate();

            DataSeries.ForEach(ds => ds.Clear());

            _bb = new() { Period = _bbPeriod, Width = _bbStdDevMultiplier };
        }

        protected override void OnCalculate(int bar, decimal value)
        {
            if (bar < 2)
                return;

            if (CurrentBar < (BBPeriod + 1) || InstrumentInfo is null)
                return;

            // Wait for new bar
            if (bar == _lastBar)
                return;

            int barIdx0 = bar;
            int barIdx1 = bar - 1;
            int barIdx2 = bar - 2;

            var candle0 = GetCandle(barIdx0);
            var candle1 = GetCandle(barIdx1);
            var candle2 = GetCandle(barIdx2);

            _bb.Calculate(bar, value);


            // Signals
            IDataSeries bbUpper = _bb.DataSeries[1];
            IDataSeries bbLower = _bb.DataSeries[2];

            bool buySignal = (candle1.Close > candle1.Open) && (candle2.Close < candle2.Open) && (candle1.Low < (decimal)bbLower[barIdx1]) && (candle2.Low < (decimal)bbLower[barIdx2]);
            _buySignalsSeries[barIdx1] = buySignal ? (candle1.Low - (InstrumentInfo.TickSize * SignalOffset)) : 0;

            bool sellSignal = (candle1.Close < candle1.Open) && (candle2.Close > candle2.Open) && (candle1.High > (decimal)bbUpper[barIdx1]) && (candle2.High > (decimal)bbUpper[barIdx2]);
            _sellSignalsSeries[barIdx1] = sellSignal ? (candle1.High + (InstrumentInfo.TickSize * SignalOffset)) : 0;


            // Alerts
            if (bar == CurrentBar - 1)
            {
                if (BuyAlertFilter.Enabled && _buySignalsSeries[barIdx1] != 0)
                {
                    string audioFile = SoundPackHelper.ResolveAlertFilePath(BuyAlertFilter.Value, AlertSoundsPath);
                    AddAlert(audioFile, InstrumentInfo.Instrument, $"Detected bullish entry signal off lower BB", DefaultColors.Black.Convert(), _buySignalsSeries.ValuesColor.Convert());
                }
                if (SellAlertFilter.Enabled && _sellSignalsSeries[barIdx1] != 0)
                {
                    string audioFile = SoundPackHelper.ResolveAlertFilePath(SellAlertFilter.Value, AlertSoundsPath);
                    AddAlert(audioFile, InstrumentInfo.Instrument, $"Detected bearish entry signal off upper BB", DefaultColors.Black.Convert(), _sellSignalsSeries.ValuesColor.Convert());
                }
            }

            _lastBar = bar;
        }

        #endregion
    }
}
