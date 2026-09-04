#pragma once
#include <windows.h>
#include <vector>

// Cari PID Roblox
DWORD FindRobloxProcess();

// Base address RobloxPlayerBeta.exe yang di-resolve saat runtime (ASLR-safe)
uint64_t ResolveRobloxBase(HANDLE hProcess);
uint64_t GetRobloxModuleBase();

// Struktur data untuk manual mapping
struct ManualMappingData {
    LPVOID pLoadLibraryA;
    LPVOID pGetProcAddress;
    LPVOID pBase;
    DWORD dwEntryPoint;
};

// Fungsi utama manual mapping
bool ManualMapInject(DWORD pid);

// Baca DLL dari file (akan kita load dari resource atau file terpisah)
std::vector<BYTE> ReadDLLFromMemory();
