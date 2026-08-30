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
    public partial class ScriptHubView : UserControl
    {
        private readonly ScriptSearchService _searchService;
        private readonly ObservableCollection<ScriptItem> _results = new();
        private ScriptItem? _selectedScript;

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
                IScriptProvider[] providers =
                {
                    new RScriptProvider(apiKey)
                };

                _searchService = new ScriptSearchService(providers);
            }
            else
            {
                IScriptProvider[] providers =
                {
                    new MockScriptProvider()
                };

                _searchService = new ScriptSearchService(providers);
            }
        }

        private async void SearchButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string query = SearchBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(query))
                return;

            try
            {
                SearchBox.IsEnabled = false;
                SearchButton.IsEnabled = false;
                _results.Clear();
                EmptyState.Visibility = Visibility.Collapsed;

                var results = await _searchService.SearchAsync(query);

                foreach (ScriptItem script in results)
                    _results.Add(script);

                if (results.Count == 0)
                {
                    EmptyStateText.Text = $"Tidak ada script untuk \"{query}\"";
                    EmptyState.Visibility = Visibility.Visible;
                }
                else
                {
                    // Load thumbnails in parallel in the background
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
                SearchBox.IsEnabled = true;
                SearchButton.IsEnabled = true;
                SearchBox.Focus();
            }
        }

        /// <summary>
        /// Loads thumbnails for all scripts in parallel and updates
        /// each ScriptItem.ThumbnailImage so the UI updates automatically via binding.
        /// </summary>
        private async Task LoadThumbnailsAsync(System.Collections.Generic.List<ScriptItem> scripts)
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

        private void SearchBox_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchButton_Click(
                    sender,
                    new RoutedEventArgs(Button.ClickEvent));
            }
        }

        private async void ScriptCard_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not ScriptItem script)
            {
                return;
            }

            _selectedScript = script;

            ActionTitle.Text =
                string.IsNullOrWhiteSpace(script.Title)
                    ? "Untitled Script"
                    : script.Title;

            ActionGame.Text =
                string.IsNullOrWhiteSpace(script.Game)
                    ? "Game: -"
                    : $"🎮  {script.Game}";

            ActionProvider.Text =
                string.IsNullOrWhiteSpace(script.Provider)
                    ? "Provider: -"
                    : $"Provider: {script.Provider}  •  {script.Views:N0} views";

            // Reuse already-loaded thumbnail from card; try loading if not yet available
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

        private void CloseAction_Click(
            object sender,
            RoutedEventArgs e)
        {
            ActionOverlay.Visibility = Visibility.Collapsed;
            _selectedScript = null;
        }

        private void CopySelected_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_selectedScript == null)
                return;

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

        private void OpenTabSelected_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_selectedScript == null)
                return;

            OpenInNewTabRequested?.Invoke(_selectedScript);
            ActionOverlay.Visibility = Visibility.Collapsed;
        }

        private void ExecuteSelected_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_selectedScript == null)
                return;

            ExecuteRequested?.Invoke(_selectedScript);
            ActionOverlay.Visibility = Visibility.Collapsed;
        }
    }
}