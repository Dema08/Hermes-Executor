using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Hermes_Executor.Core;
using Hermes_Executor.Models;
using Hermes_Executor.Views;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;

namespace Hermes_Executor {
    public partial class MainWindow : Window {
        public static MainWindow Instance { get; private set; } = null!;

        // Komponen UI yang digunakan (di-bind via FindName agar aman dari error Roslyn / VS Code)
        private Border? _sidebarBorder;
        private Grid? _sidebarContentGrid;
        private ColumnDefinition? _colSidebar;
        private GridSplitter? _sidebarSplitter;

        private Grid? _consoleContainer;
        private ColumnDefinition? _colConsole;
        private GridSplitter? _consoleSplitter;
        private ConsoleView? _consoleViewPanel;

        private Border? _bottomPanelBorder;
        private GridSplitter? _bottomSplitter;
        private RowDefinition? _rowBottomPanel;
        private TextBox? _txtOutputLog;

        private Grid? _editorContainer;
        private Grid? _scriptHubContainer;
        private ScriptEditor? _scriptEditorView;
        private ScriptHubView? _scriptHubViewControl;

        private Grid? _explorerSidebarContent;
        private Grid? _executorSidebarContent;
        private Grid? _activityBarGrid;
        private ColumnDefinition? _colActivityBar;

        private Button? _actBtnExplorer;
        private Button? _actBtnExecutor;
        private Button? _actBtnScriptHub;
        private TextBlock? _txtSidebarHeader;

        private Ellipse? _robloxStatusDot;
        private TextBlock? _txtRobloxStatus;
        private Button? _btnInject;
        private Button? _btnExecute;

        private double _lastSidebarWidth = 240;
        private double _lastConsoleWidth = 300;
        private double _lastBottomHeight = 150;

        private readonly Injector _injector;
        private readonly DispatcherTimer _robloxCheckTimer;
        private string _currentActivity = "Explorer";
        private bool _isSidebarOpen = true;

        public MainWindow() {
            Instance = this;
            InitializeComponent();
            InitializeComponents();
            LoadSettings();
            InitializeConsole();
            CheckAdminStatus();

            _injector = new Injector();
            _injector.OnLog += AddConsoleMessage;
            ScriptEngine.OnLog += AddConsoleMessage;

            if (_scriptEditorView != null) {
                _scriptEditorView.ExecuteClicked += BtnExecute_Click;
                _scriptEditorView.ScriptSaved += (filePath) => AddConsoleMessage($"Saved script to {filePath}");
            }

            if (_consoleViewPanel != null) {
                _consoleViewPanel.OnCommandSubmitted += ProcessConsoleCommand;
            }

            if (_scriptHubViewControl != null) {
                _scriptHubViewControl.OpenInNewTabRequested += ScriptHub_OpenInNewTab;
                _scriptHubViewControl.ExecuteRequested += ScriptHub_ExecuteRequested;
            }

            _robloxCheckTimer = new DispatcherTimer {
                Interval = TimeSpan.FromSeconds(2)
            };
            _robloxCheckTimer.Tick += RobloxCheckTimer_Tick;
            _robloxCheckTimer.Start();

            Closing += MainWindow_Closing;
            Loaded += MainWindow_Loaded;
        }

