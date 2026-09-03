#include <windows.h>
#include <stdio.h>
#include <string>
#include <mutex>

static HANDLE g_hEvent = NULL;
static std::string g_lastScript;

// ============================================
// 1. BUAT FILE MARKER SEBAGAI BUKTI PAYLOAD AKTIF
// ============================================
void CreatePayloadMarker() {
    HANDLE hFile = CreateFileA("C:\\hermes_payload_active.txt", 
                                GENERIC_WRITE, 0, NULL, 
                                CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (hFile != INVALID_HANDLE_VALUE) {
        DWORD written;
        char buffer[256];
        snprintf(buffer, sizeof(buffer), "Hermes Payload Active! PID: %d Time: %lld", 
                 GetCurrentProcessId(), GetTickCount64());
        WriteFile(hFile, buffer, (DWORD)strlen(buffer), &written, NULL);
        CloseHandle(hFile);
    }
}

// ============================================
// 2. CONSOLE DEBUG
// ============================================
void CreateDebugConsole() {
    if (AllocConsole()) {
        FILE* f;
        freopen_s(&f, "CONOUT$", "w", stdout);
        freopen_s(&f, "CONIN$", "r", stdin);
        SetConsoleTitleA("Hermes Payload Debug");
        printf("[HERMES] ========================================\n");
        printf("[HERMES] Payload loaded in Roblox!\n");
        printf("[HERMES] PID: %d\n", GetCurrentProcessId());
        printf("[HERMES] Time: %lld\n", GetTickCount64());
        printf("[HERMES] ========================================\n");
    }
}

// ============================================
// 3. THREAD UTAMA PAYLOAD (TUNGGU PERINTAH)
// ============================================
DWORD WINAPI PayloadMainThread(LPVOID lpParam) {
    printf("[HERMES] Payload thread started!\n");
    
    CreatePayloadMarker();
    printf("[HERMES] ✅ Marker file created: C:\\hermes_payload_active.txt\n");
    
    while (true) {
        if (g_hEvent) {
            WaitForSingleObject(g_hEvent, INFINITE);
            
            if (!g_lastScript.empty()) {
                printf("[HERMES] Executing script: %s\n", g_lastScript.c_str());
            }
        }
        Sleep(100);
    }
    
    return 0;
}

// ============================================
// 4. DLL MAIN
// ============================================
BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved) {
    switch (ul_reason_for_call) {
    case DLL_PROCESS_ATTACH:
        DisableThreadLibraryCalls(hModule);
        
        CreateDebugConsole();
        printf("[HERMES] DllMain: DLL_PROCESS_ATTACH\n");
        
        g_hEvent = CreateEventA(NULL, FALSE, FALSE, "Hermes_Payload_Event");
        if (g_hEvent) {
            printf("[HERMES] ✅ Event created: Hermes_Payload_Event\n");
        }
        
        CreateThread(NULL, 0, PayloadMainThread, NULL, 0, NULL);
        printf("[HERMES] ✅ Payload thread started\n");
        break;
        
    case DLL_PROCESS_DETACH:
        printf("[HERMES] DllMain: DLL_PROCESS_DETACH\n");
        if (g_hEvent) {
            CloseHandle(g_hEvent);
        }
        break;
    }
    return TRUE;
}

// ============================================
// 5. EXPORT - DIPANGGIL DARI HERMESCORE
// ============================================
extern "C" __declspec(dllexport) void PayloadEntry() {
    printf("[HERMES] PayloadEntry called!\n");
    CreatePayloadMarker();
}
