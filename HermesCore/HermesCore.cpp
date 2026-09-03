#define HERMESCORE_EXPORTS
#include "HermesCore.h"
#include "Injector/ManualMapper.h"
#include "Executor/CorescriptExecutor.h"
#include <string>
#include <mutex>
#include <fstream>

std::mutex g_mutex;
bool g_isInjected = false;
bool g_isScriptRunning = false;
std::string g_lastError;

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved) {
    switch (ul_reason_for_call) {
    case DLL_PROCESS_ATTACH:
        DisableThreadLibraryCalls(hModule);
        break;
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

extern "C" {
    __declspec(dllexport) bool InitializeInjector() {
        std::lock_guard<std::mutex> lock(g_mutex);
        g_lastError.clear();
        return true;
    }

    __declspec(dllexport) bool InjectRoblox() {
        std::lock_guard<std::mutex> lock(g_mutex);
        g_lastError.clear();

        DWORD pid = FindRobloxProcess();
        if (pid == 0) {
            g_lastError = "Roblox process not found!";
            g_isInjected = false;
            return false;
        }

        bool result = ManualMapInject(pid);

        if (result) {
            g_isInjected = true;
            g_lastError = "Injection successful!";

            HANDLE hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, pid);
            if (hProcess) {
                SetRobloxProcessHandle(hProcess, pid);
            }
        } else {
            g_isInjected = false;
            g_lastError = "Manual mapping failed!";
        }

        return g_isInjected;
    }

    __declspec(dllexport) bool IsInjected() {
        return g_isInjected;
    }

    __declspec(dllexport) bool IsScriptRunning() {
        return g_isScriptRunning;
    }

    __declspec(dllexport) const char* GetCoreLastError() {
        return g_lastError.c_str();
    }

    __declspec(dllexport) bool ExecuteScript(const char* script) {
        std::lock_guard<std::mutex> lock(g_mutex);
        if (!g_isInjected) {
            g_lastError = "Not injected into Roblox!";
            return false;
        }
        if (script == nullptr || strlen(script) == 0) {
            g_lastError = "Empty script!";
            return false;
        }

        g_isScriptRunning = true;
        bool result = ExecuteCorescript(std::string(script));
        g_isScriptRunning = false;

        if (!result) {
            g_lastError = "Script execution failed!";
        } else {
            g_lastError = "Script executed successfully!";
        }
        return result;
    }

    __declspec(dllexport) bool ExecuteScriptFromFile(const char* filePath) {
        std::lock_guard<std::mutex> lock(g_mutex);
        if (!g_isInjected) {
            g_lastError = "Not injected into Roblox!";
            return false;
        }
        if (filePath == nullptr || strlen(filePath) == 0) {
            g_lastError = "Empty file path!";
            return false;
        }

        std::ifstream file(filePath);
        if (!file.is_open()) {
            g_lastError = "Failed to open script file!";
            return false;
        }

        std::string script((std::istreambuf_iterator<char>(file)),
                            std::istreambuf_iterator<char>());
        file.close();

        g_isScriptRunning = true;
        bool result = ExecuteCorescript(script);
        g_isScriptRunning = false;

        if (!result) {
            g_lastError = "Script execution failed!";
        } else {
            g_lastError = "Script executed successfully!";
        }
        return result;
    }
}
