#pragma once
#include <string>
#include <windows.h>

// Fungsi utama yang dipanggil dari HermesCore.cpp
bool ExecuteCorescript(const std::string& script);

// Set handle process Roblox (dipanggil setelah inject berhasil)
void SetRobloxProcessHandle(HANDLE hProcess, DWORD pid);

// Inisialisasi LuauScanner secara manual
bool InitializeLuauScanner();

// Status scanner
bool IsLuauScannerReady();

// Akses pointer fungsi yang ditemukan scanner
void* GetLuauLoad();
void* GetLuaPcall();
