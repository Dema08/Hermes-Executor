#pragma once
#include <windows.h>

#ifdef HERMESCORE_EXPORTS
#define HERMESCORE_API __declspec(dllexport)
#else
#define HERMESCORE_API __declspec(dllimport)
#endif

extern "C" {
    // Fungsi yang sudah ada
    HERMESCORE_API bool InitializeInjector();
    HERMESCORE_API bool InjectRoblox();
    HERMESCORE_API bool IsInjected();
    HERMESCORE_API const char* GetCoreLastError();
    
    // FUNGSI BARU UNTUK EKSEKUSI SCRIPT
    HERMESCORE_API bool ExecuteScript(const char* script);
    HERMESCORE_API bool ExecuteScriptFromFile(const char* filePath);
    HERMESCORE_API bool IsScriptRunning();
}
