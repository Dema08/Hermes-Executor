using System;
using System.Collections.ObjectModel;
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

        public event Action<ScriptItem>? ExecuteRequested;
        public event Action<ScriptItem>? OpenInNewTabRequested;

        public ScriptHubView()
        {
            InitializeComponent();

            ResultsList.ItemsSource = _results;

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
                _results.Clear();

                var results =
                    await _searchService.SearchAsync(query);

                foreach (ScriptItem script in results)
                    _results.Add(script);

                if (results.Count == 0)
                {
                    HermesMessageBox.Show(
                        "Script Hub",
                        $"Tidak ada script untuk \"{query}\".",
                        HermesMessageBox.NotificationType.Info);
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
                SearchBox.Focus();
            }
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

        private void ScriptCard_Click(
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
                    : $"Game: {script.Game}";

            ActionProvider.Text =
                string.IsNullOrWhiteSpace(script.Provider)
                    ? "Provider: -"
                    : $"Provider: {script.Provider}";

            ActionThumbnail.Source = null;

            if (!string.IsNullOrWhiteSpace(script.ThumbnailUrl) &&
                Uri.TryCreate(
                    script.ThumbnailUrl,
                    UriKind.Absolute,
                    out Uri? thumbnailUri))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = thumbnailUri;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    ActionThumbnail.Source = bitmap;
                }
                catch
                {
                    ActionThumbnail.Source = null;
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