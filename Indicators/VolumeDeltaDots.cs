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
    [DisplayName("VolumeDeltaDots")]
    [Category("LunarTick-ATAS-Indicators")]
    public class VolumeDeltaDots : Indicator
    {
        #region Enums

        public enum VolumeDeltaDotsDataSeriesIndexEnum
        {
            VolumeDeltaDotsValueDataSeries
        }

        #endregion

        #region Constants

        const decimal DefaultDisplayLevel = 50m;
        const int DefaultDisplayWidth = 5;

        #endregion

        #region Members

        private Delta _delta = new();
        private decimal _displayLevel = DefaultDisplayLevel;
        private int _displayWidth = DefaultDisplayWidth;
        private Color _bullishVolumeDeltaColor = DefaultColors.Green;
        private Color _equalVolumeDeltaColor = DefaultColors.Yellow;
        private Color _bearishVolumeDeltaColor = DefaultColors.Red;
        private readonly ValueDataSeries _volumeDeltaDotsSeries = new("VolumeDeltaDots", "Volume Delta Dots")
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
                _volumeDeltaDotsSeries.Width = value;
                RecalculateValues();
            }
        }

        [Display(Name = "Bullish Volume Delta Color", GroupName = "Display", Description = "Dot color used to indicate a bullish volume delta.", Order = 103)]
        public Color BullishVolumeDeltaColor
        {
            get => _bullishVolumeDeltaColor;

            set
            {
                _bullishVolumeDeltaColor = value;
                RecalculateValues();
            }
        }

        [Display(Name = "Equal Volume Delta Color", GroupName = "Display", Description = "Dot color used to indicate an equal volume delta.", Order = 104)]
        public Color EqualVolumeDeltaColor
        {
            get => _equalVolumeDeltaColor;

            set
            {
                _equalVolumeDeltaColor = value;
                RecalculateValues();
            }
        }

        [Display(Name = "Bearish Volume Delta Color", GroupName = "Display", Description = "Dot color used to indicate a bearish volume delta.", Order = 105)]
        public Color BearishVolumeDeltaColor
        {
            get => _bearishVolumeDeltaColor;

            set
            {
                _bearishVolumeDeltaColor = value;
                RecalculateValues();
            }
        }

        [Display(Name = "Show Label", GroupName = "Display", Order = 106)]
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

        public VolumeDeltaDots()
        {
            Panel = IndicatorDataProvider.NewPanel;
            EnableCustomDrawing = true;
            SubscribeToDrawingEvents(DrawingLayouts.LatestBar);

            // NOTE: The DataSeries must match the order found in VolumeDeltaDotsDataSeriesIndexEnum.
            DataSeries[0] = _volumeDeltaDotsSeries;

            DisplayLevel = DefaultDisplayLevel;
            DisplayWidth = DefaultDisplayWidth;
            BullishVolumeDeltaColor = DefaultColors.Green;
            EqualVolumeDeltaColor = DefaultColors.Yellow;
            BearishVolumeDeltaColor = DefaultColors.Red;
            ShowLabel = true;

            Add(_delta);
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
            decimal deltaValue = ((ValueDataSeries)_delta.DataSeries[2])[bar];
            _volumeDeltaDotsSeries[bar] = _displayLevel;
            if (deltaValue > 0)
            {
                _volumeDeltaDotsSeries.Colors[bar] = BullishVolumeDeltaColor;
            }
            else if (deltaValue < 0)
            {
                _volumeDeltaDotsSeries.Colors[bar] = BearishVolumeDeltaColor;
            }
            else
            {
                _volumeDeltaDotsSeries.Colors[bar] = EqualVolumeDeltaColor;
            }
        }

        protected override void OnRender(RenderContext context, DrawingLayouts layout)
        {
            base.OnRender(context, layout);

            if (ChartInfo is null) { return; }
            if (Container is null) { return; }

            if (ShowLabel)
            {
                var text = "Volume Delta";
                var font = new RenderFont("Arial", 9);
                var textSize = context.MeasureString(text, font);
                context.DrawString(text, font, Color.Gray, ChartInfo.GetXByBar(CurrentBar + 1, true), Container.GetYByValue(DisplayLevel) - (textSize.Height / 2));
            }
        }

        #endregion
    }
}
