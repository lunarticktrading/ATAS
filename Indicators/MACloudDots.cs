using ATAS.Indicators;
using ATAS.Indicators.Drawing;
using OFT.Rendering.Context;
using OFT.Rendering.Tools;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using Utils.Common.Logging;

namespace LunarTick.ATAS.Indicators
{
    [DisplayName("MACloudDots")]
    [Category("LunarTick-ATAS-Indicators")]
    public class MACloudDots : Indicator
    {
        #region Enums

        public enum MACloudDotsDataSeriesIndexEnum
        {
            MACloudDotsValueDataSeries
        }

        #endregion

        #region Constants

        const decimal DefaultDisplayLevel = 50m;
        const int DefaultDisplayWidth = 5;

        #endregion

        #region Members

        private bool _dummyValue = false;
        private bool _forceRecalculate = false;
        private MACloud _maCloud = new();
        private decimal _displayLevel = DefaultDisplayLevel;
        private int _displayWidth = DefaultDisplayWidth;
        private Color _maCloudDotsBullishTrendColor = DefaultColors.Green;
        private Color _maCloudDotsBearishTrendColor = DefaultColors.Red;
        private readonly ValueDataSeries _maCloudDotsSeries = new("MACloudDots", "MA Cloud Dots")
        {
            VisualType = VisualMode.Dots,
            ShowZeroValue = false,
            ShowCurrentValue = false,
            ShowTooltip = false,
            Color = Color.Transparent.Convert(),
            Width = DefaultDisplayWidth,
            DrawAbovePrice = false,
            IsHidden = true
        };
        private bool _showLabel;

        #endregion

        #region Properties

        [OFT.Attributes.Parameter]
        [Display(Name = "Toggle to Recalculate", GroupName = "Settings", Order = 000)]
        public bool ToggleToRecalculate
        {
            get => _dummyValue;

            set
            {
                _dummyValue = value;
                RecalculateValues();
            }
        }

        [OFT.Attributes.Parameter]
        [Display(Name = "MA Type", GroupName = "Settings", Description = "The type of Moving Average.", Order = 001)]
        public MACloud.MATypeEnum MAType
        {
            get => _maCloud.MAType;

            set
            {
                _maCloud.MAType = value;
                _maCloudDotsSeries.Name = $"{value} Cloud Dots";
                RecalculateValues();
            }
        }

        [OFT.Attributes.Parameter]
        [Display(Name = "Fast Period", GroupName = "Settings", Description = "The period of the fast Moving Average.", Order = 002)]
        [Range(1, 1000)]
        public int FastPeriod
        {
            get => _maCloud.FastPeriod;

            set
            {
                _maCloud.FastPeriod = value;
                RecalculateValues();
            }
        }

        [OFT.Attributes.Parameter]
        [Display(Name = "Slow Period", GroupName = "Settings", Description = "The period of the slow Moving Average.", Order = 003)]
        [Range(1, 1000)]
        public int SlowPeriod
        {
            get => _maCloud.SlowPeriod;

            set
            {
                _maCloud.SlowPeriod = value;
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
                _maCloudDotsSeries.Width = value;
                RecalculateValues();
            }
        }

        [Display(Name = "Bullish Trend Color", GroupName = "Display", Description = "Dot color used to indicate a bullish trend.", Order = 103)]
        public Color BullishTrendColor
        {
            get => _maCloudDotsBullishTrendColor;

            set
            {
                _maCloudDotsBullishTrendColor = value;
                RecalculateValues();
            }
        }

        [Display(Name = "Bearish Trend Color", GroupName = "Display", Description = "Dot color used to indicate a bearish trend.", Order = 104)]
        public Color BearishTrendColor
        {
            get => _maCloudDotsBearishTrendColor;

            set
            {
                _maCloudDotsBearishTrendColor = value;
                RecalculateValues();
            }
        }

        [Display(Name = "Show Label", GroupName = "Display", Order = 105)]
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

        public MACloudDots()
        {
            Panel = IndicatorDataProvider.NewPanel;
            EnableCustomDrawing = true;
            SubscribeToDrawingEvents(DrawingLayouts.LatestBar);

            // NOTE: The DataSeries must match the order found in MACloudDotsDataSeriesIndexEnum.
            DataSeries[0] = _maCloudDotsSeries;

            DisplayLevel = DefaultDisplayLevel;
            DisplayWidth = DefaultDisplayWidth;
            BullishTrendColor = DefaultColors.Green;
            BearishTrendColor = DefaultColors.Red;
            ShowLabel = true;

            MAType = MACloud.MATypeEnum.EMA;
            FastPeriod = 9;
            SlowPeriod = 21;

            Add(_maCloud);
        }

        #endregion

        #region Indicator methods

        protected override void OnInitialize()
        {
            base.OnInitialize();
            // Work around a bug in ATAS where the child indicators do not seem to get recalculated.
            _forceRecalculate = true;
        }

        protected override void OnRecalculate()
        {
            base.OnRecalculate();

            DataSeries.ForEach(ds => ds.Clear());
        }

        protected override void OnCalculate(int bar, decimal value)
        {
            if (bar == 0)
                return;

            if (CurrentBar < Math.Max(_maCloud.FastPeriod, _maCloud.SlowPeriod))
                return;

            decimal fastValue = ((ValueDataSeries)_maCloud.DataSeries[(int)MACloud.MACloudDataSeriesIndexEnum.FastMAValueDataSeries])[bar];
            decimal slowValue = ((ValueDataSeries)_maCloud.DataSeries[(int)MACloud.MACloudDataSeriesIndexEnum.SlowMAValueDataSeries])[bar];
            _maCloudDotsSeries[bar] = _displayLevel;
            if (fastValue > slowValue)
            {
                _maCloudDotsSeries.Colors[bar] = _maCloudDotsBullishTrendColor;
            }
            else if (fastValue < slowValue)
            {
                _maCloudDotsSeries.Colors[bar] = _maCloudDotsBearishTrendColor;
            }
            else
            {
                _maCloudDotsSeries.Colors[bar] = Color.Transparent;
            }
        }

        protected override void OnRender(RenderContext context, DrawingLayouts layout)
        {
            base.OnRender(context, layout);

            if (ChartInfo is null) { return; }
            if (Container is null) { return; }

            if (ShowLabel)
            {
                var text = _maCloud.MAType + " Cloud";
                var font = new RenderFont("Arial", 9);
                var textSize = context.MeasureString(text, font);
                context.DrawString(text, font, Color.Gray, ChartInfo.GetXByBar(CurrentBar + 1, true), Container.GetYByValue(DisplayLevel) - (textSize.Height / 2));
            }

            // Work around a bug in ATAS where the child indicators do not seem to get recalculated.
            if (_forceRecalculate)
            {
                DoActionInGuiThread(() =>
                {
                    this.LogInfo("Forcing child indicators to recalculate");
                    ToggleToRecalculate = !ToggleToRecalculate;
                });
                _forceRecalculate = false;
            }
        }

        #endregion
    }
}
