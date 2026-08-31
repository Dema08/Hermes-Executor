using System;
using System.Windows.Controls;

namespace Hermes_Executor.Views
{
    public partial class ConsoleView : UserControl
    {
        public event Action<string>? OnCommandSubmitted;

        public ConsoleView()
        {
            InitializeComponent();
            BtnClearConsole.Click += (s, e) => Clear();
            TxtConsoleInput.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    string cmd = TxtConsoleInput.Text.Trim();
                    TxtConsoleInput.Clear();
                    if (!string.IsNullOrEmpty(cmd))
                    {
                        AppendMessage($"> {cmd}");
                        OnCommandSubmitted?.Invoke(cmd.ToLower());
                    }
                }
            };
        }

        public void AppendText(string text)
        {
            Dispatcher.Invoke(() =>
            {
                TxtConsoleOutput.AppendText(text);
                TxtConsoleOutput.ScrollToEnd();
            });
        }

        public void AppendMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                TxtConsoleOutput.AppendText($"[{timestamp}] {message}\n");
                TxtConsoleOutput.ScrollToEnd();
            });
        }

        public void Clear() => TxtConsoleOutput.Clear();
    }
}
