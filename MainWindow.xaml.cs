using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Hermes_Executor.Core;
using Hermes_Executor.Views;
using Microsoft.Win32;

namespace Hermes_Executor
{
    public partial class MainWindow : Window
    {
        private readonly Injector _injector;
        private readonly ScriptEngine _scriptEngine;

        public MainWindow()
        {
            InitializeComponent();

            _injector = new Injector();
            _scriptEngine = new ScriptEngine();

            _injector.OnLog += AddConsoleMessage;
            _scriptEngine.OnLog += AddConsoleMessage;

            // Wire up execute button from ScriptEditor view
            ScriptEditorView.BtnExecute.Click += Execute_Click;

            // Wire up console commands
            ConsoleViewPanel.OnCommandSubmitted += ProcessConsoleCommand;

            AddConsoleMessage("Hermes Executor v1.0 initialized with Auto-Attach engine.");
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

        private async void Execute_Click(object sender, RoutedEventArgs e)
        {
            string script = ScriptEditorView.GetScriptText();
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
                ScriptEditorView.SetScriptText(File.ReadAllText(dlg.FileName));
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
                File.WriteAllText(dlg.FileName, ScriptEditorView.GetScriptText());
                AddConsoleMessage($"Saved script to {dlg.FileName}");
            }
        }

        private void ClipboardScript_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                ScriptEditorView.SetScriptText(Clipboard.GetText());
                AddConsoleMessage("Loaded script from clipboard.");
            }
        }

        private void ClearScript_Click(object sender, RoutedEventArgs e)
        {
            ScriptEditorView.Clear();
            AddConsoleMessage("Script editor cleared.");
        }

        public void AddConsoleMessage(string message)
        {
            ConsoleViewPanel.AppendMessage(message);
        }

        private void ProcessConsoleCommand(string command)
        {
            switch (command)
            {
                case "help":
                    AddConsoleMessage("Available commands: help, status, clear");
                    break;
                case "status":
                    AddConsoleMessage("Hermes-Executor running normally with Auto-Attach active.");
                    break;
                case "clear":
                    ConsoleViewPanel.Clear();
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

        private void ShowSplash_Click(object sender, RoutedEventArgs e)
        {
            SplashScreenWindow splash = new SplashScreenWindow();
            splash.Show();
        }
    }
}
