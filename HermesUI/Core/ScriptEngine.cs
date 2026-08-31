using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading.Tasks;

namespace Hermes_Executor.Core {
    public static class ScriptEngine {
        public static event Action<string>? OnLog;

        [DllImport("HermesCore.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool InitializeInjector();

        [DllImport("HermesCore.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool InjectRoblox();

        [DllImport("HermesCore.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool IsInjected();

        [DllImport("HermesCore.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetCoreLastError();

        [DllImport("HermesCore.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool ExecuteScript(string script);

        [DllImport("HermesCore.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool ExecuteScriptFromFile(string filePath);

        [DllImport("HermesCore.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool IsScriptRunning();

        public static string LastError {
            get {
                IntPtr ptr = GetCoreLastError();
                return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) ?? string.Empty : string.Empty;
            }
        }

        public static bool Inject() {
            OnLog?.Invoke("Initializing injector...");
            if (!InitializeInjector()) {
                OnLog?.Invoke("Failed to initialize injector.");
                return false;
            }

            OnLog?.Invoke("Injecting into Roblox...");
            bool result = InjectRoblox();

            // Debug output
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Inject result: {result}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] IsInjected: {IsInjected()}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] LastError: {LastError}");

            if (!result) {
                OnLog?.Invoke($"Injection failed: {LastError}");
            } else {
                OnLog?.Invoke("Injected successfully!");
            }
            return result;
        }

        public static bool Execute(string script) {
            if (!IsInjected()) {
                throw new Exception("Not injected into Roblox");
            }
            return ExecuteScript(script);
        }

        public static bool ExecuteFromFile(string filePath) {
            if (!File.Exists(filePath)) {
                throw new FileNotFoundException($"Script file not found: {filePath}");
            }
            return ExecuteScriptFromFile(filePath);
        }

        public static bool IsConnected => _isInjectedNative();
        public static bool IsExecuting => IsScriptRunning();

        // Alias publik yang aman untuk dipanggil dari mana saja
        public static bool IsInjectedStatus() => _isInjectedNative();

        // Helper private untuk menghindari naming conflict dengan P/Invoke
        private static bool _isInjectedNative() {
            try { return IsInjected(); }
            catch { return false; }
        }

        // Status lengkap untuk debugging
        public static string GetStatus() {
            return $"Injected: {IsInjectedStatus()}\n" +
                   $"Connected: {IsConnected}\n" +
                   $"LastError: {LastError}\n" +
                   $"IsExecuting: {IsScriptRunning()}\n";
        }


        public static async Task<bool> ExecuteAsync(string scriptContent) {
            if (string.IsNullOrWhiteSpace(scriptContent)) {
                OnLog?.Invoke("Error: Script is empty.");
                return false;
            }

            OnLog?.Invoke("Executing script via HermesCore...");
            return await Task.Run(() => {
                if (!IsInjected()) {
                    OnLog?.Invoke("Not injected into Roblox. Attempting auto-inject...");
                    if (!Inject()) {
                        return false;
                    }
                }

                bool result = ExecuteScript(scriptContent);
                if (result) {
                    OnLog?.Invoke("Script executed successfully.");
                } else {
                    OnLog?.Invoke($"Execution failed: {LastError}");
                }
                return result;
            });
        }
    }
}
