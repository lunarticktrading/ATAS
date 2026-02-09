using ATAS.Indicators;
using ATAS.Indicators.Drawing;
using LunarTick.ATAS.Indicators.Helpers;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using FilterColor = ATAS.Indicators.FilterColor;

namespace LunarTick.ATAS.Indicators
{
    [DisplayName("Volume Imbalances")]
    [Category("LunarTick-ATAS-Indicators")]
    public class VolumeImbalances : Indicator
    {
        #region Enums

        public enum VolumeImbalancesDataSeriesIndexEnum
        {
            BullishVolumeImbalanceDotsSeries,
            BearishVolumeImbalanceDotsSeries
        }

        #endregion

        #region Constants

        private const int MinTicks = 1;

        #endregion

        #region Members

        private int _minTicks = 1;
        private int _lastBar = 0;
        private int _lastAlertBar = 0;
        private LineTillTouch? _currentBullishVolumeImbalance = null;
        private LineTillTouch? _currentBearishVolumeImbalance = null;
        private readonly ValueDataSeries _bullishVolumeImbalanceDotsSeries = new("BullishVolumeImbalanceDots")
        {
            VisualType = VisualMode.Dots,
            Width = 8,
            Color = DefaultColors.Aqua.Convert(),
            IsHidden = true,
            IgnoredByAlerts = true
        };
        private readonly ValueDataSeries _bearishVolumeImbalanceDotsSeries = new("BearishVolumeImbalanceDots")
        {
            VisualType = VisualMode.Dots,
            Width = 8,
            Color = DefaultColors.Fuchsia.Convert(),
            IsHidden = true,
            IgnoredByAlerts = true
        };
        private int _lineWidth = 6;
        private int _dotWidth = 6;

        #endregion

        #region Properties

        [Display(Name = "Bullish Volume Imbalance", GroupName = "Display", Description = "When enabled, displays bullish volume imbalances, using the specified color.", Order = 101)]
        public FilterColor BullishVolumeImbalanceColorFilter { get; set; }

        [Display(Name = "Bearish Volume Imbalance", GroupName = "Display", Description = "When enabled, displays bearish volume imbalances, using the specified color.", Order = 102)]
        public FilterColor BearishVolumeImbalanceColorFilter { get; set; }

        [Display(Name = "Line Width", GroupName = "Display", Description = "Controls the width of the volume imbalance lines on the chart.", Order = 103)]
        [Range(1, 100)]
        public int LineWidth
        {
            get => _lineWidth;

            set
            {
                _lineWidth = value;
                RecalculateValues();
            }
        }

        [Display(Name = "Dot Width", GroupName = "Display", Description = "Controls the size of the volume imbalance dots on the chart.", Order = 104)]
        [Range(1, 100)]
        public int DotWidth
        {
            get => _dotWidth;

            set
            {
                _dotWidth = value;
                _bullishVolumeImbalanceDotsSeries.Width = value;
                _bearishVolumeImbalanceDotsSeries.Width = value;
                RecalculateValues();
            }
        }

        [Display(Name = "Bullish Volume Imbalance Alerts", GroupName = "Alerts", Description = "When enabled, an alert is triggered for detected bullish volume imbalances, using the specified sound file.", Order = 201)]
        public FilterString BullishVolumeImbalanceAlertFilter { get; set; }

        [Display(Name = "Bearish Volume Imbalance Alerts", GroupName = "Alerts", Description = "When enabled, an alert is triggered for detected bearish volume imbalances, using the specified sound file.", Order = 202)]
        public FilterString BearishVolumeImbalanceAlertFilter { get; set; }

        [Display(Name = "Alert Sounds Path", GroupName = "Alerts", Description = "Location of alert audio files.", Order = 203)]
        public string AlertSoundsPath { get; set; }

        #endregion

        #region Constructor

        public VolumeImbalances()
        {
            DenyToChangePanel = true;

            // NOTE: The DataSeries must match the order found in VolumeImbalancesDataSeriesIndexEnum.
            DataSeries[0] = _bullishVolumeImbalanceDotsSeries;
            DataSeries.Add(_bearishVolumeImbalanceDotsSeries);

            BullishVolumeImbalanceColorFilter = new(true) { Enabled = true, Value = DefaultColors.Aqua.Convert() };
            BearishVolumeImbalanceColorFilter = new(true) { Enabled = true, Value = DefaultColors.Fuchsia.Convert() };
            LineWidth = 6;
            DotWidth = 8;

            BullishVolumeImbalanceAlertFilter = new(true) { Enabled = false, Value = "VolumeImbalance.wav" };
            BearishVolumeImbalanceAlertFilter = new(true) { Enabled = false, Value = "VolumeImbalance.wav" };
            AlertSoundsPath = SoundPackHelper.DefaultAlertFilePath();

            BullishVolumeImbalanceColorFilter.PropertyChanged += OnFilterPropertyChanged;
            BearishVolumeImbalanceColorFilter.PropertyChanged += OnFilterPropertyChanged;
        }

