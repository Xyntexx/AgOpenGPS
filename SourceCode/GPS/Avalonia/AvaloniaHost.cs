using System;
using System.Windows.Forms;

namespace AgOpenGPS.Avalonia
{
    /// <summary>
    /// Windows Forms control that shows information about Avalonia embedding
    /// True embedding in .NET Framework 4.8 is not available with standard Avalonia packages
    /// </summary>
    public class AvaloniaHost : Control
    {
        public AvaloniaHost()
        {
            InitializeHosting();
        }

        private void InitializeHosting()
        {
            try
            {
                // Show information about Avalonia embedding limitations
                var messageLabel = new Label
                {
                    Text = "Avalonia Embedding Information\n\n" +
                           "True embedding of Avalonia in .NET Framework 4.8 WinForms is not currently available " +
                           "with the standard Avalonia packages.\n\n" +
                           "Options:\n" +
                           "1. Use separate Avalonia windows (already implemented)\n" +
                           "2. Upgrade to .NET 6+ for better embedding support\n" +
                           "3. Use Avalonia XPF (commercial solution for WPF/WinForms embedding)\n\n" +
                           "The demo includes a working example of opening Avalonia windows from WinForms.",
                    Dock = DockStyle.Fill,
                    Font = new System.Drawing.Font("Microsoft Sans Serif", 9F),
                    Padding = new Padding(20),
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter
                };

                Controls.Add(messageLabel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AvaloniaHost: {ex.Message}");
            }
        }
    }
}