        private void InitializeComponents() {
            // Ambil referensi ke semua komponen UI dari XAML secara eksplisit
            _sidebarBorder = FindName("SidebarBorder") as Border;
            _sidebarContentGrid = FindName("SidebarContentGrid") as Grid;
            _colSidebar = FindName("ColSidebar") as ColumnDefinition;
            _sidebarSplitter = FindName("SidebarSplitter") as GridSplitter;

            _consoleContainer = FindName("ConsoleContainer") as Grid;
            _colConsole = FindName("ColConsole") as ColumnDefinition;
            _consoleSplitter = FindName("ConsoleSplitter") as GridSplitter;
            _consoleViewPanel = (FindName("ConsoleViewPanel") ?? FindName("ConsoleView")) as ConsoleView;

            _bottomPanelBorder = FindName("BottomPanelBorder") as Border;
            _bottomSplitter = FindName("BottomSplitter") as GridSplitter;
            _rowBottomPanel = FindName("RowBottomPanel") as RowDefinition;
            _txtOutputLog = FindName("TxtOutputLog") as TextBox;

            _editorContainer = FindName("EditorContainer") as Grid;
            _scriptHubContainer = FindName("ScriptHubContainer") as Grid;
            _scriptEditorView = FindName("ScriptEditorView") as ScriptEditor;
            _scriptHubViewControl = FindName("ScriptHubViewControl") as ScriptHubView;

            _explorerSidebarContent = FindName("ExplorerSidebarContent") as Grid;
            _executorSidebarContent = FindName("ExecutorSidebarContent") as Grid;
            _activityBarGrid = FindName("ActivityBarGrid") as Grid;
            _colActivityBar = FindName("ColActivityBar") as ColumnDefinition;

            _actBtnExplorer = FindName("ActBtnExplorer") as Button;
            _actBtnExecutor = FindName("ActBtnExecutor") as Button;
            _actBtnScriptHub = FindName("ActBtnScriptHub") as Button;
            _txtSidebarHeader = FindName("TxtSidebarHeader") as TextBlock;

            _robloxStatusDot = FindName("RobloxStatusDot") as Ellipse;
            _txtRobloxStatus = FindName("TxtRobloxStatus") as TextBlock;
            _btnInject = FindName("BtnInject") as Button;
            _btnExecute = FindName("BtnExecute") as Button;
        }

        private void LoadSettings() {
            // Load settings dari SettingsManager
            SettingsManager.Load();
            var settings = SettingsManager.Current;

            if (_sidebarBorder != null) {
                _sidebarBorder.Visibility = settings.ShowSidebar ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_consoleContainer != null) {
                _consoleContainer.Visibility = settings.ShowConsole ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_bottomPanelBorder != null) {
                _bottomPanelBorder.Visibility = settings.ShowBottomPanel ? Visibility.Visible : Visibility.Collapsed;
            }

            _lastConsoleWidth = settings.ConsoleWidth;
            _lastSidebarWidth = settings.SidebarWidth;
            _lastBottomHeight = settings.BottomPanelHeight;
        }

        private void InitializeConsole() {
            // Inisialisasi console
            _consoleViewPanel?.Clear();
            _consoleViewPanel?.AppendText($"[{DateTime.Now:HH:mm:ss}] Hermes Executor v1.0 initialized with Auto-Attach engine.\n");
            _consoleViewPanel?.AppendText($"[{DateTime.Now:HH:mm:ss}] Type 'help' in console for available commands.\n");
        }

        private void CheckAdminStatus() {
            using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent()) {
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                if (principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator)) {
                    _consoleViewPanel?.AppendText($"[{DateTime.Now:HH:mm:ss}] ✅ Running as Administrator\n");
                } else {
                    _consoleViewPanel?.AppendText($"[{DateTime.Now:HH:mm:ss}] ⚠️ WARNING: Not running as Administrator! Injection may fail.\n");
                }
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            ApplySettings();
            SwitchActivity("Explorer");
        }

        public void ApplySettings() {
            var settings = SettingsManager.Current;
            _lastSidebarWidth = settings.SidebarWidth;
            if (_lastSidebarWidth < 260) _lastSidebarWidth = 300;
            _lastConsoleWidth = settings.ConsoleWidth;
            _lastBottomHeight = settings.BottomPanelHeight;

            if (_scriptEditorView != null) {
                foreach (var tab in _scriptEditorView.TabsList) {
                    tab.Editor.FontSize = 13;
                    tab.Editor.FontFamily = new System.Windows.Media.FontFamily("Consolas");
                    tab.Editor.ShowLineNumbers = true;
                    tab.Editor.WordWrap = false;
                }
            }

            ToggleSidebar(settings.ShowSidebar, true);
            ToggleConsole(settings.ShowConsole);
            ToggleBottomPanel(settings.ShowBottomPanel);

            if (_activityBarGrid != null) {
                _activityBarGrid.Visibility = settings.ShowActivityBar ? Visibility.Visible : Visibility.Collapsed;
            }
            if (_colActivityBar != null) {
                _colActivityBar.Width = settings.ShowActivityBar ? new GridLength(48) : new GridLength(0);
            }
        }

