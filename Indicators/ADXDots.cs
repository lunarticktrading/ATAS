using ATAS.Indicators;
using ATAS.Indicators.Drawing;
using ATAS.Indicators.Technical;
using OFT.Rendering.Context;
using OFT.Rendering.Tools;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace LunarTick.ATAS.Indicators
{
    [DisplayName("ADXDots")]
    [Category("LunarTick-ATAS-Indicators")]
    public class ADXDots : Indicator
    {
        #region Enums

        public enum ADXDotsDataSeriesIndexEnum
        {
            ADXValueDataSeries,
            ADXDotsValueDataSeries
        }

        #endregion

        #region Constants

        const decimal DefaultDisplayLevel = 50m;
        const int DefaultDisplayWidth = 5;
        const decimal DefaultMediumTrendThreshold = 15m;
        const decimal DefaultStrongTrendThreshold = 23m;

        #endregion

        #region Members

        private readonly ADX _adx = new();
        private decimal _displayLevel = DefaultDisplayLevel;
        private int _displayWidth = DefaultDisplayWidth;
        private decimal _mediumTrendThreshold = DefaultMediumTrendThreshold;
        private decimal _strongTrendThreshold = DefaultStrongTrendThreshold;
        private Color _adxDotsWeakTrendColor = Color.FromArgb(255, 225, 190, 231);
        private Color _adxDotsMediumTrendColor = Color.FromArgb(255, 186, 104, 200);
        private Color _adxDotsStrongTrendColor = Color.FromArgb(255, 123, 31, 162);
        private readonly ValueDataSeries _adxSeries = new("ADX")
        {
            VisualType = VisualMode.Hide,
            ShowZeroValue = false,
            ShowCurrentValue = false,
            ShowTooltip = true,
            DrawAbovePrice = false,
            IsHidden = true
        };
        private readonly ValueDataSeries _adxDotsSeries = new("ADXDots", "ADX Dots")
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

        [OFT.Attributes.Parameter]
        [Display(Name = "ADX Period", GroupName = "Settings", Order = 001)]
        [Range(1, 10000)]
        public int Period
        {
            get => _adx.Period;

            set
            {
                _adx.Period = value;
                RecalculateValues();
            }
        }

        [OFT.Attributes.Parameter]
        [Display(Name = "ADX Smoothing Period", GroupName = "Settings", Order = 002)]
        [Range(1, 10000)]
        public int SmoothPeriod
        {
            get => _adx.SmoothPeriod;

            set
            {
                _adx.SmoothPeriod = value;
                RecalculateValues();
            }
        }

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
                _adxDotsSeries.Width = value;
                RecalculateValues();
            }
        }

        [Display(Name = "Medium Trend Threshold", GroupName = "Display", Description = "Minimum ADX value to be considered a medium trend.", Order = 103)]
        public decimal MediumTrendThreshold
        {
            get => _mediumTrendThreshold;

            set
            {
                _mediumTrendThreshold = value;
                RecalculateValues();
            }
        }

        [Display(Name = "Strong Trend Threshold", GroupName = "Display", Description = "Minimum ADX value to be considered a strong trend.", Order = 104)]
        public decimal StrongTrendThreshold
        {
            get => _strongTrendThreshold;

            set
            {
                _strongTrendThreshold = value;
                RecalculateValues();
            }
        }

        [Display(Name = "Weak Trend Color", GroupName = "Display", Description = "Dot color used to indicate a weak trend.", Order = 105)]
        public Color WeakTrendColor
        {
            get => _adxDotsWeakTrendColor;

            set
            {
                _adxDotsWeakTrendColor = value;
                RecalculateValues();
            }
        }

        [Display(Name = "Medium Trend Color", GroupName = "Display", Description = "Dot color used to indicate a medium trend.", Order = 106)]
        public Color MediumTrendColor
        {
            get => _adxDotsMediumTrendColor;

            set
            {
                _adxDotsMediumTrendColor = value;
                RecalculateValues();
            }
        }

        [Display(Name = "Strong Trend Color", GroupName = "Display", Description = "Dot color used to indicate a strong trend.", Order = 107)]
        public Color StrongTrendColor
        {
            get => _adxDotsStrongTrendColor;

            set
            {
                _adxDotsStrongTrendColor = value;
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

        public ADXDots() : base(true)
        {
            Panel = IndicatorDataProvider.NewPanel;
            EnableCustomDrawing = true;
            SubscribeToDrawingEvents(DrawingLayouts.LatestBar);

            // NOTE: The DataSeries must match the order found in ADXDotsDataSeriesIndexEnum.
            DataSeries[0] = _adxSeries;
            DataSeries.Add(_adxDotsSeries);

            Period = 14;
            SmoothPeriod = 14;
            DisplayLevel = DefaultDisplayLevel;
            DisplayWidth = DefaultDisplayWidth;
            MediumTrendThreshold = DefaultMediumTrendThreshold;
            StrongTrendThreshold = DefaultStrongTrendThreshold;
            WeakTrendColor = Color.FromArgb(255, 225, 190, 231);
            MediumTrendColor = Color.FromArgb(255, 186, 104, 200);
            StrongTrendColor = Color.FromArgb(255, 123, 31, 162);
            ShowLabel = true;

            Add(_adx);
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

            if (CurrentBar < Math.Max(Period, SmoothPeriod))
                return;

            var adx = _adx[bar];
            _adxSeries[bar] = adx;
            _adxDotsSeries[bar] = _displayLevel;
            if (adx >= _strongTrendThreshold)
            {
                _adxDotsSeries.Colors[bar] = _adxDotsStrongTrendColor;
            }
            else if (adx >= _mediumTrendThreshold)
            {
                _adxDotsSeries.Colors[bar] = _adxDotsMediumTrendColor;
            }
            else
            {
                _adxDotsSeries.Colors[bar] = _adxDotsWeakTrendColor;
            }
        }

        protected override void OnRender(RenderContext context, DrawingLayouts layout)
        {
            base.OnRender(context, layout);

            if (ChartInfo is null) { return; }
            if (Container is null) { return; }

            if (ShowLabel)
            {
                var text = "ADX";
                var font = new RenderFont("Arial", 9);
                var textSize = context.MeasureString(text, font);
                context.DrawString(text, font, Color.Gray, ChartInfo.GetXByBar(CurrentBar + 1, true), Container.GetYByValue(DisplayLevel) - (textSize.Height / 2));
            }
        }

        #endregion
    }
}
