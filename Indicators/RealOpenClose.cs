using ATAS.Indicators;
using ATAS.Indicators.Drawing;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LunarTick.ATAS.Indicators
{
    [DisplayName("RealOpenClose")]
    [Category("LunarTick-ATAS-Indicators")]
    public class RealOpenClose : Indicator
    {
        #region Enums

        public enum RealOpenCloseDataSeriesIndexEnum
        {
            RealOpenValueDataSeries,
            RealCloseValueDataSeries
        }

        #endregion

        #region Constants

        const int DefaultWidth = 2;

        #endregion

        #region Members

        private int _width = DefaultWidth;
        private readonly ValueDataSeries _realOpenSeries = new("RealOpen", "Real Open")
        {
            VisualType = VisualMode.Hash,
            Color = DefaultColors.White.Convert(),
            IsHidden = true,
            Width = DefaultWidth,
            DrawAbovePrice = true,
            ShowCurrentValue = false,
            ShowZeroValue = false,
            ShowOnlyNonZeroLabels = true,
            IgnoredByAlerts = true
        };
        private readonly ValueDataSeries _realCloseSeries = new("RealClose", "Real Close")
        {
            VisualType = VisualMode.Hash,
            Color = DefaultColors.Fuchsia.Convert(),
            IsHidden = true,
            Width = DefaultWidth,
            DrawAbovePrice = true,
            ShowCurrentValue = false,
            ShowZeroValue = false,
            ShowOnlyNonZeroLabels = true,
            IgnoredByAlerts = true
        };

        #endregion

        #region Properties

        [Display(Name = "Show Real Open", GroupName = "Settings", Description = "Show the real Open price of each candle, using the specified color.", Order = 001)]
        public FilterColor ShowRealOpen {  get; set; }

        [Display(Name = "Show Real Close", GroupName = "Settings", Description = "Show the real Close price of each candle, using the specified color.", Order = 002)]
        public FilterColor ShowRealClose { get; set; }

        [Display(Name = "Width", GroupName = "Settings", Description = "Controls the thickness of the real Open/Close levels", Order = 003)]
        public int Width
        {
            get => _width;

            set
            {
                _width = value;
                _realOpenSeries.Width = value;
                _realCloseSeries.Width = value;
                RecalculateValues();
            }
        }

        #endregion

        #region Constructor

        public RealOpenClose() : base(true)
        {
            DenyToChangePanel = true;

            // NOTE: The DataSeries must match the order found in RealOpenCloseDataSeriesIndexEnum.
            DataSeries[0] = _realOpenSeries;
            DataSeries.Add(_realCloseSeries);

            ShowRealOpen = new(true) { Enabled = true, Value = DefaultColors.White.Convert() };
            ShowRealClose = new(true) { Enabled = true, Value = DefaultColors.Fuchsia.Convert() };
            Width = DefaultWidth;

            ShowRealOpen.PropertyChanged += OnFilterPropertyChanged;
            ShowRealClose.PropertyChanged += OnFilterPropertyChanged;
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
            var candle = GetCandle(bar);
            _realOpenSeries[bar] = ShowRealOpen.Enabled ? candle.Open : 0m;
            _realCloseSeries[bar] = ShowRealClose.Enabled ? candle.Close : 0m;
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

            if ((FilterColor)sender == ShowRealOpen)
            {
                if (e.PropertyName.Equals("Value"))
                    _realOpenSeries.Color = ShowRealOpen.Value;
                RecalculateValues();
            }
            else if ((FilterColor)sender == ShowRealClose)
            {
                if (e.PropertyName.Equals("Value"))
                    _realCloseSeries.Color = ShowRealClose.Value;
                RecalculateValues();
            }
        }

        #endregion
    }
}
