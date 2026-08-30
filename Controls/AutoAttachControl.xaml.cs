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
                switch (status)
                {
                    case AutoInjector.RobloxStatus.Offline:
                        StatusDot.Fill = (Brush)_brushConverter.ConvertFromString("#FF4444");
                        StatusText.Text = "OFFLINE";
                        StatusText.Foreground = (Brush)_brushConverter.ConvertFromString("#FF4444");
                        StatusDetail.Text = "Roblox tidak terdeteksi";
                        StatusIndicator.BorderBrush = (Brush)_brushConverter.ConvertFromString("#FF4444");
                        break;

                    case AutoInjector.RobloxStatus.Detecting:
                        StatusDot.Fill = (Brush)_brushConverter.ConvertFromString("#FFAA00");
                        StatusText.Text = "DETECTING";
                        StatusText.Foreground = (Brush)_brushConverter.ConvertFromString("#FFAA00");
                        StatusDetail.Text = "Mencari Roblox...";
                        StatusIndicator.BorderBrush = (Brush)_brushConverter.ConvertFromString("#FFAA00");
                        break;

                    case AutoInjector.RobloxStatus.Online:
                        StatusDot.Fill = (Brush)_brushConverter.ConvertFromString("#00FF00");
                        StatusText.Text = "ONLINE";
                        StatusText.Foreground = (Brush)_brushConverter.ConvertFromString("#00FF00");
                        StatusDetail.Text = "Roblox terdeteksi, siap inject";
                        StatusIndicator.BorderBrush = (Brush)_brushConverter.ConvertFromString("#00FF00");
                        break;

                    case AutoInjector.RobloxStatus.Injecting:
                        StatusDot.Fill = (Brush)_brushConverter.ConvertFromString("#FFAA00");
                        StatusText.Text = "INJECTING";
                        StatusText.Foreground = (Brush)_brushConverter.ConvertFromString("#FFAA00");
                        StatusDetail.Text = "Sedang inject...";
                        StatusIndicator.BorderBrush = (Brush)_brushConverter.ConvertFromString("#FFAA00");
                        break;

                    case AutoInjector.RobloxStatus.Injected:
                        StatusDot.Fill = (Brush)_brushConverter.ConvertFromString("#00FF44");
                        StatusText.Text = "INJECTED ✦";
                        StatusText.Foreground = (Brush)_brushConverter.ConvertFromString("#00FF44");
                        StatusDetail.Text = "Hermes aktif!";
                        StatusIndicator.BorderBrush = (Brush)_brushConverter.ConvertFromString("#00FF44");
                        break;

                    case AutoInjector.RobloxStatus.Failed:
                        StatusDot.Fill = (Brush)_brushConverter.ConvertFromString("#FF4444");
                        StatusText.Text = "FAILED";
                        StatusText.Foreground = (Brush)_brushConverter.ConvertFromString("#FF4444");
                        StatusDetail.Text = "Inject gagal, coba manual";
                        StatusIndicator.BorderBrush = (Brush)_brushConverter.ConvertFromString("#FF4444");
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
