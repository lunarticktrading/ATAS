using ATAS.Indicators;
using ATAS.Indicators.Drawing;
using ATAS.Indicators.Technical;
using LunarTick.ATAS.Indicators.Helpers;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Utils.Common.Logging;
using FilterColor = ATAS.Indicators.FilterColor;

namespace LunarTick.ATAS.Indicators
{
    [DisplayName("Engulfing Off BB")]
    [Category("LunarTick-ATAS-Indicators")]
    public class EngulfingOffBB : Indicator
    {
        #region Enums

        public enum EngulfingOffBBDataSeriesIndexEnum
        {
            PaintbarsDataSeries,
            EngulfingSeries
        }

        #endregion

        #region Members

        private int _bbPeriod = 20;
        private decimal _bbStdDevMultiplier = 2.0M;
        private BollingerBands _bb = new();
        private readonly PaintbarsDataSeries _bars = new("Bars", "Bars")
        {
            IsHidden = true
        };
        private readonly ValueDataSeries _engulfingSeries = new("Engulfing")
        {
            VisualType = VisualMode.Hide,
            IsHidden = true,
            IgnoredByAlerts = true
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

        [Display(Name = "Bullish Engulfing Off BB", GroupName = "Display", Description = "When enabled, displays bullish engulfing candles off the lower BB, using the specified color.", Order = 101)]
        public FilterColor BullishEngulfingOffBBColorFilter { get; set; }

        [Display(Name = "Bearish Engulfing Off BB", GroupName = "Display", Description = "When enabled, displays bearish engulfing candles off the upper BB, using the specified color.", Order = 102)]
        public FilterColor BearishEngulfingOffBBColorFilter { get; set; }


        [Display(Name = "Bullish Engulfing Off BB Alerts", GroupName = "Alerts", Description = "When enabled, an alert is triggered for bullish engulfing candles off the lower BB, using the specified sound file.", Order = 201)]
        public FilterString BullishEngulfingOffBBAlertFilter { get; set; }

        [Display(Name = "Bearish Engulfing Off BB Alerts", GroupName = "Alerts", Description = "When enabled, an alert is triggered for bearish engulfing candles off the upper BB, using the specified sound file.", Order = 202)]
        public FilterString BearishEngulfingOffBBAlertFilter { get; set; }

        [Display(Name = "Alert Sounds Path", GroupName = "Alerts", Description = "Location of alert audio files.", Order = 203)]
        public string AlertSoundsPath { get; set; }

        #endregion

        #region Constructor

        public EngulfingOffBB()
        {
            DenyToChangePanel = true;

            // NOTE: The DataSeries must match the order found in EngulfingOffBBDataSeriesIndexEnum.
            DataSeries[0] = _bars;
            DataSeries.Add(_engulfingSeries);

            BBPeriod = 20;
            BBStdDevMultiplier = 2.0M;

            BullishEngulfingOffBBColorFilter = new(true) { Enabled = true, Value = DefaultColors.Aqua.Convert() };
            BearishEngulfingOffBBColorFilter = new(true) { Enabled = true, Value = DefaultColors.Fuchsia.Convert() };

            BullishEngulfingOffBBAlertFilter = new(true) { Enabled = false, Value = "EngulfingBB.wav" };
            BearishEngulfingOffBBAlertFilter = new(true) { Enabled = false, Value = "EngulfingBB.wav" };
            AlertSoundsPath = SoundPackHelper.DefaultAlertFilePath();

            BullishEngulfingOffBBColorFilter.PropertyChanged += OnFilterPropertyChanged;
            BearishEngulfingOffBBColorFilter.PropertyChanged += OnFilterPropertyChanged;
        }

        #endregion

        #region Indicator methods

        protected override void OnRecalculate()
        {
            base.OnRecalculate();

            DataSeries.ForEach(ds => ds.Clear());

            _bb = new() { Period = _bbPeriod, Width = _bbStdDevMultiplier };
            _lastBar = 0;
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

            if (BullishEngulfingOffBBColorFilter.Enabled)
            {
                if ((candle1.Close > candle1.Open) && (candle1.Low < (decimal)bbLower[barIdx1]) && (candle1.Close > (decimal)bbLower[barIdx1]) && (candle2.Close < candle2.Open) && ((candle1.Close - candle1.Open) > (candle2.Open - candle2.Close)))
                {
                    _engulfingSeries[barIdx1] = 1;
                    _bars[barIdx1] = BullishEngulfingOffBBColorFilter.Value;
                }
            }

            if (BearishEngulfingOffBBColorFilter.Enabled)
            {
                if ((candle1.Close < candle1.Open) && (candle1.High > (decimal)bbUpper[barIdx1]) && (candle1.Close < (decimal)bbUpper[barIdx1]) && (candle2.Close > candle2.Open) && ((candle1.Open - candle1.Close) > (candle2.Close - candle2.Open)))
                {
                    _engulfingSeries[barIdx1] = -1;
                    _bars[barIdx1] = BearishEngulfingOffBBColorFilter.Value;
                }
            }


            // Alerts
            if (bar == CurrentBar - 1)
            {
                if (BullishEngulfingOffBBAlertFilter.Enabled && _engulfingSeries[barIdx1] > 0)
                {
                    string audioFile = SoundPackHelper.ResolveAlertFilePath(BullishEngulfingOffBBAlertFilter.Value, AlertSoundsPath);
                    AddAlert(audioFile, InstrumentInfo.Instrument, $"Detected bullish engulfing candle off lower BB", DefaultColors.Black.Convert(), BullishEngulfingOffBBColorFilter.Value);
                }
                if (BearishEngulfingOffBBAlertFilter.Enabled && _engulfingSeries[barIdx1] < 0)
                {
                    string audioFile = SoundPackHelper.ResolveAlertFilePath(BearishEngulfingOffBBAlertFilter.Value, AlertSoundsPath);
                    AddAlert(audioFile, InstrumentInfo.Instrument, $"Detected bearish engulfing candle off upper BB", DefaultColors.Black.Convert(), BearishEngulfingOffBBColorFilter.Value);
                }
            }

            _lastBar = bar;
        }

        #endregion

        #region Private methods

        private void OnFilterPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RecalculateValues();
        }

        #endregion
    }
}
