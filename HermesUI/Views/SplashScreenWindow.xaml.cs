using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Hermes_Executor.Views
{
    public partial class SplashScreenWindow : Window
    {
        private readonly DispatcherTimer _loadingTimer;
        private int _currentProgress = 0;

        public SplashScreenWindow()
        {
            InitializeComponent();
            Loaded += Window_Loaded;

            _loadingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(35)
            };
            _loadingTimer.Tick += LoadingTimer_Tick;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _loadingTimer.Start();
        }

        private void LoadingTimer_Tick(object? sender, EventArgs e)
        {
            _currentProgress += 1;

            if (_currentProgress <= 100)
            {
                UpdateProgressUI(_currentProgress);
            }

            if (_currentProgress >= 100)
            {
                _loadingTimer.Stop();
                CompleteLoading();
            }
        }

        private void UpdateProgressUI(int progress)
        {
            TxtPercentage.Text = $"{progress}%";

            // Update status text based on progress stage
            if (progress < 25)
            {
                TxtLoadingStatus.Text = "Initializing Hermes Core Engine...";
            }
            else if (progress < 50)
            {
                TxtLoadingStatus.Text = "Scanning Roblox Process & Hooks...";
            }
            else if (progress < 75)
            {
                TxtLoadingStatus.Text = "Loading Lua Script Library & AvalonEdit...";
            }
            else if (progress < 95)
            {
                TxtLoadingStatus.Text = "Establishing Security Channels & Environment...";
            }
            else
            {
                TxtLoadingStatus.Text = "System Ready! Launching Hermes Executor...";
            }

            // Track width calculation
            double trackWidth = ProgressBarTrack.ActualWidth > 0 ? ProgressBarTrack.ActualWidth : 580;
            double calculatedWidth = (trackWidth * progress) / 100.0;

            DoubleAnimation anim = new DoubleAnimation
            {
                To = calculatedWidth,
                Duration = TimeSpan.FromMilliseconds(50),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            ProgressBarFill.BeginAnimation(WidthProperty, anim);
        }

        private async void CompleteLoading()
        {
            await Task.Delay(300);

            // Fade out splash screen
            DoubleAnimation fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(450),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            fadeOut.Completed += (s, e) =>
            {
                MainWindow mainWindow = new MainWindow();
                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();
                Close();
            };

            RootGrid.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
