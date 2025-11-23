using ATAS.Indicators;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace LunarTick.ATAS.Indicators
{
    [DisplayName("HeikenAshi")]
    [Category("LunarTick-ATAS-Indicators")]
    public class HeikenAshi : RealOpenClose
    {
        #region Enums

        public enum HeikenAshiDataSeriesIndexEnum
        {
            // Added by HeikenAshi.
            PaintbarsDataSeries,
            HeikenAshiCandleDataSeries,
            // Inherited from RealOpenClose.
            RealOpenValueDataSeries,
            RealCloseValueDataSeries
        }

        #endregion

        #region Members

        private readonly Color _transparent = System.Drawing.Color.Transparent;
        private readonly PaintbarsDataSeries _bars = new("Bars", "Bars")
        {
            IsHidden = true
        };
        private readonly CandleDataSeries _candles = new("Candles", "Heiken Ashi")
        {
            DrawAbovePrice = true,
            ShowCurrentValue = false,
            ShowTooltip = false
        };
        private int _days;
        private int _targetBar;

        #endregion

        #region Properties

        [Display(Name = "Days Look Back", GroupName = "Settings", Order = 001)]
        [Range(0, Int32.MaxValue)]
        public int Days
        {
            get => _days;

            set
            {
                if (value < 0)
                    return;

                _days = value;
                RecalculateValues();
            }
        }

        #endregion

        #region Constructor

        public HeikenAshi()
        {
            DenyToChangePanel = true;

            // We are inheriting from the RealOpenClose indicator, but we need the HA candles drawn first, so insert at the beginning.
            DataSeries.Insert(0, _bars);
            DataSeries.Insert(1, _candles);

            Days = 20;
        }

        #endregion

        #region Indicator methods

        protected override void OnApplyDefaultColors()
        {
            if (ChartInfo is null)
                return;

            _candles.UpCandleColor = ChartInfo.ColorsStore.UpCandleColor.Convert();
            _candles.DownCandleColor = ChartInfo.ColorsStore.DownCandleColor.Convert();
            _candles.BorderColor = ChartInfo.ColorsStore.BarBorderPen.Color.Convert();
        }

        protected override void OnRecalculate()
        {
            base.OnRecalculate();

            DataSeries.ForEach(ds => ds.Clear());
        }

        protected override void OnCalculate(int bar, decimal value)
        {
            base.OnCalculate(bar, value);

            // Make the standard bars transparent.
            _bars[bar] = _transparent.Convert();

            if (bar == 0)
            {
                if (_days > 0)
                {
                    var days = 0;

                    for (var i = CurrentBar - 1; i >= 0; i--)
                    {
                        _targetBar = i;

                        if (!IsNewSession(i))
                            continue;

                        days++;

                        if (days == _days)
                            break;
                    }
                }
            }

            if (bar < _targetBar)
                return;

            if (bar == _targetBar)
            {
                var candle = GetCandle(bar);

                _candles[bar] = new Candle
                {
                    Close = candle.Close,
                    High = candle.High,
                    Low = candle.Low,
                    Open = candle.Open
                };
            }
            else
            {
                var candle = GetCandle(bar);
                var prevCandle = _candles[bar - 1];
                var close = (candle.Open + candle.Close + candle.High + candle.Low) * 0.25m;
                var open = (prevCandle.Open + prevCandle.Close) * 0.5m;
                var high = Math.Max(Math.Max(close, open), candle.High);
                var low = Math.Min(Math.Min(close, open), candle.Low);

                _candles[bar] = new Candle
                {
                    Close = close,
                    High = high,
                    Low = low,
                    Open = open
                };
            }
        }

        #endregion
    }
}
