using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Hermes_Executor.Core;

namespace Hermes_Executor.Controls
{
    public partial class AutoAttachControl : UserControl
    {
        private readonly AutoInjector _injector;
        private readonly BrushConverter _brushConverter;

        public AutoAttachControl()
        {
            InitializeComponent();
            _brushConverter = new BrushConverter();
            
            _injector = new AutoInjector();
            _injector.OnStatusChanged += UpdateStatus;
            _injector.OnLog += AddLog;
            _injector.OnInjectionResult += InjectionResult;
        }

        private void UpdateStatus(AutoInjector.RobloxStatus status)
        {
            Dispatcher.Invoke(() =>
            {
                Brush redBrush = Brushes.Red;
                Brush orangeBrush = Brushes.Orange;
                Brush greenBrush = Brushes.Green;
                Brush limeBrush = Brushes.LimeGreen;

                if (_brushConverter.ConvertFromString("#FF4444") is Brush b1) redBrush = b1;
                if (_brushConverter.ConvertFromString("#FFAA00") is Brush b2) orangeBrush = b2;
                if (_brushConverter.ConvertFromString("#00FF00") is Brush b3) greenBrush = b3;
                if (_brushConverter.ConvertFromString("#00FF44") is Brush b4) limeBrush = b4;

                switch (status)
                {
                    case AutoInjector.RobloxStatus.Offline:
                        StatusDot.Fill = redBrush;
                        StatusText.Text = "OFFLINE";
                        StatusText.Foreground = redBrush;
                        StatusDetail.Text = "Roblox tidak terdeteksi";
                        StatusIndicator.BorderBrush = redBrush;
                        break;

                    case AutoInjector.RobloxStatus.Detecting:
                        StatusDot.Fill = orangeBrush;
                        StatusText.Text = "DETECTING";
                        StatusText.Foreground = orangeBrush;
                        StatusDetail.Text = "Mencari Roblox...";
                        StatusIndicator.BorderBrush = orangeBrush;
                        break;

                    case AutoInjector.RobloxStatus.Online:
                        StatusDot.Fill = greenBrush;
                        StatusText.Text = "ONLINE";
                        StatusText.Foreground = greenBrush;
                        StatusDetail.Text = "Roblox terdeteksi, siap inject";
                        StatusIndicator.BorderBrush = greenBrush;
                        break;

                    case AutoInjector.RobloxStatus.Injecting:
                        StatusDot.Fill = orangeBrush;
                        StatusText.Text = "INJECTING";
                        StatusText.Foreground = orangeBrush;
                        StatusDetail.Text = "Sedang inject...";
                        StatusIndicator.BorderBrush = orangeBrush;
                        break;

                    case AutoInjector.RobloxStatus.Injected:
                        StatusDot.Fill = limeBrush;
                        StatusText.Text = "INJECTED ✦";
                        StatusText.Foreground = limeBrush;
                        StatusDetail.Text = "Hermes aktif!";
                        StatusIndicator.BorderBrush = limeBrush;
                        break;

                    case AutoInjector.RobloxStatus.Failed:
                        StatusDot.Fill = redBrush;
                        StatusText.Text = "FAILED";
                        StatusText.Foreground = redBrush;
                        StatusDetail.Text = "Inject gagal, coba manual";
                        StatusIndicator.BorderBrush = redBrush;
                        break;
                }

                InstanceInfo.Text = _injector.GetRobloxInstanceInfo();
            });
        }

        private void AddLog(string message)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.AddConsoleMessage(message);
        }

        private void InjectionResult(bool success)
        {
            Dispatcher.Invoke(() =>
            {
                if (success)
                {
                    UpdateStatus(AutoInjector.RobloxStatus.Injected);
                }
                else
                {
                    UpdateStatus(AutoInjector.RobloxStatus.Failed);
                }
            });
        }

        private void AutoAttach_Checked(object sender, RoutedEventArgs e)
        {
            _injector.StartAutoAttach();
            AddLog("🔗 Auto-Attach enabled");
        }

        private void AutoAttach_Unchecked(object sender, RoutedEventArgs e)
        {
            _injector.StopAutoAttach();
            AddLog("🔗 Auto-Attach disabled");
        }

        private async void InjectButton_Click(object sender, RoutedEventArgs e)
        {
            await _injector.InjectAsync();
        }

        private void KillRoblox_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Kill Roblox process?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                _injector.KillRoblox();
            }
        }
    }
}
