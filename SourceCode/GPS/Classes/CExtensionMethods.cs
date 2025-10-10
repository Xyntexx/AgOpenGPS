using AgLibrary.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AgOpenGPS
{
    /// <summary>
    /// Legacy NudlessNumericUpDown - just hides spinner buttons.
    /// Kept for backward compatibility with existing forms.
    /// </summary>
    public class NudlessNumericUpDown : NumericUpDown
    {
        public NudlessNumericUpDown()
        {
            Controls[0].Hide();
        }

        protected override void OnTextBoxResize(object source, EventArgs e)
        {
            Controls[1].Width = Width - 4;
        }

        public new decimal Value
        {
            get
            {
                return base.Value;
            }
            set
            {
                if (value != base.Value)
                {
                    if (value < Minimum)
                    {
                        value = Minimum;
                    }
                    if (value > Maximum)
                    {
                        value = Maximum;
                    }
                    base.Value = value;
                }
            }
        }
    }

    /// <summary>
    /// Enhanced NudlessNumericUpDown with touch-friendly keypad and unit conversion.
    /// Use this for new forms or when migrating existing controls.
    /// </summary>
    public class NudlessNumericUpDownEx : AgLibrary.Controls.NudlessNumericUpDown
    {
        public NudlessNumericUpDownEx()
        {
            // Wire up the numeric form creator
            CreateNumericForm = (min, max, val) => new FormNumeric(min, max, val);

            // Set up unit conversion delegates - get values from FormGPS
            GetDisplayConversionFactor = (mode) =>
            {
                var mf = Application.OpenForms["FormGPS"] as FormGPS;
                if (mf == null) return 1.0;

                if (mode == UnitMode.Small)
                    return mf.m2InchOrCm;
                else if (mode == UnitMode.Large)
                    return mf.m2FtOrM;
                else
                    return 1.0;
            };

            GetStorageConversionFactor = (mode) =>
            {
                var mf = Application.OpenForms["FormGPS"] as FormGPS;
                if (mf == null) return 1.0;

                if (mode == UnitMode.Small)
                    return mf.inchOrCm2m;
                else if (mode == UnitMode.Large)
                    return mf.ftOrMtoM;
                else
                    return 1.0;
            };
        }
    }


    public static class CExtensionMethods
    {
        /// <summary>
        /// Sets the progress bar value, without using 'Windows Aero' animation.
        /// This is to work around a known WinForms issue where the progress bar
        /// is slow to update.
        /// </summary>
        public static void SetProgressNoAnimation(this ProgressBar pb, int value)
        {
            // To get around the progressive animation, we need to move the
            // progress bar backwards.
            if (value == pb.Maximum)
            {
                // Special case as value can't be set greater than Maximum.
                pb.Maximum = value + 1;     // Temporarily Increase Maximum
                pb.Value = value + 1;       // Move past
                pb.Maximum = value;         // Reset maximum
            }
            else
            {
                pb.Value = value + 1;       // Move past
            }
            pb.Value = value;               // Move to correct value
        }

        public static Color CheckColorFor255(this Color color)
        {
            var currentR = color.R;
            var currentG = color.G;
            var currentB = color.B;

            if (currentR == 255) currentR = 254;
            if (currentG == 255) currentG = 254;
            if (currentB == 255) currentB = 254;

            return Color.FromArgb(color.A, currentR, currentG, currentB);
        }

        /// <summary>
        /// Offsets a list of vec3 points by a given distance perpendicular to their heading.
        /// Filters out points that are too close to the original line or each other.
        /// </summary>
        /// <param name="points">List of points to offset</param>
        /// <param name="distance">Distance to offset (perpendicular to heading)</param>
        /// <param name="minDist">Minimum distance between consecutive offset points</param>
        /// <param name="loop">Whether the points form a closed loop</param>
        /// <returns>New list of offset points</returns>
        public static List<vec3> OffsetLine(this List<vec3> points, double distance, double minDist, bool loop)
        {
            points.CalculateHeadings(loop);

            var result = new List<vec3>();
            int count = points.Count;

            double distSq = distance * distance - 0.0001;

            // Create offset points perpendicular to the heading
            for (int i = 0; i < count; i++)
            {
                // Calculate the point offset perpendicular to the heading
                var easting = points[i].easting + (Math.Cos(points[i].heading) * distance);
                var northing = points[i].northing - (Math.Sin(points[i].heading) * distance);

                bool add = true;

                // Check if offset point is too close to any original point
                for (int j = 0; j < count; j++)
                {
                    double check = glm.DistanceSquared(northing, easting, points[j].northing, points[j].easting);
                    if (check < distSq)
                    {
                        add = false;
                        break;
                    }
                }

                if (add)
                {
                    // Check minimum distance from previous offset point
                    if (result.Count > 0)
                    {
                        double dist = glm.DistanceSquared(northing, easting, result[result.Count - 1].northing, result[result.Count - 1].easting);
                        if (dist > minDist)
                            result.Add(new vec3(easting, northing, 0));
                    }
                    else
                        result.Add(new vec3(easting, northing, 0));
                }
            }

            return result;
        }

        /// <summary>
        /// Calculates the heading for each point in a list based on adjacent points.
        /// The heading is the average angle from the previous to the next point.
        /// </summary>
        /// <param name="points">List of points to calculate headings for (modified in place)</param>
        /// <param name="loop">Whether the points form a closed loop</param>
        public static void CalculateHeadings(this List<vec3> points, bool loop)
        {
            int cnt = points.Count;

            if (cnt > 1)
            {
                vec3[] arr = new vec3[cnt];
                cnt--;
                points.CopyTo(arr);
                points.Clear();

                // First point heading
                vec3 pt3 = arr[0];
                if (loop)
                    pt3.heading = Math.Atan2(arr[1].easting - arr[cnt].easting, arr[1].northing - arr[cnt].northing);
                else
                    pt3.heading = Math.Atan2(arr[1].easting - arr[0].easting, arr[1].northing - arr[0].northing);

                if (pt3.heading < 0) pt3.heading += glm.twoPI;
                points.Add(pt3);

                // Middle points - average heading from previous to next point
                for (int i = 1; i < cnt; i++)
                {
                    pt3 = arr[i];
                    pt3.heading = Math.Atan2(arr[i + 1].easting - arr[i - 1].easting, arr[i + 1].northing - arr[i - 1].northing);
                    if (pt3.heading < 0) pt3.heading += glm.twoPI;
                    points.Add(pt3);
                }

                // Last point heading
                pt3 = arr[cnt];
                if (loop)
                    pt3.heading = Math.Atan2(arr[0].easting - arr[cnt - 1].easting, arr[0].northing - arr[cnt - 1].northing);
                else
                    pt3.heading = Math.Atan2(arr[cnt].easting - arr[cnt - 1].easting, arr[cnt].northing - arr[cnt - 1].northing);

                if (pt3.heading < 0) pt3.heading += glm.twoPI;
                points.Add(pt3);
            }
        }
    }

    //public class ExtendedPanel : Panel
    //{
    //    private const int WS_EX_TRANSPARENT = 0x20;
    //    public ExtendedPanel()
    //    {
    //        SetStyle(ControlStyles.Opaque, true);
    //    }

    //    private int opacity = 50;
    //    [DefaultValue(50)]
    //    public int Opacity
    //    {
    //        get
    //        {
    //            return this.opacity;
    //        }
    //        set
    //        {
    //            if (value < 0 || value > 100)
    //                throw new System.ArgumentException("value must be between 0 and 100");
    //            this.opacity = value;
    //        }
    //    }
    //    protected override CreateParams CreateParams
    //    {
    //        get
    //        {
    //            CreateParams cp = base.CreateParams;
    //            cp.ExStyle = cp.ExStyle | WS_EX_TRANSPARENT;
    //            return cp;
    //        }
    //    }
    //    protected override void OnPaint(PaintEventArgs e)
    //    {
    //        using (var brush = new SolidBrush(Color.FromArgb(this.opacity * 255 / 100, this.BackColor)))
    //        {
    //            e.Graphics.FillRectangle(brush, this.ClientRectangle);
    //        }
    //        base.OnPaint(e);
    //    }
    //}
}