        #endregion

        #region Indicator methods

        protected override void OnRecalculate()
        {
            base.OnRecalculate();

            DataSeries.ForEach(ds => ds.Clear());

            HorizontalLinesTillTouch.Clear();
            _currentBullishVolumeImbalance = null;
            _currentBearishVolumeImbalance = null;

            _lastBar = 0;
            _lastAlertBar = 0;
        }

        protected override void OnCalculate(int bar, decimal value)
        {
            if (bar < 1)
                return;

            if (InstrumentInfo is null)
                return;

            if (bar != _lastBar)
            {
                // New bar started.
                _currentBullishVolumeImbalance = null;
                _currentBearishVolumeImbalance = null;
            }

            int barIdx0 = bar;
            int barIdx1 = bar - 1;

            var candle0 = GetCandle(barIdx0);
            var candle1 = GetCandle(barIdx1);


            // Signals

            if (BullishVolumeImbalanceColorFilter.Enabled)
            {
                if ((candle1.Close > candle1.Open) && (candle0.Close > candle0.Open) && (((candle0.Open - candle1.Close) / InstrumentInfo.TickSize) >= MinTicks))
                {
                    if (_currentBullishVolumeImbalance == null)
                    {
                        _currentBullishVolumeImbalance = new LineTillTouch(bar, candle0.Open, new System.Drawing.Pen(BullishVolumeImbalanceColorFilter.Value.Convert(), LineWidth));
                        HorizontalLinesTillTouch.Add(_currentBullishVolumeImbalance);
                        _bullishVolumeImbalanceDotsSeries[bar] = candle0.Low - InstrumentInfo.TickSize;
                    }
                }
                else if (_currentBullishVolumeImbalance != null)
                {
                    HorizontalLinesTillTouch.Remove(_currentBullishVolumeImbalance);
                    _currentBullishVolumeImbalance = null;
                    _bullishVolumeImbalanceDotsSeries[bar] = 0;
                }
            }

            if (BearishVolumeImbalanceColorFilter.Enabled)
            {
                if ((candle1.Close < candle1.Open) && (candle0.Close < candle0.Open) && (((candle1.Close - candle0.Open) / InstrumentInfo.TickSize) >= MinTicks))
                {
                    if (_currentBearishVolumeImbalance == null)
                    {
                        _currentBearishVolumeImbalance = new LineTillTouch(bar, candle0.Open, new System.Drawing.Pen(BearishVolumeImbalanceColorFilter.Value.Convert(), LineWidth));
                        HorizontalLinesTillTouch.Add(_currentBearishVolumeImbalance);
                        _bearishVolumeImbalanceDotsSeries[bar] = candle0.High + InstrumentInfo.TickSize;
                    }
                }
                else if (_currentBearishVolumeImbalance != null)
                {
                    HorizontalLinesTillTouch.Remove(_currentBearishVolumeImbalance);
                    _currentBearishVolumeImbalance = null;
                    _bearishVolumeImbalanceDotsSeries[bar] = 0;
                }
            }


            // Alerts
            if (bar == CurrentBar - 1)
            {
                if (BullishVolumeImbalanceAlertFilter.Enabled && _currentBullishVolumeImbalance != null && bar != _lastAlertBar)
                {
                    string audioFile = SoundPackHelper.ResolveAlertFilePath(BullishVolumeImbalanceAlertFilter.Value, AlertSoundsPath);
                    AddAlert(audioFile, InstrumentInfo.Instrument, $"Detected bullish volume imbalance", DefaultColors.Black.Convert(), BullishVolumeImbalanceColorFilter.Value);
                    _lastAlertBar = bar;
                }
                if (BearishVolumeImbalanceAlertFilter.Enabled && _currentBearishVolumeImbalance != null && bar != _lastAlertBar)
                {
                    string audioFile = SoundPackHelper.ResolveAlertFilePath(BearishVolumeImbalanceAlertFilter.Value, AlertSoundsPath);
                    AddAlert(audioFile, InstrumentInfo.Instrument, $"Detected bearish volume imbalance", DefaultColors.Black.Convert(), BearishVolumeImbalanceColorFilter.Value);
                    _lastAlertBar = bar;
                }
            }

            _lastBar = bar;
        }

        #endregion

        #region Private methods

        private void OnFilterPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is null)
                return;

            if (sender is not FilterColor)
                return;

            if (e.PropertyName is null)
                return;

            if ((FilterColor)sender == BullishVolumeImbalanceColorFilter)
            {
                if (e.PropertyName.Equals("Value"))
                    _bullishVolumeImbalanceDotsSeries.Color = BullishVolumeImbalanceColorFilter.Value;
            }
            else if ((FilterColor)sender == BearishVolumeImbalanceColorFilter)
            {
                if (e.PropertyName.Equals("Value"))
                    _bearishVolumeImbalanceDotsSeries.Color = BearishVolumeImbalanceColorFilter.Value;
            }

            RecalculateValues();
        }

        #endregion
    }
}
