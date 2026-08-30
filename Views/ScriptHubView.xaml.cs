using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Hermes_Executor.Core;
using Hermes_Executor.Core.Providers;
using Hermes_Executor.Models;

namespace Hermes_Executor.Views
{
    public partial class ScriptHubView : UserControl, System.ComponentModel.INotifyPropertyChanged
    {
        private readonly ScriptSearchService? _searchService;
        private readonly ObservableCollection<ScriptItem> _results = new();
        private readonly List<ScriptItem> _originalResults = new();
        private ScriptItem? _selectedScript;

        private int _columnsCount = 3;
        public int ColumnsCount
        {
            get => _columnsCount;
            set
            {
                if (_columnsCount != value)
                {
                    _columnsCount = value;
                    OnPropertyChanged(nameof(ColumnsCount));
                }
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double availableWidth = e.NewSize.Width - 40;
            if (availableWidth < 280)
            {
                ColumnsCount = 1;
            }
            else
            {
                int cols = (int)Math.Max(1, Math.Floor(availableWidth / 280));
                ColumnsCount = cols;
            }
        }

        private static readonly HttpClient _httpClient = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            return client;
        }

        public event Action<ScriptItem>? ExecuteRequested;
        public event Action<ScriptItem>? OpenInNewTabRequested;

        public ScriptHubView()
        {
            InitializeComponent();

            ResultsList.ItemsSource = _results;
            EmptyState.Visibility = Visibility.Visible;

            string? apiKey = ApiKeyManager.LoadApiKey();

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                IScriptProvider[] providers = { new RScriptProvider(apiKey) };
                _searchService = new ScriptSearchService(providers);
            }
            else
            {
                IScriptProvider[] providers = { new MockScriptProvider() };
                _searchService = new ScriptSearchService(providers);
            }

            Loaded += (s, e) => TriggerFadeIn();
        }

        public void TriggerFadeIn()
        {
            if (FindResource("FadeInStoryboard") is System.Windows.Media.Animation.Storyboard sb)
            {
                sb.Begin(this);
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (_searchService == null) return;
            string query = SearchBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(query))
                return;

            try
            {
                SearchProgressBar.Visibility = Visibility.Visible;
                SearchBox.IsEnabled = false;
                SearchButton.IsEnabled = false;
                
                _originalResults.Clear();
                _results.Clear();
                EmptyState.Visibility = Visibility.Collapsed;

                var results = await _searchService.SearchAsync(query);
                _originalResults.AddRange(results);

                ApplyFiltersAndSorting();

                if (results.Count == 0)
                {
                    EmptyStateText.Text = $"Tidak ada script untuk \"{query}\"";
                    EmptyState.Visibility = Visibility.Visible;
                }
                else
                {
                    _ = LoadThumbnailsAsync(results);
                }
            }
            catch (Exception ex)
            {
                HermesMessageBox.Show(
                    "Error",
                    "Pencarian Script Hub gagal:\n\n" + ex.Message,
                    HermesMessageBox.NotificationType.Error);
            }
            finally
            {
                SearchProgressBar.Visibility = Visibility.Collapsed;
                SearchBox.IsEnabled = true;
                SearchButton.IsEnabled = true;
                SearchBox.Focus();
            }
        }

        private void ApplyFiltersAndSorting()
        {
            if (_originalResults == null) return;

            var filtered = _originalResults.AsEnumerable();

            // Sort by views or update time
            string sortBy = "Popular";
            if (ComboSort != null && ComboSort.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                sortBy = selectedItem.Tag.ToString()!;
            }

            filtered = sortBy switch
            {
                "Recent" => filtered.OrderByDescending(x => x.UpdatedAt ?? DateTime.MinValue),
                "Popular" => filtered.OrderByDescending(x => x.Views),
                _ => filtered.OrderByDescending(x => x.Views)
            };

            _results.Clear();
            foreach (var script in filtered)
            {
                _results.Add(script);
            }

            if (EmptyState != null)
            {
                EmptyState.Visibility = _results.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                if (_results.Count == 0)
                {
                    EmptyStateText.Text = "Tidak ada script yang cocok dengan filter pencarian.";
                }
            }
        }

        private void ComboSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFiltersAndSorting();
        }

        private async Task LoadThumbnailsAsync(List<ScriptItem> scripts)
        {
            var tasks = scripts.Select(async script =>
            {
                if (string.IsNullOrWhiteSpace(script.ThumbnailUrl))
                    return;

                if (!Uri.TryCreate(script.ThumbnailUrl, UriKind.Absolute, out Uri? uri))
                    return;

                try
                {
                    byte[] data = await _httpClient.GetByteArrayAsync(uri);

                    await Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = new System.IO.MemoryStream(data);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            bitmap.Freeze();

                            script.ThumbnailImage = bitmap;
                        }
                        catch { }
                    });
                }
                catch { }
            });

            await Task.WhenAll(tasks);
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchButton_Click(sender, new RoutedEventArgs(Button.ClickEvent));
            }
        }

        private async void ScriptCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not ScriptItem script)
                return;

            _selectedScript = script;

            ActionTitle.Text = string.IsNullOrWhiteSpace(script.Title) ? "Untitled Script" : script.Title;
            ActionGame.Text = string.IsNullOrWhiteSpace(script.Game) ? "Game: -" : $"🎮  {script.Game}";
            ActionProvider.Text = string.IsNullOrWhiteSpace(script.Provider)
                ? "Provider: -"
                : $"Provider: {script.Provider}  •  {script.Views:N0} views";

            if (script.ThumbnailImage != null)
            {
                ActionThumbnail.Source = script.ThumbnailImage;
            }
            else
            {
                ActionThumbnail.Source = null;

                if (!string.IsNullOrWhiteSpace(script.ThumbnailUrl) &&
                    Uri.TryCreate(script.ThumbnailUrl, UriKind.Absolute, out Uri? thumbnailUri))
                {
                    try
                    {
                        byte[] data = await _httpClient.GetByteArrayAsync(thumbnailUri);
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = new System.IO.MemoryStream(data);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();

                        script.ThumbnailImage = bitmap;
                        ActionThumbnail.Source = bitmap;
                    }
                    catch
                    {
                        ActionThumbnail.Source = null;
                    }
                }
            }

            ActionOverlay.Visibility = Visibility.Visible;
        }

        private void CloseAction_Click(object sender, RoutedEventArgs e)
        {
            ActionOverlay.Visibility = Visibility.Collapsed;
            _selectedScript = null;
        }

        private void CopySelected_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedScript == null) return;

            if (string.IsNullOrWhiteSpace(_selectedScript.Script))
            {
                HermesMessageBox.Show(
                    "Warning",
                    "Source script tidak tersedia.",
                    HermesMessageBox.NotificationType.Warning);
                return;
            }

            Clipboard.SetText(_selectedScript.Script);

            HermesMessageBox.Show(
                "Script Copied",
                "Script berhasil disalin ke clipboard.",
                HermesMessageBox.NotificationType.Success);
        }

        private void OpenTabSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedScript == null) return;

            OpenInNewTabRequested?.Invoke(_selectedScript);
            ActionOverlay.Visibility = Visibility.Collapsed;
        }

        private void ExecuteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedScript == null) return;

            ExecuteRequested?.Invoke(_selectedScript);
            ActionOverlay.Visibility = Visibility.Collapsed;
        }
    }
}