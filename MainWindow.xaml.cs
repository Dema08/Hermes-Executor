using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Hermes_Executor.Core;
using Hermes_Executor.Models;
using Hermes_Executor.Views;
using Microsoft.Win32;

namespace Hermes_Executor
{
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; } = null!;

        private readonly Injector _injector;
        private readonly ScriptEngine _scriptEngine;
        private readonly DispatcherTimer _robloxCheckTimer;

        // Track last panel dimensions before collapse
        private double _lastSidebarWidth = 240;
        private double _lastConsoleWidth = 300;
        private double _lastBottomHeight = 150;

        private string _currentActivity = "Explorer";
        private bool _isSidebarOpen = true;

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();

            _injector = new Injector();
            _scriptEngine = new ScriptEngine();

            _injector.OnLog += AddConsoleMessage;
            _scriptEngine.OnLog += AddConsoleMessage;

            // Wire up execute button from ScriptEditor view
            ScriptEditorView.BtnExecute.Click += Execute_Click;
            ScriptEditorView.ScriptSaved += (filePath) => AddConsoleMessage($"Saved script to {filePath}");

            // Wire up console commands
            ConsoleViewPanel.OnCommandSubmitted += ProcessConsoleCommand;

            // Wire up Script Hub events
            ScriptHubViewControl.OpenInNewTabRequested += ScriptHub_OpenInNewTab;
            ScriptHubViewControl.ExecuteRequested += ScriptHub_ExecuteRequested;

            _robloxCheckTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _robloxCheckTimer.Tick += RobloxCheckTimer_Tick;
            _robloxCheckTimer.Start();

            // Hook window events
            Closing += MainWindow_Closing;
            Loaded += MainWindow_Loaded;

            AddConsoleMessage("Hermes Executor v1.0 initialized with Auto-Attach engine.");
            AddConsoleMessage("Type 'help' in console for available commands.");
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SettingsManager.Load();
            ApplySettings();
            SwitchActivity("Explorer"); // Default activity at startup
        }

        public void ApplySettings()
        {
            var settings = SettingsManager.Current;
            _lastSidebarWidth = settings.SidebarWidth;
            if (_lastSidebarWidth < 260) _lastSidebarWidth = 300;
            _lastConsoleWidth = settings.ConsoleWidth;
            _lastBottomHeight = settings.BottomPanelHeight;

            // Apply default configurations to editors
            foreach (var tab in ScriptEditorView.TabsList)
            {
                tab.Editor.FontSize = 13;
                tab.Editor.FontFamily = new System.Windows.Media.FontFamily("Consolas");
                tab.Editor.ShowLineNumbers = true;
                tab.Editor.WordWrap = false;
            }

            // Sync layout visibility settings
            ToggleSidebar(settings.ShowSidebar, true);
            ToggleConsole(settings.ShowConsole);
            ToggleBottomPanel(settings.ShowBottomPanel);
            
            ActivityBarGrid.Visibility = settings.ShowActivityBar ? Visibility.Visible : Visibility.Collapsed;
            ColActivityBar.Width = settings.ShowActivityBar ? new GridLength(48) : new GridLength(0);
        }

        // --- LAYOUT SWITCHING & COLLAPSIBILITY ---

        private void ActivityButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                string targetActivity = btn.Tag.ToString()!;
                if (targetActivity == "ScriptHub") targetActivity = "Script"; // Normalization

                if (targetActivity == _currentActivity)
                {
                    ToggleSidebar(!_isSidebarOpen);
                }
                else
                {
                    SwitchActivity(targetActivity);
                    ToggleSidebar(true);
                }
            }
        }

        private void SwitchActivity(string activity)
        {
            if (activity == "ScriptHub") activity = "Script"; // Compatibility
            _currentActivity = activity;

            // Reset foreground colors to inactive style
            var secBrush = (System.Windows.Media.Brush)Application.Current.Resources["TextSecondary"];
            ActBtnExplorer.Foreground = secBrush;
            ActBtnExecutor.Foreground = secBrush;
            ActBtnScriptHub.Foreground = secBrush;

            var goldBrush = (System.Windows.Media.Brush)Application.Current.Resources["HermesGold"];

            if (activity == "Explorer")
            {
                ActBtnExplorer.Foreground = goldBrush;
                TxtSidebarHeader.Text = "EXPLORER";

                ExplorerSidebarContent.Visibility = Visibility.Visible;
                ExecutorSidebarContent.Visibility = Visibility.Collapsed;

                EditorContainer.Visibility = Visibility.Visible;
                ScriptHubContainer.Visibility = Visibility.Collapsed;
            }
            else if (activity == "Executor")
            {
                ActBtnExecutor.Foreground = goldBrush;
                TxtSidebarHeader.Text = "EXECUTOR";

                ExplorerSidebarContent.Visibility = Visibility.Collapsed;
                ExecutorSidebarContent.Visibility = Visibility.Visible;

                EditorContainer.Visibility = Visibility.Visible;
                ScriptHubContainer.Visibility = Visibility.Collapsed;
            }
            else if (activity == "Script")
            {
                ActBtnScriptHub.Foreground = goldBrush;
                TxtSidebarHeader.Text = "SCRIPT";

                ExplorerSidebarContent.Visibility = Visibility.Visible;
                ExecutorSidebarContent.Visibility = Visibility.Collapsed;

                EditorContainer.Visibility = Visibility.Collapsed;
                ScriptHubContainer.Visibility = Visibility.Visible;

                ScriptHubViewControl.TriggerFadeIn();
            }
        }

        private void ToggleSidebar(bool show, bool force = false)
        {
            if (!force && _isSidebarOpen == show) return;
            _isSidebarOpen = show;

            // Sync layout preferences
            SettingsManager.Current.ShowSidebar = show;
            SettingsManager.Save();

            // Cancel any active animation
            SidebarBorder.BeginAnimation(WidthProperty, null);
            SidebarContentGrid.BeginAnimation(OpacityProperty, null);

            if (force)
            {
                if (show)
                {
                    ColSidebar.MinWidth = 260;
                    ColSidebar.MaxWidth = 420;
                    SidebarBorder.Visibility = Visibility.Visible;
                    SidebarSplitter.Visibility = Visibility.Visible;
                    ColSidebar.Width = new GridLength(_lastSidebarWidth);
                    SidebarBorder.Width = double.NaN;
                    SidebarContentGrid.Opacity = 1.0;
                }
                else
                {
                    ColSidebar.MinWidth = 0;
                    SidebarBorder.Visibility = Visibility.Collapsed;
                    SidebarSplitter.Visibility = Visibility.Collapsed;
                    ColSidebar.Width = new GridLength(0);
                    SidebarContentGrid.Opacity = 0.0;
                }
                return;
            }

            if (show)
            {
                ColSidebar.MinWidth = 260;
                ColSidebar.MaxWidth = 420;
                ColSidebar.Width = GridLength.Auto;
                SidebarBorder.Visibility = Visibility.Visible;
                SidebarSplitter.Visibility = Visibility.Visible;
                SidebarContentGrid.Opacity = 0.0;

                var animWidth = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0,
                    To = _lastSidebarWidth,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };

                animWidth.Completed += (s, e) =>
                {
                    SidebarBorder.BeginAnimation(WidthProperty, null); // Clear animation lock
                    ColSidebar.Width = new GridLength(_lastSidebarWidth);
                    SidebarBorder.Width = double.NaN; // Restore auto stretch
                    UpdateLayout(); // Force redraw layout
                };

                var animOpacity = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(150),
                    BeginTime = TimeSpan.FromMilliseconds(50),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };

                animOpacity.Completed += (s, e) =>
                {
                    SidebarContentGrid.BeginAnimation(OpacityProperty, null); // Clear opacity lock
                    SidebarContentGrid.Opacity = 1.0;
                    UpdateLayout();
                };

                SidebarBorder.BeginAnimation(WidthProperty, animWidth);
                SidebarContentGrid.BeginAnimation(OpacityProperty, animOpacity);

                if (_currentActivity == "Script")
                {
                    ScriptHubViewControl.TriggerFadeIn();
                }
            }
            else
            {
                if (ColSidebar.Width.IsAbsolute)
                {
                    _lastSidebarWidth = ColSidebar.Width.Value;
                }
                else if (SidebarBorder.ActualWidth > 0)
                {
                    _lastSidebarWidth = SidebarBorder.ActualWidth;
                }

                ColSidebar.MinWidth = 0;
                SidebarBorder.Width = _lastSidebarWidth;
                ColSidebar.Width = GridLength.Auto;

                var animWidth = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = _lastSidebarWidth,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };

                animWidth.Completed += (s, e) =>
                {
                    SidebarBorder.BeginAnimation(WidthProperty, null); // Clear animation lock
                    SidebarBorder.Visibility = Visibility.Collapsed;
                    SidebarSplitter.Visibility = Visibility.Collapsed;
                };

                var animOpacity = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = SidebarContentGrid.Opacity,
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(120),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };

                animOpacity.Completed += (s, e) =>
                {
                    SidebarContentGrid.BeginAnimation(OpacityProperty, null); // Clear opacity lock
                    SidebarContentGrid.Opacity = 0.0;
                };

                SidebarBorder.BeginAnimation(WidthProperty, animWidth);
                SidebarContentGrid.BeginAnimation(OpacityProperty, animOpacity);
            }
        }

        private void ToggleConsole(bool show)
        {
            if (show)
            {
                ColConsole.MinWidth = 200;
                ColConsole.MaxWidth = 500;
                ConsoleContainer.Visibility = Visibility.Visible;
                ConsoleSplitter.Visibility = Visibility.Visible;
                ColConsole.Width = new GridLength(_lastConsoleWidth);
            }
            else
            {
                if (ColConsole.Width.IsAbsolute && ColConsole.Width.Value > 0)
                {
                    _lastConsoleWidth = ColConsole.Width.Value;
                }
                ColConsole.MinWidth = 0;
                ConsoleContainer.Visibility = Visibility.Collapsed;
                ConsoleSplitter.Visibility = Visibility.Collapsed;
                ColConsole.Width = new GridLength(0);
            }
        }

        private void ToggleBottomPanel(bool show)
        {
            if (show)
            {
                BottomPanelBorder.Visibility = Visibility.Visible;
                BottomSplitter.Visibility = Visibility.Visible;
                RowBottomPanel.Height = new GridLength(_lastBottomHeight);
            }
            else
            {
                if (RowBottomPanel.Height.Value > 0)
                {
                    _lastBottomHeight = RowBottomPanel.Height.Value;
                }
                BottomPanelBorder.Visibility = Visibility.Collapsed;
                BottomSplitter.Visibility = Visibility.Collapsed;
                RowBottomPanel.Height = new GridLength(0);
            }
        }

        private void CollapseSidebar_Click(object sender, RoutedEventArgs e) => ToggleSidebar(false);
        private void CollapseConsole_Click(object sender, RoutedEventArgs e) => ToggleConsole(false);
        private void CollapseBottomPanel_Click(object sender, RoutedEventArgs e) => ToggleBottomPanel(false);

        // --- BUTTONS EVENTS & SHIELD INTERACTION ---

        private void ScriptHubButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchActivity("ScriptHub");
        }

        private void EditorButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchActivity("Explorer");
        }

        private void ScriptHub_OpenInNewTab(ScriptItem script)
        {
            if (string.IsNullOrWhiteSpace(script.Script))
            {
                MessageBox.Show(
                    "Source script tidak tersedia pada hasil ini.",
                    "Hermes Script Hub",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            ScriptEditorView.OpenNewTab(script.Title, script.Script);
            SwitchActivity("Explorer");
            AddConsoleMessage($"Loaded script from Script Hub: {script.Title}");
        }

        private async void ScriptHub_ExecuteRequested(ScriptItem script)
        {
            if (string.IsNullOrWhiteSpace(script.Script))
            {
                MessageBox.Show(
                    "Source script tidak tersedia pada hasil ini.",
                    "Hermes Script Hub",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                await _scriptEngine.ExecuteAsync(script.Script);
                AddConsoleMessage($"Executed script from Script Hub: {script.Title}");
            }
            catch (Exception ex)
            {
                AddConsoleMessage($"Execute failed: {ex.Message}");
            }
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
                try
                {
                    ScriptEditorView.OpenNewTab(Path.GetFileName(dlg.FileName), File.ReadAllText(dlg.FileName), dlg.FileName);
                    AddConsoleMessage($"Loaded script from {dlg.FileName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Gagal membuka file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveScript_Click(object sender, RoutedEventArgs e)
        {
            if (ScriptEditorView.ActiveTab != null)
            {
                ScriptEditorView.SaveTab(ScriptEditorView.ActiveTab, false);
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
            
            // Append to Bottom panel OUTPUT tab
            Dispatcher.Invoke(() =>
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                TxtOutputLog.AppendText($"[{timestamp}] {message}\n");
                TxtOutputLog.ScrollToEnd();
            });
        }

        private void ProcessConsoleCommand(string command)
        {
            switch (command)
            {
                case "help":
                    AddConsoleMessage("Available commands: help, status, clear");
                    break;
                case "status":
                    AddConsoleMessage($"Hermes-Executor running normally with Roblox Online: {_injector.CheckRobloxRunning()}.");
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

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.N:
                        e.Handled = true;
                        ScriptEditorView.AddNewTab();
                        break;
                    case Key.W:
                        e.Handled = true;
                        if (ScriptEditorView.ActiveTab != null)
                        {
                            ScriptEditorView.PromptAndCloseTab(ScriptEditorView.ActiveTab);
                        }
                        break;
                    case Key.S:
                        e.Handled = true;
                        if (ScriptEditorView.ActiveTab != null)
                        {
                            ScriptEditorView.SaveTab(ScriptEditorView.ActiveTab, false);
                        }
                        break;
                    case Key.O:
                        e.Handled = true;
                        LoadScript_Click(this, new RoutedEventArgs());
                        break;
                    case Key.Tab:
                        e.Handled = true;
                        if (ScriptEditorView.TabsList.Count > 1)
                        {
                            int nextIndex = (ScriptEditorView.Tabs.SelectedIndex + 1) % ScriptEditorView.TabsList.Count;
                            ScriptEditorView.Tabs.SelectedIndex = nextIndex;
                        }
                        break;
                }
            }
            else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                if (e.Key == Key.S)
                {
                    e.Handled = true;
                    if (ScriptEditorView.ActiveTab != null)
                    {
                        ScriptEditorView.SaveTab(ScriptEditorView.ActiveTab, true);
                    }
                }
                else if (e.Key == Key.Tab)
                {
                    e.Handled = true;
                    if (ScriptEditorView.TabsList.Count > 1)
                    {
                        int prevIndex = (ScriptEditorView.Tabs.SelectedIndex - 1 + ScriptEditorView.TabsList.Count) % ScriptEditorView.TabsList.Count;
                        ScriptEditorView.Tabs.SelectedIndex = prevIndex;
                    }
                }
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save layout parameters
            if (ColSidebar.Width.IsAbsolute && ColSidebar.Width.Value > 0)
            {
                SettingsManager.Current.SidebarWidth = ColSidebar.Width.Value;
            }
            else
            {
                SettingsManager.Current.SidebarWidth = _lastSidebarWidth;
            }

            if (ColConsole.Width.IsAbsolute && ColConsole.Width.Value > 0)
            {
                SettingsManager.Current.ConsoleWidth = ColConsole.Width.Value;
            }
            else
            {
                SettingsManager.Current.ConsoleWidth = _lastConsoleWidth;
            }

            if (RowBottomPanel.Height.IsAbsolute && RowBottomPanel.Height.Value > 0)
            {
                SettingsManager.Current.BottomPanelHeight = RowBottomPanel.Height.Value;
            }
            else
            {
                SettingsManager.Current.BottomPanelHeight = _lastBottomHeight;
            }

            SettingsManager.Current.ShowSidebar = SidebarBorder.Visibility == Visibility.Visible;
            SettingsManager.Current.ShowConsole = ConsoleContainer.Visibility == Visibility.Visible;
            SettingsManager.Current.ShowBottomPanel = BottomPanelBorder.Visibility == Visibility.Visible;

            SettingsManager.Save();

            // Save active tab session
            ScriptEditorView.SaveSessionImmediate();
        }
    }
}
