using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Hermes_Executor.Core
{
    public class AutoInjector : IDisposable
    {
        // Events
        public event Action<RobloxStatus>? OnStatusChanged;
        public event Action<string>? OnLog;
        public event Action<bool>? OnInjectionResult;

        // Status
        public enum RobloxStatus
        {
            Offline,
            Detecting,
            Online,
            Injecting,
            Injected,
            Failed
        }

        // Properties
        private RobloxStatus _currentStatus = RobloxStatus.Offline;
        public RobloxStatus CurrentStatus 
        { 
            get => _currentStatus;
            private set
            {
                _currentStatus = value;
                OnStatusChanged?.Invoke(value);
            }
        }

        private Process? _robloxProcess;
        private CancellationTokenSource? _cts;
        private bool _isAutoAttach = false;
        private readonly int _retryCount = 3;

        // Konfigurasi
        private const string ROBLOX_PROCESS = "RobloxPlayerBeta";
        private const string ROBLOX_STUDIO = "RobloxStudioBeta";
        private const int CHECK_INTERVAL_MS = 500;
        private const int INJECT_DELAY_MS = 1500;

        public AutoInjector()
        {
            OnLog?.Invoke("🔄 AutoInjector initialized");
        }

        public void StartAutoAttach()
        {
            if (_cts != null) return;
            
            _isAutoAttach = true;
            _cts = new CancellationTokenSource();
            CurrentStatus = RobloxStatus.Detecting;
            
            OnLog?.Invoke("🔍 Auto-Attach started - monitoring for Roblox...");
            
            Task.Run(() => MonitorRoblox(_cts.Token));
        }

        public void StopAutoAttach()
        {
            _isAutoAttach = false;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            
            CurrentStatus = RobloxStatus.Offline;
            OnLog?.Invoke("⏹️ Auto-Attach stopped");
        }

        private async Task MonitorRoblox(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var processes = Process.GetProcessesByName(ROBLOX_PROCESS);
                    if (processes.Length == 0)
                    {
                        processes = Process.GetProcessesByName(ROBLOX_STUDIO);
                    }
                    
                    if (processes.Length > 0)
                    {
                        _robloxProcess = processes[0];
                        CurrentStatus = RobloxStatus.Online;
                        OnLog?.Invoke($"✅ Roblox detected! (PID: {_robloxProcess.Id})");
                        
                        // Tunggu sebentar lalu inject
                        await Task.Delay(INJECT_DELAY_MS, token);
                        
                        if (_isAutoAttach && !token.IsCancellationRequested)
                        {
                            await InjectAsync();
                        }
                    }
                    else
                    {
                        if (CurrentStatus == RobloxStatus.Online || 
                            CurrentStatus == RobloxStatus.Injected)
                        {
                            CurrentStatus = RobloxStatus.Offline;
                            OnLog?.Invoke("🔴 Roblox closed");
                        }
                        else
                        {
                            CurrentStatus = RobloxStatus.Detecting;
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke($"⚠️ Monitor error: {ex.Message}");
                }

                await Task.Delay(CHECK_INTERVAL_MS, token);
            }
        }

        public async Task<bool> InjectAsync()
        {
            if (_robloxProcess == null || _robloxProcess.HasExited)
            {
                var processes = Process.GetProcessesByName(ROBLOX_PROCESS);
                if (processes.Length > 0) _robloxProcess = processes[0];
                else
                {
                    processes = Process.GetProcessesByName(ROBLOX_STUDIO);
                    if (processes.Length > 0) _robloxProcess = processes[0];
                }

                if (_robloxProcess == null || _robloxProcess.HasExited)
                {
                    OnLog?.Invoke("❌ No Roblox process to inject");
                    return false;
                }
            }

            CurrentStatus = RobloxStatus.Injecting;
            OnLog?.Invoke($"💉 Injecting into process (PID: {_robloxProcess.Id})...");

            for (int i = 0; i < _retryCount; i++)
            {
                try
                {
                    // Simulasi inject untuk demo
                    await Task.Delay(1000);
                    
                    bool injectionSuccess = PerformInjection(_robloxProcess.Id);
                    
                    if (injectionSuccess)
                    {
                        CurrentStatus = RobloxStatus.Injected;
                        OnLog?.Invoke($"✅ Successfully injected! (Attempt {i+1})");
                        OnInjectionResult?.Invoke(true);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke($"⚠️ Injection attempt {i+1} failed: {ex.Message}");
                }

                if (i < _retryCount - 1)
                {
                    OnLog?.Invoke($"⏳ Retrying in 1 second...");
                    await Task.Delay(1000);
                }
            }

            CurrentStatus = RobloxStatus.Failed;
            OnLog?.Invoke($"❌ Injection failed after {_retryCount} attempts");
            OnInjectionResult?.Invoke(false);
            return false;
        }

        private bool PerformInjection(int pid)
        {
            // Placeholder untuk real manual mapping / DLL injection
            return true;
        }

        public void KillRoblox()
        {
            try
            {
                if (_robloxProcess != null && !_robloxProcess.HasExited)
                {
                    _robloxProcess.Kill();
                    _robloxProcess.WaitForExit(3000);
                    OnLog?.Invoke($"🔫 Roblox terminated (PID: {_robloxProcess.Id})");
                    CurrentStatus = RobloxStatus.Offline;
                }
                else
                {
                    var processes = Process.GetProcessesByName(ROBLOX_PROCESS);
                    foreach (var proc in processes)
                    {
                        proc.Kill();
                        OnLog?.Invoke($"🔫 Killed Roblox (PID: {proc.Id})");
                    }
                    var studioProcesses = Process.GetProcessesByName(ROBLOX_STUDIO);
                    foreach (var proc in studioProcesses)
                    {
                        proc.Kill();
                        OnLog?.Invoke($"🔫 Killed Roblox Studio (PID: {proc.Id})");
                    }
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"⚠️ Kill failed: {ex.Message}");
            }
        }

        public string GetRobloxInstanceInfo()
        {
            if (_robloxProcess != null && !_robloxProcess.HasExited)
            {
                return $"PID: {_robloxProcess.Id} ({_robloxProcess.ProcessName})";
            }
            return "No instance detected";
        }

        public void Dispose()
        {
            StopAutoAttach();
            _cts?.Dispose();
        }
    }
}
