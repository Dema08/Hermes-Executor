using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Highlighting;
using Hermes_Executor.Core;
using Microsoft.Win32;

namespace Hermes_Executor
{
    public partial class MainWindow : Window
    {
        private readonly Injector _injector;
        private readonly ScriptEngine _scriptEngine;
        private readonly DispatcherTimer _robloxCheckTimer;
        private bool _isInjected = false;

        public MainWindow()
        {
            InitializeComponent();

            _injector = new Injector();
            _scriptEngine = new ScriptEngine();

            _injector.OnLog += AddConsoleMessage;
            _scriptEngine.OnLog += AddConsoleMessage;

            // Timer for Roblox detection (every 2 seconds)
            _robloxCheckTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _robloxCheckTimer.Tick += RobloxCheckTimer_Tick;
            _robloxCheckTimer.Start();

            // Setup AvalonEdit sample text
            ScriptEditorControl.Text = "-- Welcome to Hermes Executor\nprint(\"Hello, Hermes!\")";
            
            // Track cursor position
            ScriptEditorControl.TextArea.Caret.PositionChanged += Caret_PositionChanged;

            AddConsoleMessage("Hermes Executor v1.0 initialized.");
            AddConsoleMessage("Type 'help' in console for available commands.");
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                Maximize_Click(sender, e);
            }
            else
            {
                DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void Inject_Click(object sender, RoutedEventArgs e)
        {
            BtnInject.IsEnabled = false;
            StatusDot.Fill = System.Windows.Media.Brushes.Orange;
            TxtStatus.Text = "Injecting...";

            bool success = await _injector.InjectAsync();
            if (success)
            {
                _isInjected = true;
                StatusDot.Fill = System.Windows.Media.Brushes.LimeGreen;
                TxtStatus.Text = "Injected";
                BtnInject.Content = "✦ INJECTED";
            }
            else
            {
                StatusDot.Fill = System.Windows.Media.Brushes.Red;
                TxtStatus.Text = "Injection Failed";
                BtnInject.IsEnabled = true;
            }
        }

        private async void Execute_Click(object sender, RoutedEventArgs e)
        {
            string script = ScriptEditorControl.Text;
            await _scriptEngine.ExecuteAsync(script);
        }

        private void LoadScript_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Lua Files (*.lua)|*.lua|Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                ScriptEditorControl.Text = File.ReadAllText(dlg.FileName);
                AddConsoleMessage($"Loaded script from {dlg.FileName}");
            }
        }

        private void SaveScript_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "Lua Files (*.lua)|*.lua|Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                File.WriteAllText(dlg.FileName, ScriptEditorControl.Text);
                AddConsoleMessage($"Saved script to {dlg.FileName}");
            }
        }

        private void ClipboardScript_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                ScriptEditorControl.Text = Clipboard.GetText();
                AddConsoleMessage("Loaded script from clipboard.");
            }
        }

        private void ClearScript_Click(object sender, RoutedEventArgs e)
        {
            ScriptEditorControl.Clear();
            AddConsoleMessage("Script editor cleared.");
        }

        private void ClearConsole_Click(object sender, RoutedEventArgs e)
        {
            ClearConsole();
        }

        private void AddConsoleMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                TxtConsoleOutput.AppendText($"[{timestamp}] {message}\n");
                TxtConsoleOutput.ScrollToEnd();
            });
        }

        private void ClearConsole()
        {
            TxtConsoleOutput.Clear();
        }

        private void ConsoleInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string cmd = TxtConsoleInput.Text.Trim();
                TxtConsoleInput.Clear();
                if (!string.IsNullOrEmpty(cmd))
                {
                    AddConsoleMessage($"> {cmd}");
                    ProcessConsoleCommand(cmd.ToLower());
                }
            }
        }

        private void ProcessConsoleCommand(string command)
        {
            switch (command)
            {
                case "help":
                    AddConsoleMessage("Available commands: help, status, inject, clear");
                    break;
                case "status":
                    AddConsoleMessage($"Injected: {_isInjected}, Roblox Online: {_injector.CheckRobloxRunning()}");
                    break;
                case "inject":
                    Inject_Click(this, new RoutedEventArgs());
                    break;
                case "clear":
                    ClearConsole();
                    break;
                default:
                    AddConsoleMessage($"Unknown command: {command}. Type 'help' for options.");
                    break;
            }
        }

        private void RobloxCheckTimer_Tick(object? sender, EventArgs e)
        {
            bool isRunning = _injector.CheckRobloxRunning();
            if (isRunning)
            {
                RobloxStatusDot.Fill = System.Windows.Media.Brushes.LimeGreen;
                TxtRobloxStatus.Text = "Online";
            }
            else
            {
                RobloxStatusDot.Fill = System.Windows.Media.Brushes.Red;
                TxtRobloxStatus.Text = "Offline";
            }
        }

        private void Caret_PositionChanged(object? sender, EventArgs e)
        {
            var caret = ScriptEditorControl.TextArea.Caret;
            TxtEditorStatus.Text = $"Ln: {caret.Line} | Col: {caret.Column}";
        }
    }
}
