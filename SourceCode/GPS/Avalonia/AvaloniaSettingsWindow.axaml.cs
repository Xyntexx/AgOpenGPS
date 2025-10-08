using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace AgOpenGPS.Avalonia
{
    /// <summary>
    /// Avalonia settings window (code-based, no AXAML)
    /// </summary>
    public class AvaloniaSettingsWindow : Window
    {
        public AvaloniaSettingsWindow()
        {
            Title = "Avalonia Settings Panel";
            Width = 600;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            BuildContent();
        }

        private void BuildContent()
        {
            var mainPanel = new StackPanel
            {
                Spacing = 15,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Header
            mainPanel.Children.Add(new TextBlock
            {
                Text = "Avalonia UI Integration",
                FontSize = 24,
                FontWeight = global::Avalonia.Media.FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            mainPanel.Children.Add(new TextBlock
            {
                Text = "This is a modern Avalonia window running alongside WinForms",
                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                MaxWidth = 400
            });

            mainPanel.Children.Add(new TextBlock
            {
                Text = "Settings and controls would go here...",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new global::Avalonia.Thickness(0, 20, 0, 0)
            });

            // Close Button
            var closeButton = new Button
            {
                Content = "Close",
                Width = 100,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new global::Avalonia.Thickness(0, 30, 0, 0)
            };
            closeButton.Click += (s, e) => Close();
            mainPanel.Children.Add(closeButton);

            Content = mainPanel;
        }
    }
}
