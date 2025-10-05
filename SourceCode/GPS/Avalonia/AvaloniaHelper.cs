using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

namespace AgOpenGPS.Avalonia
{
    /// <summary>
    /// Helper class to integrate Avalonia UI into the WinForms application
    /// </summary>
    public static class AvaloniaHelper
    {
        private static bool _isInitialized = false;
        private static Thread? _avaloniaThread;
        private static ClassicDesktopStyleApplicationLifetime? _lifetime;

        /// <summary>
        /// Check if Avalonia is initialized
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Initialize Avalonia UI framework for embedding in WinForms
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;

            // Initialize Avalonia without starting the application lifetime
            // This allows embedding in WinForms
            AppBuilder.Configure<AvaloniaApp>()
                .UsePlatformDetect()
                .LogToTrace()
                .SetupWithoutStarting();
        }

        /// <summary>
        /// Initialize Avalonia with separate window support (legacy mode)
        /// </summary>
        public static void InitializeWithWindows()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;

            // Start Avalonia on a separate thread for standalone windows
            _avaloniaThread = new Thread(() =>
            {
                var builder = AppBuilder.Configure<AvaloniaApp>()
                    .UsePlatformDetect()
                    .LogToTrace();

                _lifetime = new ClassicDesktopStyleApplicationLifetime
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };

                builder.SetupWithLifetime(_lifetime);
                _lifetime.Start(new string[0]);
            });

            _avaloniaThread.SetApartmentState(ApartmentState.STA);
            _avaloniaThread.IsBackground = true;
            _avaloniaThread.Start();

            // Wait for Avalonia to initialize
            Thread.Sleep(1000);
        }


        /// <summary>
        /// Show the Avalonia settings window
        /// </summary>
        public static void ShowSettingsWindow()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            Dispatcher.UIThread.Post(() =>
            {
                var window = new AvaloniaSettingsWindow();
                window.Show();
            });
        }

        /// <summary>
        /// Show the Avalonia settings window modally
        /// </summary>
        public static void ShowSettingsWindowModal()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            Dispatcher.UIThread.Post(async () =>
            {
                var window = new AvaloniaSettingsWindow();
                await window.ShowDialog(null);
            });
        }
    }

    /// <summary>
    /// Minimal Avalonia application class
    /// </summary>
    public class AvaloniaApp : Application
    {
        public override void Initialize()
        {
            // Initialize Fluent theme
            Styles.Add(new FluentTheme());
        }

        public override void OnFrameworkInitializationCompleted()
        {
            base.OnFrameworkInitializationCompleted();
        }
    }
}
