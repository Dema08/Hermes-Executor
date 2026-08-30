using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Hermes_Executor.Core;

namespace Hermes_Executor.Views
{
    public class ScriptTabItem : System.ComponentModel.INotifyPropertyChanged
    {
        private string _header = "";
        private string? _filePath;
        private bool _isModified;
        private ICSharpCode.AvalonEdit.TextEditor _editor;

        public string Header
        {
            get => _header;
            set { _header = value; OnPropertyChanged(nameof(Header)); }
        }

        public string? FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                OnPropertyChanged(nameof(FilePath));
                if (!string.IsNullOrEmpty(_filePath))
                {
                    Header = System.IO.Path.GetFileName(_filePath);
                }
            }
        }

        public bool IsModified
        {
            get => _isModified;
            set { _isModified = value; OnPropertyChanged(nameof(IsModified)); }
        }

        public ICSharpCode.AvalonEdit.TextEditor Editor
        {
            get => _editor;
            set { _editor = value; OnPropertyChanged(nameof(Editor)); }
        }

        public ScriptTabItem(string header, ICSharpCode.AvalonEdit.TextEditor editor, string? filePath = null)
        {
            _header = header;
            _editor = editor;
            _filePath = filePath;
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    public class SessionTabItem
    {
        public string Header { get; set; } = "";
        public string? FilePath { get; set; }
        public string Content { get; set; } = "";
        public bool IsModified { get; set; }
    }

    public class SessionData
    {
        public int ActiveTabIndex { get; set; }
        public List<SessionTabItem> Tabs { get; set; } = new();
    }

    public partial class ScriptEditor : UserControl
    {
        public ObservableCollection<ScriptTabItem> TabsList { get; } = new();

        public static readonly DependencyProperty ActiveTabProperty =
            DependencyProperty.Register(
                nameof(ActiveTab),
                typeof(ScriptTabItem),
                typeof(ScriptEditor),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnActiveTabChanged));

        public ScriptTabItem? ActiveTab
        {
            get => (ScriptTabItem?)GetValue(ActiveTabProperty);
            set => SetValue(ActiveTabProperty, value);
        }

        private static void OnActiveTabChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScriptEditor editor && e.NewValue is ScriptTabItem tabItem)
            {
                if (editor.Tabs.SelectedItem != tabItem)
                {
                    editor.Tabs.SelectedItem = tabItem;
                }
            }
        }
        
        public event Action<string>? ScriptSaved;
        private readonly System.Windows.Threading.DispatcherTimer _sessionSaveTimer;

        public ScriptEditor()
        {
            InitializeComponent();

            _sessionSaveTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _sessionSaveTimer.Tick += (s, e) =>
            {
                _sessionSaveTimer.Stop();
                SaveSessionImmediate();
            };

            TabsList.CollectionChanged += TabsList_CollectionChanged;
            Tabs.ItemsSource = TabsList;

            bool restored = RestoreSession();
            if (!restored)
            {
                AddNewTab("-- Hermes Script Editor\nprint(\"Ready!\")");
            }
        }

        private ICSharpCode.AvalonEdit.TextEditor CreateNewEditor(string content)
        {
            var editor = new ICSharpCode.AvalonEdit.TextEditor
            {
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#D4D4D4")!,
                LineNumbersForeground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#6E7681")!,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 13,
                ShowLineNumbers = true,
                WordWrap = false
            };
            
            editor.Text = content;
            
            // Install Search Panel
            try
            {
                ICSharpCode.AvalonEdit.Search.SearchPanel.Install(editor);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SearchPanel Error: {ex.Message}");
            }
            
            // Load highlighting
            try
            {
                editor.TextArea.TextView.LineTransformers.Add(new Hermes_Executor.Core.LuaColorizer());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lua Highlighting Error: {ex.Message}");
            }

            editor.TextChanged += (s, e) =>
            {
                QueueSaveSession();
            };
            
            return editor;
        }

        private string GetNextUntitledName()
        {
            int i = 1;
            while (true)
            {
                string candidate = $"Untitled {i}";
                bool exists = false;
                foreach (var tab in TabsList)
                {
                    if (tab.Header == candidate)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    return candidate;
                }
                i++;
            }
        }

        public void AddNewTab(string content = "", string? filePath = null)
        {
            string header = string.IsNullOrEmpty(filePath) ? GetNextUntitledName() : System.IO.Path.GetFileName(filePath);
            
            var editor = CreateNewEditor(content);
            var newTab = new ScriptTabItem(header, editor, filePath);
            
            // Mark modified on text changes
            editor.TextChanged += (s, e) =>
            {
                newTab.IsModified = true;
            };
            newTab.IsModified = false;
            
            // Wire up position change logic
            editor.TextArea.Caret.PositionChanged += (s, e) =>
            {
                if (Tabs.SelectedItem == newTab)
                {
                    UpdateStatusText(editor);
                }
            };
            
            TabsList.Add(newTab);
            Tabs.SelectedItem = newTab;
            UpdateStatusText(editor);
        }

        public void OpenNewTab(string header, string content, string? filePath = null)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                foreach (var tab in TabsList)
                {
                    if (tab.FilePath == filePath)
                    {
                        Tabs.SelectedItem = tab;
                        return;
                    }
                }
            }

            var editor = CreateNewEditor(content);
            var newTab = new ScriptTabItem(header, editor, filePath);
            
            editor.TextChanged += (s, e) =>
            {
                newTab.IsModified = true;
            };
            newTab.IsModified = false;
            
            editor.TextArea.Caret.PositionChanged += (s, e) =>
            {
                if (Tabs.SelectedItem == newTab)
                {
                    UpdateStatusText(editor);
                }
            };
            
            TabsList.Add(newTab);
            Tabs.SelectedItem = newTab;
            UpdateStatusText(editor);
        }

        private void NewTab_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab();
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ScriptTabItem tabItem)
            {
                PromptAndCloseTab(tabItem);
            }
        }

        public bool PromptAndCloseTab(ScriptTabItem tab)
        {
            if (tab.IsModified)
            {
                var result = HermesMessageBox.ShowUnsavedChanges(
                    "Konfirmasi Close File",
                    $"File '{tab.Header}' memiliki perubahan yang belum disimpan. Apakah Anda ingin menyimpan perubahan sebelum menutup?"
                );

                if (result == HermesMessageBox.MessageBoxResult3Way.Save)
                {
                    bool saved = SaveTab(tab, false);
                    if (!saved) return false;
                }
                else if (result == HermesMessageBox.MessageBoxResult3Way.Cancel)
                {
                    return false;
                }
            }

            TabsList.Remove(tab);
            if (TabsList.Count == 0)
            {
                AddNewTab();
            }
            return true;
        }

        public bool SaveTab(ScriptTabItem tab, bool forceSaveAs)
        {
            if (string.IsNullOrEmpty(tab.FilePath) || forceSaveAs)
            {
                SaveFileDialog dlg = new SaveFileDialog
                {
                    Filter = "Lua Files (*.lua)|*.lua|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    FileName = tab.Header.EndsWith(".lua") || tab.Header.EndsWith(".txt") ? tab.Header : tab.Header + ".lua"
                };
                if (dlg.ShowDialog() == true)
                {
                    try
                    {
                        File.WriteAllText(dlg.FileName, tab.Editor.Text);
                        tab.FilePath = dlg.FileName;
                        tab.Header = Path.GetFileName(dlg.FileName);
                        tab.IsModified = false;
                        ScriptSaved?.Invoke(dlg.FileName);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Gagal menyimpan file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
                return false;
            }
            else
            {
                try
                {
                    File.WriteAllText(tab.FilePath, tab.Editor.Text);
                    tab.IsModified = false;
                    ScriptSaved?.Invoke(tab.FilePath);
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Gagal menyimpan file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
        }

        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ActiveTab != Tabs.SelectedItem)
            {
                ActiveTab = Tabs.SelectedItem as ScriptTabItem;
            }
            // Update status text with caret of currently selected editor
            if (ActiveTab != null)
            {
                UpdateStatusText(ActiveTab.Editor);
            }
            QueueSaveSession();
        }

        private void UpdateStatusText(ICSharpCode.AvalonEdit.TextEditor editor)
        {
            TxtEditorStatus.Text = $"Ln: {editor.TextArea.Caret.Line} | Col: {editor.TextArea.Caret.Column}";
        }

        public string GetScriptText() => ActiveTab?.Editor.Text ?? string.Empty;
        
        public void SetScriptText(string text)
        {
            if (ActiveTab != null)
            {
                ActiveTab.Editor.Text = text;
            }
            else
            {
                AddNewTab(text);
            }
        }

        public void Clear() => ActiveTab?.Editor.Clear();

        // --- SESSION PERSISTENCE ---

        private void QueueSaveSession()
        {
            _sessionSaveTimer.Stop();
            _sessionSaveTimer.Start();
        }

        private void TabsList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (ScriptTabItem item in e.OldItems)
                {
                    item.PropertyChanged -= TabItem_PropertyChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (ScriptTabItem item in e.NewItems)
                {
                    item.PropertyChanged += TabItem_PropertyChanged;
                }
            }
            QueueSaveSession();
        }

        private void TabItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScriptTabItem.Header) || 
                e.PropertyName == nameof(ScriptTabItem.FilePath) || 
                e.PropertyName == nameof(ScriptTabItem.IsModified))
            {
                QueueSaveSession();
            }
        }

        private string GetSessionFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dir = Path.Combine(appData, "Hermes-Executor");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return Path.Combine(dir, "session.json");
        }

        public void SaveSessionImmediate()
        {
            try
            {
                var data = new SessionData
                {
                    ActiveTabIndex = Tabs.SelectedIndex,
                    Tabs = new List<SessionTabItem>()
                };

                foreach (var tab in TabsList)
                {
                    data.Tabs.Add(new SessionTabItem
                    {
                        Header = tab.Header,
                        FilePath = tab.FilePath,
                        Content = tab.Editor.Text,
                        IsModified = tab.IsModified
                    });
                }

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(GetSessionFilePath(), json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save session: {ex.Message}");
            }
        }

        public bool RestoreSession()
        {
            try
            {
                string path = GetSessionFilePath();
                if (!File.Exists(path)) return false;

                string json = File.ReadAllText(path);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<SessionData>(json);
                if (data == null || data.Tabs == null || data.Tabs.Count == 0) return false;

                // Temporarily disable collection change saves during loading
                TabsList.CollectionChanged -= TabsList_CollectionChanged;

                TabsList.Clear();
                foreach (var tabData in data.Tabs)
                {
                    var editor = CreateNewEditor(tabData.Content);
                    var tabItem = new ScriptTabItem(tabData.Header, editor, tabData.FilePath);
                    tabItem.IsModified = tabData.IsModified;

                    editor.TextChanged += (s, e) =>
                    {
                        tabItem.IsModified = true;
                    };

                    editor.TextArea.Caret.PositionChanged += (s, e) =>
                    {
                        if (Tabs.SelectedItem == tabItem)
                        {
                            UpdateStatusText(editor);
                        }
                    };

                    tabItem.PropertyChanged += TabItem_PropertyChanged;

                    TabsList.Add(tabItem);
                }

                TabsList.CollectionChanged += TabsList_CollectionChanged;

                if (data.ActiveTabIndex >= 0 && data.ActiveTabIndex < TabsList.Count)
                {
                    Tabs.SelectedIndex = data.ActiveTabIndex;
                }
                else
                {
                    Tabs.SelectedIndex = 0;
                }

                if (ActiveTab != null)
                {
                    UpdateStatusText(ActiveTab.Editor);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to restore session: {ex.Message}");
                return false;
            }
        }

        public void ClearSession()
        {
            try
            {
                string path = GetSessionFilePath();
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clear session: {ex.Message}");
            }
        }
    }
}