        // ==============================================
        // EVENT INJECT
        // ==============================================
        public async void BtnInject_Click(object sender, RoutedEventArgs e) {
            try {
                _consoleViewPanel?.AppendText($"[{DateTime.Now:HH:mm:ss}] 💉 Injecting into process...\n");

                if (_btnInject != null) _btnInject.IsEnabled = false;

                bool success = await Task.Run(() => ScriptEngine.Inject());
                bool isInjected = ScriptEngine.IsConnected;

                _consoleViewPanel?.AppendText($"[{DateTime.Now:HH:mm:ss}] 📊 IsConnected = {isInjected}\n");
                _consoleViewPanel?.AppendText($"[{DateTime.Now:HH:mm:ss}] 📊 IsInjected = {isInjected}\n");

                if (success && isInjected) {
                    _consoleViewPanel?.AppendText($"[{DateTime.Now:HH:mm:ss}] ✅ Successfully injected! (REAL)\n");

                    if (_btnInject != null) {
                        _btnInject.Content = "✅ Injected";
                        _btnInject.Background = new SolidColorBrush(Colors.Green);
                    }
                } else {
                    _consoleViewPanel?.AppendText($"[{DateTime.Now:HH:mm:ss}] ❌ Injection failed!\n");
                }
            } catch (Exception ex) {
                _consoleViewPanel?.AppendText($"[{DateTime.Now:HH:mm:ss}] ❌ Error: {ex.Message}\n");
            } finally {
                if (_btnInject != null) _btnInject.IsEnabled = true;
            }
        }

