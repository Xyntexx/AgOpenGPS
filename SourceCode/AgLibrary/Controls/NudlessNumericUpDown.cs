using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Windows.Forms;

namespace AgLibrary.Controls
{
    public enum UnitMode
    {
        None,
        Large,
        Small,
        Speed,
        Area,
        Distance,
        Temperature
    }

    public class NumericUnitModeConverter : EnumConverter
    {
        public NumericUnitModeConverter(Type type) : base(type) { }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(new UnitMode[]
            {
                UnitMode.None,
                UnitMode.Large,
                UnitMode.Small,
                UnitMode.Speed
            });
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
    }

    /// <summary>
    /// A button-based numeric input control designed for touch interfaces.
    /// Opens a full-screen numeric keypad when clicked, instead of using spinner arrows.
    /// Supports unit conversion for imperial/metric display.
    /// </summary>
    public class NudlessNumericUpDown : Button
    {
        private double _value = double.NaN;
        private double minimum = 0;
        private double maximum = 100;
        private int decimalPlaces = 0;
        private bool initializing = true;
        private string format = "0";
        private EventHandler onValueChanged;
        private UnitMode mode;

        // Delegate to get unit conversion factor - allows parent form to provide conversion
        public Func<UnitMode, double> GetDisplayConversionFactor { get; set; }
        public Func<UnitMode, double> GetStorageConversionFactor { get; set; }

        // Reference to the numeric input form - must be set by parent application
        public Func<double, double, double, Form> CreateNumericForm { get; set; }

        public NudlessNumericUpDown()
        {
            base.TextAlign = ContentAlignment.MiddleCenter;
            base.BackColor = SystemColors.Control;
            base.ForeColor = Color.Black;
            base.UseVisualStyleBackColor = false;
            base.FlatStyle = FlatStyle.Flat;

            // Default conversion factors (1:1, no conversion)
            GetDisplayConversionFactor = _ => 1.0;
            GetStorageConversionFactor = _ => 1.0;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            base.Font = new Font("Tahoma", this.Height / 2, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
        }

        public event EventHandler ValueChanged
        {
            add => onValueChanged = (EventHandler)Delegate.Combine(onValueChanged, value);
            remove => onValueChanged = (EventHandler)Delegate.Remove(onValueChanged, value);
        }

        protected override void OnClick(EventArgs e)
        {
            if (CreateNumericForm == null)
            {
                MessageBox.Show("CreateNumericForm delegate must be set before using this control.",
                    "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var displayFactor = GetDisplayConversionFactor(mode);
            var storageFactor = GetStorageConversionFactor(mode);

            var localMin = minimum * displayFactor;
            var localMax = maximum * displayFactor;
            var localVal = Math.Round(_value * displayFactor, decimalPlaces);

            using (Form form = CreateNumericForm(localMin, localMax, localVal))
            {
                DialogResult result = form.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    // Use reflection to get ReturnValue property
                    var returnValueProp = form.GetType().GetProperty("ReturnValue");
                    if (returnValueProp != null)
                    {
                        var localReturn = Math.Round((double)returnValueProp.GetValue(form), decimalPlaces);
                        Value = localReturn * storageFactor;
                        onValueChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        [DefaultValue(typeof(UnitMode), "None")]
        [TypeConverter(typeof(NumericUnitModeConverter))]
        public UnitMode Mode
        {
            get => mode;
            set
            {
                mode = value;
                RefreshDesigner();
            }
        }

        [Bindable(false)]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double Value
        {
            get => _value;
            set
            {
                if (value < minimum)
                    value = minimum;
                else if (value > maximum)
                    value = maximum;

                _value = value;
                initializing = false;
                UpdateEditText();
            }
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            if (DesignMode)
            {
                using (Graphics g = CreateGraphics())
                {
                    float fontSize = base.Font.Size;
                    SizeF textSize = g.MeasureString(base.Text, base.Font);

                    while ((textSize.Width > base.Width - 10 || textSize.Height > base.Height - 5) && fontSize > 5)
                    {
                        fontSize -= 0.5f;
                        textSize = g.MeasureString(base.Text, new Font(base.Font.FontFamily, fontSize, base.Font.Style));
                    }

                    base.Font = new Font(base.Font.FontFamily, fontSize, base.Font.Style);
                }
            }
            base.OnPaint(pevent);
        }

        [DefaultValue(typeof(double), "0")]
        public double Minimum
        {
            get => minimum;
            set
            {
                minimum = value;
                if (minimum > maximum)
                    maximum = value;

                if (!initializing)
                    Value = _value;

                RefreshDesigner();
            }
        }

        private void RefreshDesigner()
        {
            if (DesignMode)
            {
                if (decimalPlaces > 0)
                    base.Text = minimum.ToString("0.0#######") + "|" + maximum.ToString("0.0#######") + "|" + mode.ToString();
                else
                    base.Text = minimum.ToString("0") + "|" + maximum.ToString("0") + "|" + mode.ToString();

                var host = (IComponentChangeService)GetService(typeof(IComponentChangeService));
                host?.OnComponentChanged(this, null, null, null);
                Parent?.Invalidate();
                Parent?.Update();
            }
        }

        [DefaultValue(typeof(double), "100")]
        public double Maximum
        {
            get => maximum;
            set
            {
                maximum = value;
                if (minimum > maximum)
                    minimum = maximum;

                if (!initializing)
                    Value = _value;

                RefreshDesigner();
            }
        }

        [DefaultValue(typeof(int), "0")]
        public int DecimalPlaces
        {
            get => decimalPlaces;
            set
            {
                decimalPlaces = value;
                format = "0";

                if (decimalPlaces > 0)
                {
                    for (int i = 0; i < decimalPlaces; i++)
                    {
                        if (i == 0)
                            format = "0.0";
                        else
                            format += "0";
                    }
                }

                if (!initializing)
                    UpdateEditText();

                RefreshDesigner();
            }
        }

        public override string ToString()
        {
            return base.ToString() + ", Minimum = " + minimum.ToString("0.0") + ", Maximum = " + maximum.ToString("0.0");
        }

        protected void UpdateEditText()
        {
            var displayFactor = GetDisplayConversionFactor(mode);
            base.Text = (_value * displayFactor).ToString(format);
        }

        // Hide inherited properties that shouldn't be used
        [Bindable(false)]
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override Color BackColor { get => base.BackColor; set => base.BackColor = value; }

        [Bindable(false)]
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override string Text { get => base.Text; set => base.Text = value; }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new event EventHandler TextChanged { add => base.TextChanged += value; remove => base.TextChanged -= value; }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override ContentAlignment TextAlign { get => base.TextAlign; set => base.TextAlign = value; }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override RightToLeft RightToLeft { get => base.RightToLeft; set => base.RightToLeft = value; }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override bool AutoSize { get => base.AutoSize; set => base.AutoSize = value; }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override Cursor Cursor { get => base.Cursor; set => base.Cursor = value; }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new bool UseVisualStyleBackColor { get => base.UseVisualStyleBackColor; set => base.UseVisualStyleBackColor = value; }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Color ForeColor { get => base.ForeColor; set => base.ForeColor = value; }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new event EventHandler Click { add => base.Click += value; remove => base.Click -= value; }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new event EventHandler Enter { add => base.Enter += value; remove => base.Enter -= value; }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new FlatStyle FlatStyle { get => base.FlatStyle; set => base.FlatStyle = value; }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override Font Font { get => base.Font; set => base.Font = value; }
    }
}
