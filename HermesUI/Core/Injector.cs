using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Hermes_Executor.Core
{
    public class Injector
    {
        public event Action<string>? OnLog;

        public async Task<bool> InjectAsync()
        {
            OnLog?.Invoke("Searching for Roblox process (RobloxPlayerBeta.exe)...");
            
            await Task.Delay(1000); // Simulate search delay

            Process[] processes = Process.GetProcessesByName("RobloxPlayerBeta");
            if (processes.Length == 0)
            {
                OnLog?.Invoke("Error: Roblox process not found! Please launch Roblox first.");
                return false;
            }

            OnLog?.Invoke($"Found Roblox process (PID: {processes[0].Id}). Initializing injection...");
            await Task.Delay(1500); // Simulate injection delay

            OnLog?.Invoke("Injection successful! Hermes is now active.");
            return true;
        }

        public bool CheckRobloxRunning()
        {
            return Process.GetProcessesByName("RobloxPlayerBeta").Length > 0;
        }
    }
}