        // ==============================================
        // EVENT EXECUTE SCRIPT
        // ==============================================
        public async void BtnExecute_Click(object sender, RoutedEventArgs e) {
            try {
                string script = _scriptEditorView?.GetText() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(script)) {
                    _consoleViewPanel?.AppendText($"[{DateTime.Now:HH:mm:ss}] ❌ No script to execute\n");
                    return;
                }

                _consoleViewPanel?.AppendText($"[{DateTime.Now:HH:mm:ss}] 📊 IsConnected = {ScriptEngine.IsConnected}\n");
                _consoleViewPanel?.AppendText($"[{DateTime.Now:HH:mm:ss}] 📊 IsInjected = {ScriptEngine.IsInjectedStatus()}\n");

                if (!ScriptEngine.IsConnected) {
                    _consoleViewPanel?.AppendText($"[{DateTime.Now:HH:mm:ss}] ⚠️ Not injected into Roblox! Please inject first.\n");
                    return;
                }

                _consoleViewPanel?.AppendText($"[{DateTime.Now:HH:mm:ss}] 📜 Executing script ({script.Length} chars)...\n");

                bool success = await Task.Run(() => ScriptEngine.Execute(script));

                if (success) {
                    _consoleViewPanel?.AppendText($"[{DateTime.Now:HH:mm:ss}] ✅ Script executed successfully!\n");
                } else {
                    _consoleViewPanel?.AppendText($"[{DateTime.Now:HH:mm:ss}] ❌ Execution failed: {ScriptEngine.LastError}\n");
                }
            } catch (Exception ex) {
                _consoleViewPanel?.AppendText($"[{DateTime.Now:HH:mm:ss}] ❌ Error: {ex.Message}\n");
            }
        }

        private void Execute_Click(object sender, RoutedEventArgs e) => BtnExecute_Click(sender, e);

        // ==============================================
        // EVENT LOAD SCRIPT FROM FILE
        // ==============================================
        public void BtnLoadScript_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog {
                Filter = "Lua Scripts (*.lua;*.luau)|*.lua;*.luau|All Files (*.*)|*.*",
                Title = "Load Script"
            };

            if (openFileDialog.ShowDialog() == true) {
                try {
                    string script = File.ReadAllText(openFileDialog.FileName);
                    if (_scriptEditorView != null) {
                        _scriptEditorView.OpenNewTab(System.IO.Path.GetFileName(openFileDialog.FileName), script, openFileDialog.FileName);
                    }
                    _consoleViewPanel?.AppendText($"[{DateTime.Now:HH:mm:ss}] 📂 Loaded script: {System.IO.Path.GetFileName(openFileDialog.FileName)}\n");
                } catch (Exception ex) {
                    _consoleViewPanel?.AppendText($"[{DateTime.Now:HH:mm:ss}] ❌ Failed to load script: {ex.Message}\n");
                }
            }
        }

        private void LoadScript_Click(object sender, RoutedEventArgs e) => BtnLoadScript_Click(sender, e);

        private void SaveScript_Click(object sender, RoutedEventArgs e) {
            if (_scriptEditorView?.ActiveTab != null) {
                _scriptEditorView.SaveTab(_scriptEditorView.ActiveTab, false);
            }
        }

        private void ClipboardScript_Click(object sender, RoutedEventArgs e) {
            if (Clipboard.ContainsText()) {
                _scriptEditorView?.SetText(Clipboard.GetText());
                AddConsoleMessage("Loaded script from clipboard.");
            }
        }

        private void ClearScript_Click(object sender, RoutedEventArgs e) {
            _scriptEditorView?.Clear();
            AddConsoleMessage("Script editor cleared.");
        }

        // ==============================================
        // EVENT WINDOW CLOSING (FIX ERROR)
        // ==============================================
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e) {
            try {
                if (_colSidebar != null && _colSidebar.Width.IsAbsolute && _colSidebar.Width.Value > 0) {
                    SettingsManager.Current.SidebarWidth = _colSidebar.Width.Value;
                } else {
                    SettingsManager.Current.SidebarWidth = _lastSidebarWidth;
                }

                if (_colConsole != null && _colConsole.Width.IsAbsolute && _colConsole.Width.Value > 0) {
                    SettingsManager.Current.ConsoleWidth = _colConsole.Width.Value;
                } else if (_consoleContainer != null) {
                    SettingsManager.Current.ConsoleWidth = _lastConsoleWidth;
                }

                // Simpan tinggi bottom panel
                if (_rowBottomPanel != null &&
                    _rowBottomPanel.Height.IsAbsolute &&
                    _rowBottomPanel.Height.Value > 0) {
                    SettingsManager.Current.BottomPanelHeight = _rowBottomPanel.Height.Value;
                } else {
                    SettingsManager.Current.BottomPanelHeight = _lastBottomHeight;
                }

                // Simpan visibility sidebar
                if (_sidebarBorder != null) {
                    SettingsManager.Current.ShowSidebar = _sidebarBorder.Visibility == Visibility.Visible;
                }

                // Simpan visibility console
                if (_consoleContainer != null) {
                    SettingsManager.Current.ShowConsole = _consoleContainer.Visibility == Visibility.Visible;
                }

                // Simpan visibility bottom panel
                if (_bottomPanelBorder != null) {
                    SettingsManager.Current.ShowBottomPanel = _bottomPanelBorder.Visibility == Visibility.Visible;
                }

                SettingsManager.Save();

                // Save active tab session
                _scriptEditorView?.SaveSessionImmediate();

            } catch (Exception ex) {
                Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        // ==============================================
        // HELPER: Activity Switcher & Layout
        // ==============================================
        private void ActivityButton_Click(object sender, RoutedEventArgs e) {
            if (sender is Button btn && btn.Tag != null) {
                string targetActivity = btn.Tag.ToString()!;
                if (targetActivity == "ScriptHub") targetActivity = "Script";

                if (targetActivity == _currentActivity) {
                    ToggleSidebar(!_isSidebarOpen);
                } else {
                    SwitchActivity(targetActivity);
                    ToggleSidebar(true);
                }
            }
        }

        private void SwitchActivity(string activity) {
            if (activity == "ScriptHub") activity = "Script";
            _currentActivity = activity;

            var secBrush = (Brush)Application.Current.Resources["TextSecondary"];
            var goldBrush = (Brush)Application.Current.Resources["HermesGold"];

            if (_actBtnExplorer != null) _actBtnExplorer.Foreground = secBrush;
            if (_actBtnExecutor != null) _actBtnExecutor.Foreground = secBrush;
            if (_actBtnScriptHub != null) _actBtnScriptHub.Foreground = secBrush;

            if (activity == "Explorer") {
                if (_actBtnExplorer != null) _actBtnExplorer.Foreground = goldBrush;
                if (_txtSidebarHeader != null) _txtSidebarHeader.Text = "EXPLORER";

                if (_explorerSidebarContent != null) _explorerSidebarContent.Visibility = Visibility.Visible;
                if (_executorSidebarContent != null) _executorSidebarContent.Visibility = Visibility.Collapsed;

                if (_editorContainer != null) _editorContainer.Visibility = Visibility.Visible;
                if (_scriptHubContainer != null) _scriptHubContainer.Visibility = Visibility.Collapsed;
            } else if (activity == "Executor") {
                if (_actBtnExecutor != null) _actBtnExecutor.Foreground = goldBrush;
                if (_txtSidebarHeader != null) _txtSidebarHeader.Text = "EXECUTOR";

                if (_explorerSidebarContent != null) _explorerSidebarContent.Visibility = Visibility.Collapsed;
                if (_executorSidebarContent != null) _executorSidebarContent.Visibility = Visibility.Visible;

                if (_editorContainer != null) _editorContainer.Visibility = Visibility.Visible;
                if (_scriptHubContainer != null) _scriptHubContainer.Visibility = Visibility.Collapsed;
            } else if (activity == "Script") {
                if (_actBtnScriptHub != null) _actBtnScriptHub.Foreground = goldBrush;
                if (_txtSidebarHeader != null) _txtSidebarHeader.Text = "SCRIPT";

                if (_explorerSidebarContent != null) _explorerSidebarContent.Visibility = Visibility.Visible;
                if (_executorSidebarContent != null) _executorSidebarContent.Visibility = Visibility.Collapsed;

                if (_editorContainer != null) _editorContainer.Visibility = Visibility.Collapsed;
                if (_scriptHubContainer != null) _scriptHubContainer.Visibility = Visibility.Visible;

                _scriptHubViewControl?.TriggerFadeIn();
            }
        }

        private void ToggleSidebar(bool show, bool force = false) {
            if (!force && _isSidebarOpen == show) return;
            _isSidebarOpen = show;

            SettingsManager.Current.ShowSidebar = show;
            SettingsManager.Save();

            if (_sidebarBorder != null) _sidebarBorder.BeginAnimation(WidthProperty, null);
            if (_sidebarContentGrid != null) _sidebarContentGrid.BeginAnimation(OpacityProperty, null);

            if (force) {
                if (show) {
                    if (_colSidebar != null) {
                        _colSidebar.MinWidth = 260;
                        _colSidebar.MaxWidth = 420;
                        _colSidebar.Width = new GridLength(_lastSidebarWidth);
                    }
                    if (_sidebarBorder != null) {
                        _sidebarBorder.Visibility = Visibility.Visible;
                        _sidebarBorder.Width = double.NaN;
                    }
                    if (_sidebarSplitter != null) _sidebarSplitter.Visibility = Visibility.Visible;
                    if (_sidebarContentGrid != null) _sidebarContentGrid.Opacity = 1.0;
                } else {
                    if (_colSidebar != null) {
                        _colSidebar.MinWidth = 0;
                        _colSidebar.Width = new GridLength(0);
                    }
                    if (_sidebarBorder != null) _sidebarBorder.Visibility = Visibility.Collapsed;
                    if (_sidebarSplitter != null) _sidebarSplitter.Visibility = Visibility.Collapsed;
                    if (_sidebarContentGrid != null) _sidebarContentGrid.Opacity = 0.0;
                }
                return;
            }

            if (show) {
                if (_colSidebar != null) {
                    _colSidebar.MinWidth = 260;
                    _colSidebar.MaxWidth = 420;
                    _colSidebar.Width = GridLength.Auto;
                }
                if (_sidebarBorder != null) _sidebarBorder.Visibility = Visibility.Visible;
                if (_sidebarSplitter != null) _sidebarSplitter.Visibility = Visibility.Visible;
                if (_sidebarContentGrid != null) _sidebarContentGrid.Opacity = 0.0;

                var animWidth = new System.Windows.Media.Animation.DoubleAnimation {
                    From = 0,
                    To = _lastSidebarWidth,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };

                animWidth.Completed += (s, e) => {
                    if (_sidebarBorder != null) {
                        _sidebarBorder.BeginAnimation(WidthProperty, null);
                        _sidebarBorder.Width = double.NaN;
                    }
                    if (_colSidebar != null) _colSidebar.Width = new GridLength(_lastSidebarWidth);
                    UpdateLayout();
                };

                var animOpacity = new System.Windows.Media.Animation.DoubleAnimation {
                    From = 0.0,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(150),
                    BeginTime = TimeSpan.FromMilliseconds(50),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };

                animOpacity.Completed += (s, e) => {
                    if (_sidebarContentGrid != null) {
                        _sidebarContentGrid.BeginAnimation(OpacityProperty, null);
                        _sidebarContentGrid.Opacity = 1.0;
                    }
                    UpdateLayout();
                };

                if (_sidebarBorder != null) _sidebarBorder.BeginAnimation(WidthProperty, animWidth);
                if (_sidebarContentGrid != null) _sidebarContentGrid.BeginAnimation(OpacityProperty, animOpacity);

                if (_currentActivity == "Script") {
                    _scriptHubViewControl?.TriggerFadeIn();
                }
            } else {
                if (_colSidebar != null && _colSidebar.Width.IsAbsolute) {
                    _lastSidebarWidth = _colSidebar.Width.Value;
                } else if (_sidebarBorder != null && _sidebarBorder.ActualWidth > 0) {
                    _lastSidebarWidth = _sidebarBorder.ActualWidth;
                }

                if (_colSidebar != null) {
                    _colSidebar.MinWidth = 0;
                    _colSidebar.Width = GridLength.Auto;
                }
                if (_sidebarBorder != null) _sidebarBorder.Width = _lastSidebarWidth;

                var animWidth = new System.Windows.Media.Animation.DoubleAnimation {
                    From = _lastSidebarWidth,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };

                animWidth.Completed += (s, e) => {
                    if (_sidebarBorder != null) {
                        _sidebarBorder.BeginAnimation(WidthProperty, null);
                        _sidebarBorder.Visibility = Visibility.Collapsed;
                    }
                    if (_sidebarSplitter != null) _sidebarSplitter.Visibility = Visibility.Collapsed;
                };

                var animOpacity = new System.Windows.Media.Animation.DoubleAnimation {
                    From = _sidebarContentGrid?.Opacity ?? 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(120),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };

                animOpacity.Completed += (s, e) => {
                    if (_sidebarContentGrid != null) {
                        _sidebarContentGrid.BeginAnimation(OpacityProperty, null);
                        _sidebarContentGrid.Opacity = 0.0;
                    }
                };

                if (_sidebarBorder != null) _sidebarBorder.BeginAnimation(WidthProperty, animWidth);
                if (_sidebarContentGrid != null) _sidebarContentGrid.BeginAnimation(OpacityProperty, animOpacity);
            }
        }

        private void ToggleConsole(bool show) {
            if (_colConsole != null) {
                _colConsole.MinWidth = 200;
                _colConsole.MaxWidth = 500;
                if (_colConsole.Width.Value <= 0) {
                    _colConsole.Width = new GridLength(_lastConsoleWidth > 0 ? _lastConsoleWidth : 300);
                }
            }
            if (_consoleContainer != null) _consoleContainer.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (_consoleSplitter != null) _consoleSplitter.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ToggleBottomPanel(bool show) {
            if (show) {
                if (_bottomPanelBorder != null) _bottomPanelBorder.Visibility = Visibility.Visible;
                if (_bottomSplitter != null) _bottomSplitter.Visibility = Visibility.Visible;
                if (_rowBottomPanel != null) _rowBottomPanel.Height = new GridLength(_lastBottomHeight);
            } else {
                if (_rowBottomPanel != null && _rowBottomPanel.Height.Value > 0) {
                    _lastBottomHeight = _rowBottomPanel.Height.Value;
                }
                if (_bottomPanelBorder != null) _bottomPanelBorder.Visibility = Visibility.Collapsed;
                if (_bottomSplitter != null) _bottomSplitter.Visibility = Visibility.Collapsed;
                if (_rowBottomPanel != null) _rowBottomPanel.Height = new GridLength(0);
            }
        }

        private void CollapseSidebar_Click(object sender, RoutedEventArgs e) => ToggleSidebar(false);
        private void CollapseConsole_Click(object sender, RoutedEventArgs e) => ToggleConsole(false);
        private void CollapseBottomPanel_Click(object sender, RoutedEventArgs e) => ToggleBottomPanel(false);

        private void ScriptHubButton_Click(object sender, RoutedEventArgs e) => SwitchActivity("ScriptHub");
        private void EditorButton_Click(object sender, RoutedEventArgs e) => SwitchActivity("Explorer");

        private void ScriptHub_OpenInNewTab(ScriptItem script) {
            if (string.IsNullOrWhiteSpace(script.Script)) {
                MessageBox.Show("Source script tidak tersedia pada hasil ini.", "Hermes Script Hub", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _scriptEditorView?.OpenNewTab(script.Title, script.Script);
            SwitchActivity("Explorer");
            AddConsoleMessage($"Loaded script from Script Hub: {script.Title}");
        }

        private async void ScriptHub_ExecuteRequested(ScriptItem script) {
            if (string.IsNullOrWhiteSpace(script.Script)) {
                MessageBox.Show("Source script tidak tersedia pada hasil ini.", "Hermes Script Hub", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try {
                await ScriptEngine.ExecuteAsync(script.Script);
                AddConsoleMessage($"Executed script from Script Hub: {script.Title}");
            } catch (Exception ex) {
                AddConsoleMessage($"Execute failed: {ex.Message}");
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (e.ButtonState == MouseButtonState.Pressed) {
                DragMove();
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (e.ClickCount == 2) {
                Maximize_Click(sender, e);
            } else {
                DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        public void AddConsoleMessage(string message) {
            string line = $"[{DateTime.Now:HH:mm:ss}] {message}\n";
            string timestamp = DateTime.Now.ToString("HH:mm:ss");

            Dispatcher.Invoke(() => {
                _consoleViewPanel?.AppendText(line);
                if (_txtOutputLog != null) {
                    _txtOutputLog.AppendText($"[{timestamp}] {message}\n");
                    _txtOutputLog.ScrollToEnd();
                }
            });
        }

        private void ProcessConsoleCommand(string command) {
            switch (command) {
                case "help":
                    AddConsoleMessage("Available commands: help, status, clear");
                    break;
                case "status":
                    AddConsoleMessage($"Hermes-Executor running normally with Roblox Online: {_injector.CheckRobloxRunning()}.");
                    break;
                case "clear":
                    _consoleViewPanel?.Clear();
                    break;
                default:
                    AddConsoleMessage($"Unknown command: {command}. Type 'help' for options.");
                    break;
            }
        }

        private void RobloxCheckTimer_Tick(object? sender, EventArgs e) {
            bool isRunning = _injector.CheckRobloxRunning();
            if (_robloxStatusDot != null) {
                _robloxStatusDot.Fill = isRunning ? System.Windows.Media.Brushes.LimeGreen : System.Windows.Media.Brushes.Red;
            }
            if (_txtRobloxStatus != null) {
                _txtRobloxStatus.Text = isRunning ? "Online" : "Offline";
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (_scriptEditorView == null) return;

            if (Keyboard.Modifiers == ModifierKeys.Control) {
                switch (e.Key) {
                    case Key.N:
                        e.Handled = true;
                        _scriptEditorView.AddNewTab();
                        break;
                    case Key.W:
                        e.Handled = true;
                        if (_scriptEditorView.ActiveTab != null) {
                            _scriptEditorView.PromptAndCloseTab(_scriptEditorView.ActiveTab);
                        }
                        break;
                    case Key.S:
                        e.Handled = true;
                        if (_scriptEditorView.ActiveTab != null) {
                            _scriptEditorView.SaveTab(_scriptEditorView.ActiveTab, false);
                        }
                        break;
                    case Key.O:
                        e.Handled = true;
                        BtnLoadScript_Click(this, new RoutedEventArgs());
                        break;
                    case Key.Tab:
                        e.Handled = true;
                        _scriptEditorView.SelectNextTab();
                        break;
                }
            } else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)) {
                if (e.Key == Key.S) {
                    e.Handled = true;
                    if (_scriptEditorView.ActiveTab != null) {
                        _scriptEditorView.SaveTab(_scriptEditorView.ActiveTab, true);
                    }
                } else if (e.Key == Key.Tab) {
                    e.Handled = true;
                    _scriptEditorView.SelectPreviousTab();
                }
            }
        }

        // ==============================================
        // HELPER Properties untuk backward compatibility
        // ==============================================
        public ConsoleView? ConsoleView => _consoleViewPanel;
        public Button? BtnInject => _btnInject;
        public Button? BtnExecute => _btnExecute;
    }
}
