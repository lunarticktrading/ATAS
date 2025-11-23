using ATAS.Indicators;
using ATAS.Indicators.Drawing;
using LunarTick.ATAS.Indicators.Helpers;
using OFT.Rendering.Context;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace LunarTick.ATAS.Indicators
{
    [DisplayName("LaguerreRSI")]
    [Category("LunarTick-ATAS-Indicators")]
    public class LaguerreRSI : Indicator, IPropertiesEditorOwner
    {
        #region Enums

        public enum LaguerreRSIDataSeriesIndexEnum
        {
            LaguerreRSIValueDataSeries,
            Internal0,
            Internal1,
            Internal2,
            Internal3,
            Internal4,
            Internal5,
            Internal6,
            Internal7
        }

        #endregion

        #region Members

        private bool _useFractalEnergy = false;
        private decimal _alpha = 0.2m;
        private int _nfe = 8;
        private int _gLength = 13;
        private int _betaDev = 8;
        private decimal _overboughtLevel = 80m;
        private decimal _oversoldLevel = 20m;
        private readonly ValueDataSeries _lrsiSeries = new("LRSI", "Laguerre RSI")
        {
            VisualType = VisualMode.Line,
            Color = DefaultColors.White.Convert(),
            ValuesColor = DefaultColors.Gray,
            Width = 2
        };
        private readonly ValueDataSeries _l0Series = new("L0")
        {
            VisualType = VisualMode.Hide,
            IsHidden = true,
            IgnoredByAlerts = true
        };
        private readonly ValueDataSeries _l1Series = new("L1")
        {
            VisualType = VisualMode.Hide,
            IsHidden = true,
            IgnoredByAlerts = true
        };
        private readonly ValueDataSeries _l2Series = new("L2")
        {
            VisualType = VisualMode.Hide,
            IsHidden = true,
            IgnoredByAlerts = true
        };
        private readonly ValueDataSeries _l3Series = new("L3")
        {
            VisualType = VisualMode.Hide,
            IsHidden = true,
            IgnoredByAlerts = true
        };
        private readonly ValueDataSeries _gOSeries = new("gO")
        {
            VisualType = VisualMode.Hide,
            IsHidden = true,
            IgnoredByAlerts = true
        };
        private readonly ValueDataSeries _gHSeries = new("gH")
        {
            VisualType = VisualMode.Hide,
            IsHidden = true,
            IgnoredByAlerts = true
        };
        private readonly ValueDataSeries _gLSeries = new("gL")
        {
            VisualType = VisualMode.Hide,
            IsHidden = true,
            IgnoredByAlerts = true
        };
        private readonly ValueDataSeries _gCSeries = new("gC")
        {
            VisualType = VisualMode.Hide,
            IsHidden = true,
            IgnoredByAlerts = true
        };

        private int _lastBar = 0;

        private IPropertiesEditor? _propertiesEditor;

        #endregion

        #region Properties

        [OFT.Attributes.Parameter]
        [Display(Name = "Use Fractal Energy", GroupName = "Settings", Order = 001)]
        public bool UseFractalEnergy
        {
            get => _useFractalEnergy;

            set
            {
                _useFractalEnergy = value;
                _propertiesEditor?.SetIsExpandedCategory("Laguerre RSI", !value);
                _propertiesEditor?.SetIsExpandedCategory("Laguerre RSI with Fractal Energy", value);
                RecalculateValues();
            }
        }

        [OFT.Attributes.Parameter]
        [Display(Name = "Alpha", GroupName = "Laguerre RSI", Order = 101)]
        [Range(0, 1)]
        public decimal Alpha
        {
            get => _alpha;

            set
            {
                _alpha = value;
                RecalculateValues();
            }
        }

        [OFT.Attributes.Parameter]
        [Display(Name = "NFE", GroupName = "Laguerre RSI with Fractal Energy", Description = "Number of bars used in Fractal Energy calculations.", Order = 201)]
        [Range(1, Int32.MaxValue)]
        public int NFE
        {
            get => _nfe;

            set
            {
                _nfe = value;
                RecalculateValues();
            }
        }

        [OFT.Attributes.Parameter]
        [Display(Name = "GLength", GroupName = "Laguerre RSI with Fractal Energy", Description = "Period length for Go/Gh/Gl/Gc filter.", Order = 202)]
        [Range(1, Int32.MaxValue)]
        public int GLength
        {
            get => _gLength;

            set
            {
                _gLength = value;
                RecalculateValues();
            }
        }

        [OFT.Attributes.Parameter]
        [Display(Name = "BetaDev", GroupName = "Laguerre RSI with Fractal Energy", Description = "Controls reactivity in alpha/beta computations.", Order = 203)]
        [Range(1, Int32.MaxValue)]
        public int BetaDev
        {
            get => _betaDev;

            set
            {
                _betaDev = value;
                RecalculateValues();
            }
        }

        [OFT.Attributes.Parameter]
        [Display(Name = "Overbought Level", GroupName = "Thresholds", Order = 301)]
        [Range(0, 100)]
        public decimal OverboughtLevel
        {
            get => _overboughtLevel;

            set
            {
                _overboughtLevel = value;
                RecalculateValues();
            }
        }

        [OFT.Attributes.Parameter]
        [Display(Name = "Oversold Level", GroupName = "Thresholds", Order = 302)]
        [Range(0, 100)]
        public decimal OversoldLevel
        {
            get => _oversoldLevel;

            set
            {
                _oversoldLevel = value;
                RecalculateValues();
            }
        }

        [Display(Name = "Show Overbought Region", GroupName = "Thresholds", Order = 303)]
        public FilterColor ShowOverboughtRegion { get; set; }

        [Display(Name = "Show Oversold Region", GroupName = "Thresholds", Order = 304)]
        public FilterColor ShowOversoldRegion { get; set; }

        [Display(Name = "Enter Overbought", GroupName = "Alerts", Description = "When enabled, an alert is triggered by the Laguerre RSI entering the overbought region, using the specified sound file.", Order = 401)]
        public FilterString EnterOverboughtAlertFilter { get; set; }

        [Display(Name = "Exit Overbought", GroupName = "Alerts", Description = "When enabled, an alert is triggered by the Laguerre RSI leaving the overbought region, using the specified sound file.", Order = 402)]
        public FilterString ExitOverboughtAlertFilter { get; set; }

        [Display(Name = "Enter Oversold", GroupName = "Alerts", Description = "When enabled, an alert is triggered by the Laguerre RSI entering the oversold region, using the specified sound file.", Order = 403)]
        public FilterString EnterOversoldAlertFilter { get; set; }

        [Display(Name = "Exit Oversold", GroupName = "Alerts", Description = "When enabled, an alert is triggered by the Laguerre RSI leaving the oversold region, using the specified sound file.", Order = 404)]
        public FilterString ExitOversoldAlertFilter { get; set; }

        [Display(Name = "Alert Sounds Path", GroupName = "Alerts", Description = "Location of alert audio files.", Order = 405)]
        public string AlertSoundsPath { get; set; }

        #endregion

        #region Constructor

        public LaguerreRSI()
        {
            Panel = IndicatorDataProvider.NewPanel;
            DenyToChangePanel = true;
            EnableCustomDrawing = true;
            SubscribeToDrawingEvents(DrawingLayouts.LatestBar);

            // NOTE: The DataSeries must match the order found in LaguerreRSIDataSeriesIndexEnum.
            DataSeries[0] = _lrsiSeries;
            DataSeries.Add(_l0Series);
            DataSeries.Add(_l1Series);
            DataSeries.Add(_l2Series);
            DataSeries.Add(_l3Series);
            DataSeries.Add(_gOSeries);
            DataSeries.Add(_gHSeries);
            DataSeries.Add(_gLSeries);
            DataSeries.Add(_gCSeries);

            UseFractalEnergy = true;
            Alpha = 0.2m;
            NFE = 8;
            GLength = 13;
            BetaDev = 8;

            OverboughtLevel = 80m;
            OversoldLevel = 20m;

            ShowOverboughtRegion = new(true) { Enabled = true, Value = DefaultColors.Red.GetWithTransparency(40).Convert() };
            ShowOversoldRegion = new(true) { Enabled = true, Value = DefaultColors.Green.GetWithTransparency(40).Convert() };

            EnterOverboughtAlertFilter = new(true) { Value = "RSIOverbought.wav" };
            ExitOverboughtAlertFilter = new(true) { Value = "RSILeavingOverbought.wav" };
            EnterOversoldAlertFilter = new(true) { Value = "RSIOversold.wav" };
            ExitOversoldAlertFilter = new(true) { Value = "RSILeavingOversold.wav" };
            AlertSoundsPath = SoundPackHelper.DefaultAlertFilePath();
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

            if (!_useFractalEnergy)
            {
                ////////////////////////////////////////////////////////////////////////////////
                // Laguerre RSI
                // This source code is subject to the terms of the Mozilla Public License 2.0 at https://mozilla.org/MPL/2.0/
                // Developer: John EHLERS
                ////////////////////////////////////////////////////////////////////////////////

                if (CurrentBar < 2)
                    return;

                decimal gamma = 1.0m - _alpha;
                _l0Series[bar] = (1m - gamma) * value + gamma * _l0Series[bar - 1];
                _l1Series[bar] = -gamma * _l0Series[bar] + _l0Series[bar - 1] + gamma * _l1Series[bar - 1];
                _l2Series[bar] = -gamma * _l1Series[bar] + _l1Series[bar - 1] + gamma * _l2Series[bar - 1];
                _l3Series[bar] = -gamma * _l2Series[bar] + _l2Series[bar - 1] + gamma * _l3Series[bar - 1];
                decimal cu = (_l0Series[bar] > _l1Series[bar] ? _l0Series[bar] - _l1Series[bar] : 0) + (_l1Series[bar] > _l2Series[bar] ? _l1Series[bar] - _l2Series[bar] : 0) + (_l2Series[bar] > _l3Series[bar] ? _l2Series[bar] - _l3Series[bar] : 0);
                decimal cd = (_l0Series[bar] < _l1Series[bar] ? _l1Series[bar] - _l0Series[bar] : 0) + (_l1Series[bar] < _l2Series[bar] ? _l2Series[bar] - _l1Series[bar] : 0) + (_l2Series[bar] < _l3Series[bar] ? _l3Series[bar] - _l2Series[bar] : 0);
                decimal temp = cu + cd == 0 ? -1 : cu + cd;
                _lrsiSeries[bar] = 100 * (temp == -1 ? 0 : cu / temp);
            }
            else
            {
                ////////////////////////////////////////////////////////////////////////////////
                // Laguerre RSI with Fractal Energy
                // https://usethinkscript.com/threads/rsi-laguerre-with-fractal-energy-for-thinkorswim.116/
                ////////////////////////////////////////////////////////////////////////////////

                if (CurrentBar < (_nfe + 1))
                    return;

                decimal w = (2.0m * (decimal)Math.PI / _gLength);
                decimal beta = (1m - (decimal)Math.Cos((double)w)) / ((decimal)Math.Pow(1.414, 2.0 / (double)_betaDev) - 1m);
                decimal alpha = (-beta + (decimal)Math.Sqrt((double)(beta * beta + 2m * beta)));

                var currCandle = GetCandle(bar);
                _gOSeries[bar] = (decimal)Math.Pow((double)alpha, 4) * currCandle.Open + 4m * (1.0m - alpha) * _gOSeries[bar - 1] - 6m * (decimal)Math.Pow(1 - (double)alpha, 2) * _gOSeries[bar - 2] + 4 * (decimal)Math.Pow(1 - (double)alpha, 3) * _gOSeries[bar - 3] - (decimal)Math.Pow(1 - (double)alpha, 4) * _gOSeries[bar - 4];
                _gHSeries[bar] = (decimal)Math.Pow((double)alpha, 4) * currCandle.High + 4m * (1.0m - alpha) * _gHSeries[bar - 1] - 6m * (decimal)Math.Pow(1 - (double)alpha, 2) * _gHSeries[bar - 2] + 4 * (decimal)Math.Pow(1 - (double)alpha, 3) * _gHSeries[bar - 3] - (decimal)Math.Pow(1 - (double)alpha, 4) * _gHSeries[bar - 4];
                _gLSeries[bar] = (decimal)Math.Pow((double)alpha, 4) * currCandle.Low + 4m * (1.0m - alpha) * _gLSeries[bar - 1] - 6m * (decimal)Math.Pow(1 - (double)alpha, 2) * _gLSeries[bar - 2] + 4 * (decimal)Math.Pow(1 - (double)alpha, 3) * _gLSeries[bar - 3] - (decimal)Math.Pow(1 - (double)alpha, 4) * _gLSeries[bar - 4];
                _gCSeries[bar] = (decimal)Math.Pow((double)alpha, 4) * value + 4m * (1.0m - alpha) * _gCSeries[bar - 1] - 6m * (decimal)Math.Pow(1 - (double)alpha, 2) * _gCSeries[bar - 2] + 4 * (decimal)Math.Pow(1 - (double)alpha, 3) * _gCSeries[bar - 3] - (decimal)Math.Pow(1 - (double)alpha, 4) * _gCSeries[bar - 4];

                // Calculations
                decimal o = (_gOSeries[bar] + _gCSeries[bar - 1]) / 2.0m;
                decimal h = Math.Max(_gHSeries[bar], _gCSeries[bar - 1]);
                decimal l = Math.Min(_gLSeries[bar], _gCSeries[bar - 1]);
                decimal c = (o + h + l + _gCSeries[bar]) / 4.0m;
                decimal tempSum = 0m;
                for (int idx = bar; idx > (bar - _nfe); idx--)
                {
                    tempSum += (decimal)(Math.Max(_gHSeries[idx], _gCSeries[idx - 1]) - Math.Min(_gLSeries[idx], _gCSeries[idx - 1]));
                }
                decimal gamma = (decimal)(Math.Log( (double)(tempSum / (_gHSeries.MAX(_nfe, bar) - _gLSeries.MIN(_nfe, bar))) )
                                /
                                Math.Log(_nfe));

                _l0Series[bar] = ((1.0m - gamma) * _gCSeries[bar]) + (gamma * _l0Series[bar - 1]);
                _l1Series[bar] = -gamma * _l0Series[bar] + _l0Series[bar-1] + gamma * _l1Series[bar - 1];
                _l2Series[bar] = -gamma * _l1Series[bar] + _l1Series[bar - 1] + gamma * _l2Series[bar - 1];
                _l3Series[bar] = -gamma * _l2Series[bar] + _l2Series[bar - 1] + gamma * _l3Series[bar - 1];

                decimal cu1 = 0m;
                decimal cd1 = 0m;
                decimal cu2 = 0m;
                decimal cd2 = 0m;
                decimal cu = 0m;
                decimal cd = 0m;

                if (_l0Series[bar] >= _l1Series[bar])
                {
                    cu1 = _l0Series[bar] - _l1Series[bar];
                    cd1 = 0m;
                }
                else
                {
                    cd1 = _l1Series[bar] - _l0Series[bar];
                    cu1 = 0m;
                }

                if (_l1Series[bar] >= _l2Series[bar])
                {
                    cu2 = cu1 + _l1Series[bar] - _l2Series[bar];
                    cd2 = cd1;
                }
                else
                {
                    cd2 = cd1 + _l2Series[bar] - _l1Series[bar];
                    cu2 = cu1;
                }

                if (_l2Series[bar] >= _l3Series[bar])
                {
                    cu = cu2 + _l2Series[bar] - _l3Series[bar];
                    cd = cd2;
                }
                else
                {
                    cu = cu2;
                    cd = cd2 + _l3Series[bar] - _l2Series[bar];
                }

                _lrsiSeries[bar] = 100 * ((cu+cd) != 0 ? (cu / (cu + cd)) : 0m);
            }

            // Alerts

            if ((_lastBar != bar) && (bar == CurrentBar - 1))
            {
                if (EnterOverboughtAlertFilter.Enabled)
                {
                    if ((this[bar - 2] < _overboughtLevel && this[bar - 1] >= _overboughtLevel))
                    {
                        string audioFile = SoundPackHelper.ResolveAlertFilePath(EnterOverboughtAlertFilter.Value, AlertSoundsPath);
                        AddAlert(audioFile, InstrumentInfo.Instrument, $"Laguerre RSI entered overbought region {this[bar - 1]:0.#####}", DefaultColors.Black.Convert(), ShowOverboughtRegion.Value);
                    }
                }

                if (ExitOverboughtAlertFilter.Enabled)
                {
                    if ((this[bar - 2] >= _overboughtLevel && this[bar - 1] < _overboughtLevel))
                    {
                        string audioFile = SoundPackHelper.ResolveAlertFilePath(ExitOverboughtAlertFilter.Value, AlertSoundsPath);
                        AddAlert(audioFile, InstrumentInfo.Instrument, $"Laguerre RSI exited overbought region {this[bar - 1]:0.#####}", DefaultColors.Black.Convert(), ShowOverboughtRegion.Value);
                    }
                }

                if (EnterOversoldAlertFilter.Enabled)
                {
                    if ((this[bar - 2] > _oversoldLevel && this[bar - 1] <= _oversoldLevel))
                    {
                        string audioFile = SoundPackHelper.ResolveAlertFilePath(EnterOversoldAlertFilter.Value, AlertSoundsPath);
                        AddAlert(audioFile, InstrumentInfo.Instrument, $"Laguerre RSI entered oversold region {this[bar - 1]:0.#####}", DefaultColors.Black.Convert(), ShowOversoldRegion.Value);
                    }
                }

                if (ExitOversoldAlertFilter.Enabled)
                {
                    if ((this[bar - 2] <= _oversoldLevel && this[bar - 1] > _oversoldLevel))
                    {
                        string audioFile = SoundPackHelper.ResolveAlertFilePath(ExitOversoldAlertFilter.Value, AlertSoundsPath);
                        AddAlert(audioFile, InstrumentInfo.Instrument, $"Laguerre RSI exited oversold region {this[bar - 1]:0.#####}", DefaultColors.Black.Convert(), ShowOversoldRegion.Value);
                    }
                }
            }

            _lastBar = bar;
        }

        protected override void OnRender(RenderContext context, DrawingLayouts layout)
        {
            base.OnRender(context, layout);

            if (ChartInfo is null) { return; }
            if (Container is null) { return; }

            int leftX = ChartInfo.GetXByBar(FirstVisibleBarNumber, true);
            int rightX = ChartInfo.GetXByBar(LastVisibleBarNumber, true);
            int right1X = ChartInfo.GetXByBar(LastVisibleBarNumber + 1, true);
            int barWidth = right1X - rightX;
            int totalWidth = (rightX - leftX) + (barWidth / 2);
            int topY = Container.GetYByValue(100);
            int bottomY = Container.GetYByValue(0);

            if (ShowOverboughtRegion.Enabled)
            {
                int overboughtY = Container.GetYByValue(_overboughtLevel);
                Rectangle overboughtRect = new Rectangle(leftX, topY, totalWidth, overboughtY - topY);
                context.FillRectangle(ShowOverboughtRegion.Value.Convert(), overboughtRect);
            }

            if (ShowOversoldRegion.Enabled)
            {
                int oversoldY = Container.GetYByValue(_oversoldLevel);
                Rectangle oversoldRect = new Rectangle(leftX, oversoldY, totalWidth, bottomY - oversoldY);
                context.FillRectangle(ShowOversoldRegion.Value.Convert(), oversoldRect);
            }
        }

        #endregion

        #region IPropertiesEditorOwner

        [Browsable(false)]
        IPropertiesEditor? IPropertiesEditorOwner.PropertiesEditor
        {
            get => _propertiesEditor;

            set
            {
                if (_propertiesEditor == value)
                    return;

                _propertiesEditor = value;
                PropertiesEditorOnChanged(value);
            }
        }

        private void PropertiesEditorOnChanged(IPropertiesEditor? newValue)
        {
            newValue?.BeginInit();

            newValue?.SetIsExpandedCategory("Laguerre RSI", !UseFractalEnergy);
            newValue?.SetIsExpandedCategory("Laguerre RSI with Fractal Energy", UseFractalEnergy);

            newValue?.EndInit();
        }

        #endregion
    }
}
