using ATAS.Indicators;
using ATAS.Indicators.Drawing;
using OFT.Rendering.Context;
using OFT.Rendering.Tools;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using static LunarTick.ATAS.Indicators.HeikenAshi;

namespace LunarTick.ATAS.Indicators
{
    [DisplayName("HeikenAshiDots")]
    [Category("LunarTick-ATAS-Indicators")]
    public class HeikenAshiDots : Indicator
    {
        #region Enums

        public enum HeikenAshiDotsDataSeriesIndexEnum
        {
            HeikenAshiDotsValueDataSeries
        }

        #endregion

        #region Constants

        const decimal DefaultDisplayLevel = 50m;
        const int DefaultDisplayWidth = 5;

        #endregion

        #region Members

        private readonly LunarTick.ATAS.Indicators.HeikenAshi _ha = new();
        private decimal _displayLevel = DefaultDisplayLevel;
        private int _displayWidth = DefaultDisplayWidth;
        private Color _haDotsBullishColor = DefaultColors.Green;
        private Color _haDotsChangingColor = DefaultColors.Yellow;
        private Color _haDotsBearishColor = DefaultColors.Red;
        private readonly ValueDataSeries _haDotsSeries = new("HeikenAshiDots", "Heiken Ashi Dots")
        {
            VisualType = VisualMode.Dots,
            ShowZeroValue = false,
            ShowCurrentValue = false,
            ShowTooltip = false,
            Color = DefaultColors.White.Convert(),
            Width = DefaultDisplayWidth,
            DrawAbovePrice = false,
            IsHidden = true
        };
        private bool _showLabel;

        #endregion

        #region Properties

        [Display(Name = "Display Level", GroupName = "Display", Description = "Value level at which the row of dots will be displayed.", Order = 101)]
        public decimal DisplayLevel
        {
            get => _displayLevel;

            set
            {
                _displayLevel = value;
                RecalculateValues();
            }
        }

        [Display(Name = "Display Width", GroupName = "Display", Description = "Width of dots.", Order = 102)]
        public int DisplayWidth
        {
            get => _displayWidth;

            set
            {
                _displayWidth = value;
                _haDotsSeries.Width = value;
                RecalculateValues();
            }
        }

        [Display(Name = "Bullish Trend Color", GroupName = "Display", Description = "Dot color used to indicate a bullish trend.", Order = 105)]
        public Color BullishTrendColor
        {
            get => _haDotsBullishColor;

            set
            {
                _haDotsBullishColor = value;
                RecalculateValues();
            }
        }

        [Display(Name = "Changing Trend Color", GroupName = "Display", Description = "Dot color used to indicate a changing trend.", Order = 106)]
        public Color ChangingTrendColor
        {
            get => _haDotsChangingColor;

            set
            {
                _haDotsChangingColor = value;
                RecalculateValues();
            }
        }

        [Display(Name = "Bearish Trend Color", GroupName = "Display", Description = "Dot color used to indicate a bearish trend.", Order = 107)]
        public Color BearishTrendColor
        {
            get => _haDotsBearishColor;

            set
            {
                _haDotsBearishColor = value;
                RecalculateValues();
            }
        }

        [Display(Name = "Show Label", GroupName = "Display", Order = 108)]
        public bool ShowLabel
        {
            get => _showLabel;

            set
            {
                _showLabel = value;
                RecalculateValues();
            }
        }

        #endregion

        #region Constructor

        public HeikenAshiDots() : base(true)
        {
            Panel = IndicatorDataProvider.NewPanel;
            EnableCustomDrawing = true;
            SubscribeToDrawingEvents(DrawingLayouts.LatestBar);

            // NOTE: The DataSeries must match the order found in HeikenAshiDotsDataSeriesIndexEnum.
            DataSeries[0] = _haDotsSeries;

            DisplayLevel = DefaultDisplayLevel;
            DisplayWidth = DefaultDisplayWidth;
            BullishTrendColor = DefaultColors.Green;
            ChangingTrendColor = DefaultColors.Yellow;
            BearishTrendColor = DefaultColors.Red;
            ShowLabel = true;

            Add(_ha);
        }

        #endregion

        #region Indicator methods

        protected override void OnRecalculate()
        {
            base.OnRecalculate();

            DataSeries.ForEach(ds => ds.Clear());
        }

        protected override void OnCalculate(int bar, decimal value)
        {
            if (InstrumentInfo is null)
                return;

            if (CurrentBar < 3)
                return;

            var haCandles = (CandleDataSeries)(_ha.DataSeries[(int)HeikenAshiDataSeriesIndexEnum.HeikenAshiCandleDataSeries]);
            var prevHACandle = haCandles[bar-1];
            var currHACandle = haCandles[bar];
            var prevCandleTrend = (prevHACandle.Close >= prevHACandle.Open) ? 1 : -1;
            var currCandleTrend = (currHACandle.Close >= currHACandle.Open) ? 1 : -1;
            _haDotsSeries[bar] = _displayLevel;
            if (currCandleTrend == prevCandleTrend && currCandleTrend > 0)
            {
                _haDotsSeries.Colors[bar] = _haDotsBullishColor;
            }
            else if (currCandleTrend == prevCandleTrend && currCandleTrend < 0)
            {
                _haDotsSeries.Colors[bar] = _haDotsBearishColor;
            }
            else
            {
                _haDotsSeries.Colors[bar] = _haDotsChangingColor;
            }
        }

        protected override void OnRender(RenderContext context, DrawingLayouts layout)
        {
            base.OnRender(context, layout);

            if (ChartInfo is null) { return; }
            if (Container is null) { return; }

            if (ShowLabel)
            {
                var text = "Heiken Ashi";
                var font = new RenderFont("Arial", 9);
                var textSize = context.MeasureString(text, font);
                context.DrawString(text, font, Color.Gray, ChartInfo.GetXByBar(CurrentBar + 1, true), Container.GetYByValue(DisplayLevel) - (textSize.Height / 2));
            }
        }

        #endregion
    }
}
