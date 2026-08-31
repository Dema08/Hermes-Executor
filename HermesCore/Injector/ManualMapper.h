#pragma once
#include <windows.h>
#include <vector>

// Cari PID Roblox
DWORD FindRobloxProcess();

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
