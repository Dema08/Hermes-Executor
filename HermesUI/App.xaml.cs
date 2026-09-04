using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace Hermes_Executor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly string LogPath = @"C:\hermes_crash.log";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += (s, args) =>
            {
                Log("DispatcherUnhandledException", args.Exception);
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                Log("AppDomain.UnhandledException", args.ExceptionObject as Exception);
            };

            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                Log("UnobservedTaskException", args.Exception);
                args.SetObserved();
            };
        }

        private static void Log(string source, Exception? ex)
        {
            try
            {
                string msg = $"[{DateTime.Now:HH:mm:ss}] {source}: {ex}\n";
                File.AppendAllText(LogPath, msg);
            }
            catch { /* ignore */ }
        }
    }
}
