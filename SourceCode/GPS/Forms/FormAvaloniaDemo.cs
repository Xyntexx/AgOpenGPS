using System;
using System.Windows.Forms;

namespace AgOpenGPS
{
    /// <summary>
    /// Demo form to showcase embedded Avalonia integration
    /// </summary>
    public partial class FormAvaloniaDemo : Form
    {
        public FormAvaloniaDemo()
        {
            InitializeComponent();
            InitializeAvalonia();
        }

        private void InitializeAvalonia()
        {
            try
            {
                // Create the host control (shows embedding information)
                _avaloniaHost = new Avalonia.AvaloniaHost
                {
                    Dock = DockStyle.Fill
                };

                // Add to the panel
                panelAvaloniaHost.Controls.Add(_avaloniaHost);

                lblStatus.Text = "Status: See panel for embedding information";
                lblStatus.ForeColor = System.Drawing.Color.Blue;
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Status: Error - {ex.Message}";
                lblStatus.ForeColor = System.Drawing.Color.Red;
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
