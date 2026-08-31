#include <windows.h>
#include <cstdio>

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved) {
    switch (ul_reason_for_call) {
    case DLL_PROCESS_ATTACH: {
        DisableThreadLibraryCalls(hModule);
        AllocConsole();
        FILE* f;
        freopen_s(&f, "CONOUT$", "w", stdout);
        printf("[Hermes] Injected successfully into Roblox!\n");
        break;
    }
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}
