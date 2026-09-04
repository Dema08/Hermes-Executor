#include "ManualMapper.h"
#include "../Offsets/Offsets.h"
#include <tlhelp32.h>
#include <string>
#include <fstream>
#include <vector>
#include <algorithm>
#include <psapi.h>

#pragma comment(lib, "ntdll.lib")

extern "C" __declspec(dllexport) void PayloadEntry();

// Base address RobloxPlayerBeta.exe yang di-resolve saat runtime (ASLR-safe).
// Dipakai oleh BypassHyperionIntegrity dan dapat dibaca komponen lain.
static uint64_t g_robloxBase = 0;

uint64_t GetRobloxModuleBase() {
    return g_robloxBase;
}

// Resolve base address modul (mis. RobloxPlayerBeta.exe) di proses target saat runtime.
uint64_t ResolveRobloxBase(HANDLE hProcess) {
    HMODULE hMods[1024];
    DWORD cbNeeded = 0;
    if (!EnumProcessModules(hProcess, hMods, sizeof(hMods), &cbNeeded)) return 0;

    DWORD count = cbNeeded / sizeof(HMODULE);
    for (DWORD i = 0; i < count; ++i) {
        char szName[MAX_PATH];
        if (!GetModuleBaseNameA(hProcess, hMods[i], szName, sizeof(szName))) continue;
        std::string name = szName;
        std::transform(name.begin(), name.end(), name.begin(), ::tolower);
        if (name.find("robloxplayerbeta") != std::string::npos ||
            name.find("robloxstudiobeta") != std::string::npos) {
            MODULEINFO mi{};
            if (GetModuleInformation(hProcess, hMods[i], &mi, sizeof(mi)))
                return (uint64_t)(uintptr_t)mi.lpBaseOfDll;
        }
    }
    return 0;
}

DWORD FindRobloxProcess() {
    HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (hSnapshot == INVALID_HANDLE_VALUE) return 0;

    PROCESSENTRY32W pe = { sizeof(PROCESSENTRY32W) };
    if (Process32FirstW(hSnapshot, &pe)) {
        do {
            std::wstring name = pe.szExeFile;
            if (name.find(L"RobloxPlayerBeta.exe") != std::wstring::npos ||
                name.find(L"RobloxStudioBeta.exe") != std::wstring::npos) {
                CloseHandle(hSnapshot);
                return pe.th32ProcessID;
            }
        } while (Process32NextW(hSnapshot, &pe));
    }
    CloseHandle(hSnapshot);
    return 0;
}

HANDLE GetRobloxProcessHandle(DWORD pid) {
    HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, pid);
    if (!hProcess) {
        return OpenProcess(PROCESS_ALL_ACCESS, FALSE, pid);
    }

    HANDLE hFullAccess = NULL;
    HANDLE hCurrentProcess = GetCurrentProcess();

    typedef NTSTATUS(WINAPI* NtDuplicateObject_t)(
        HANDLE SourceProcessHandle,
        HANDLE SourceHandle,
        HANDLE TargetProcessHandle,
        PHANDLE TargetHandle,
        ACCESS_MASK DesiredAccess,
        ULONG Attributes,
        ULONG Options
    );

    HMODULE hNtdll = GetModuleHandleW(L"ntdll.dll");
    auto NtDuplicateObject = (NtDuplicateObject_t)GetProcAddress(hNtdll, "NtDuplicateObject");

    if (NtDuplicateObject) {
        NtDuplicateObject(hCurrentProcess, hProcess, hCurrentProcess,
            &hFullAccess, PROCESS_ALL_ACCESS, 0, 0);
    }

    CloseHandle(hProcess);
    return hFullAccess ? hFullAccess : OpenProcess(PROCESS_ALL_ACCESS, FALSE, pid);
}

void BypassHyperionIntegrity(HANDLE hProcess, LPVOID pBase) {
    // pBase = base address RobloxPlayerBeta.exe yang di-resolve saat runtime.
    // (BUKAN alamat alokasi payload.)
    uint64_t base = (uint64_t)(uintptr_t)pBase;
    char dbg[256];
    snprintf(dbg, sizeof(dbg), "[BypassHyperionIntegrity] Roblox base = 0x%llX", base);
    OutputDebugStringA(dbg);

    // 1. Patch Page Hash Check (InsertSet)
    BYTE nopBytes[] = { 0x90, 0x90, 0x90, 0x90, 0x90 };
    DWORD_PTR pPageHashCheck = (DWORD_PTR)base + Offsets::Offset_InsertSet;
    DWORD oldProtect;
    VirtualProtectEx(hProcess, (LPVOID)pPageHashCheck, sizeof(nopBytes), PAGE_EXECUTE_READWRITE, &oldProtect);
    WriteProcessMemory(hProcess, (LPVOID)pPageHashCheck, nopBytes, sizeof(nopBytes), NULL);
    VirtualProtectEx(hProcess, (LPVOID)pPageHashCheck, sizeof(nopBytes), oldProtect, &oldProtect);

    // 2. Patch CFG Check (_guard_check_icall)
    BYTE retBytes[] = { 0xC3 }; // RET
    DWORD_PTR pCFGCheck = (DWORD_PTR)base + Offsets::Offset_CFG_Check;
    VirtualProtectEx(hProcess, (LPVOID)pCFGCheck, sizeof(retBytes), PAGE_EXECUTE_READWRITE, &oldProtect);
    WriteProcessMemory(hProcess, (LPVOID)pCFGCheck, retBytes, sizeof(retBytes), NULL);
    VirtualProtectEx(hProcess, (LPVOID)pCFGCheck, sizeof(retBytes), oldProtect, &oldProtect);

    // 3. Whitelist pages
    DWORD_PTR pWhitelist = (DWORD_PTR)base + Offsets::Offset_WhitelistedPages;
    DWORD pageIndex = (DWORD)(base >> Offsets::kPageShift);
    WriteProcessMemory(hProcess, (LPVOID)(pWhitelist + (pageIndex * 8)), &pageIndex, sizeof(DWORD), NULL);
}

bool ResolveImports(HANDLE hProcess, LPVOID pRemoteBase, PIMAGE_NT_HEADERS pNtHeaders,
    const std::vector<BYTE>& dllData) {
    PIMAGE_DATA_DIRECTORY pImportDir = &pNtHeaders->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT];
    if (pImportDir->Size == 0) return true;

    PIMAGE_IMPORT_DESCRIPTOR pImportDesc = (PIMAGE_IMPORT_DESCRIPTOR)(dllData.data() + pImportDir->VirtualAddress);

    while (pImportDesc->Name) {
        const char* dllName = (const char*)(dllData.data() + pImportDesc->Name);

        HMODULE hLocalDll = LoadLibraryA(dllName);
        if (!hLocalDll) {
            pImportDesc++;
            continue;
        }

        PIMAGE_THUNK_DATA pThunk = (PIMAGE_THUNK_DATA)(dllData.data() + pImportDesc->OriginalFirstThunk);
        PIMAGE_THUNK_DATA pIAT = (PIMAGE_THUNK_DATA)(dllData.data() + pImportDesc->FirstThunk);

        while (pThunk->u1.AddressOfData) {
            DWORD_PTR functionAddress = 0;

            if (pThunk->u1.Ordinal & IMAGE_ORDINAL_FLAG) {
                WORD ordinal = IMAGE_ORDINAL(pThunk->u1.Ordinal);
                functionAddress = (DWORD_PTR)GetProcAddress(hLocalDll, (LPCSTR)ordinal);
            }
            else {
                PIMAGE_IMPORT_BY_NAME pImportByName = (PIMAGE_IMPORT_BY_NAME)(dllData.data() + pThunk->u1.AddressOfData);
                functionAddress = (DWORD_PTR)GetProcAddress(hLocalDll, pImportByName->Name);
            }

            if (functionAddress) {
                WriteProcessMemory(hProcess,
                    (BYTE*)pRemoteBase + (DWORD_PTR)pIAT - (DWORD_PTR)dllData.data(),
                    &functionAddress, sizeof(DWORD_PTR), NULL);
            }

            pThunk++;
            pIAT++;
        }

        FreeLibrary(hLocalDll);
        pImportDesc++;
    }

    return true;
}

void RelocateImage(const std::vector<BYTE>& dllData, PIMAGE_NT_HEADERS pNtHeaders,
    DWORD_PTR delta) {
    PIMAGE_DATA_DIRECTORY pRelocDir = &pNtHeaders->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC];
    if (pRelocDir->Size == 0) return;

    // Operasikan pada buffer LOKAL (dllData), bukan dereference alamat remote.
    BYTE* pImage = (BYTE*)dllData.data();
    DWORD_PTR pRelocData = (DWORD_PTR)dllData.data() + pRelocDir->VirtualAddress;
    DWORD_PTR pRelocEnd = pRelocData + pRelocDir->Size;

    while (pRelocData < pRelocEnd) {
        PIMAGE_BASE_RELOCATION pReloc = (PIMAGE_BASE_RELOCATION)pRelocData;
        if (pReloc->SizeOfBlock == 0) break;

        DWORD count = (pReloc->SizeOfBlock - sizeof(IMAGE_BASE_RELOCATION)) / sizeof(WORD);
        WORD* pItems = (WORD*)(pRelocData + sizeof(IMAGE_BASE_RELOCATION));

        for (DWORD i = 0; i < count; i++) {
            WORD type = pItems[i] >> 12;
            WORD offset = pItems[i] & 0xFFF;

            if (type == IMAGE_REL_BASED_DIR64) {
                DWORD_PTR* pAddress = (DWORD_PTR*)(pImage + pReloc->VirtualAddress + offset);
                *pAddress += delta;
            }
            else if (type == IMAGE_REL_BASED_HIGHLOW) {
                DWORD* pAddress = (DWORD*)(pImage + pReloc->VirtualAddress + offset);
                *pAddress += (DWORD)delta;
            }
        }

        pRelocData += pReloc->SizeOfBlock;
    }
}

std::vector<BYTE> ReadDLLFromFile(const std::string& path) {
    std::ifstream file(path, std::ios::binary | std::ios::ate);
    if (!file.is_open()) return {};

    std::streamsize size = file.tellg();
    file.seekg(0, std::ios::beg);

    std::vector<BYTE> buffer((size_t)size);
    file.read((char*)buffer.data(), size);
    file.close();

    return buffer;
}

bool ManualMapInject(DWORD pid) {
    char dbgBuf[512];
    std::vector<BYTE> dllData;

    char dllPath[MAX_PATH];
    GetModuleFileNameA(GetModuleHandleA("HermesCore.dll"), dllPath, MAX_PATH);
    std::string payloadPath = std::string(dllPath);
    size_t lastSlash = payloadPath.find_last_of("\\/");
    if (lastSlash != std::string::npos) {
        payloadPath = payloadPath.substr(0, lastSlash + 1) + "HermesPayload.dll";
    }
    
    snprintf(dbgBuf, sizeof(dbgBuf), "[DEBUG] Payload path: %s", payloadPath.c_str());
    OutputDebugStringA(dbgBuf);
    
    dllData = ReadDLLFromFile(payloadPath);

    if (dllData.empty()) {
        OutputDebugStringA("[DEBUG] HermesPayload.dll not found, falling back to HermesCore.dll image");
        HMODULE hCurrentModule = GetModuleHandleA("HermesCore.dll");
        if (hCurrentModule) {
            MODULEINFO modInfo;
            GetModuleInformation(GetCurrentProcess(), hCurrentModule, &modInfo, sizeof(modInfo));
            dllData.resize((size_t)modInfo.SizeOfImage);
            memcpy(dllData.data(), modInfo.lpBaseOfDll, modInfo.SizeOfImage);
            snprintf(dbgBuf, sizeof(dbgBuf), "[DEBUG] DLL size (fallback): %zu bytes", dllData.size());
            OutputDebugStringA(dbgBuf);
        }
    } else {
        snprintf(dbgBuf, sizeof(dbgBuf), "[DEBUG] DLL size: %zu bytes", dllData.size());
        OutputDebugStringA(dbgBuf);
    }

    if (dllData.empty()) {
        OutputDebugStringA("[DEBUG] Failed to read DLL!");
        return false;
    }

    HANDLE hProcess = GetRobloxProcessHandle(pid);
    if (!hProcess) {
        OutputDebugStringA("[DEBUG] Failed to open Roblox process!");
        return false;
    }
    snprintf(dbgBuf, sizeof(dbgBuf), "[DEBUG] Process opened successfully! PID: %lu", pid);
    OutputDebugStringA(dbgBuf);

    PIMAGE_DOS_HEADER pDosHeader = (PIMAGE_DOS_HEADER)dllData.data();
    if (pDosHeader->e_magic != IMAGE_DOS_SIGNATURE) {
        CloseHandle(hProcess);
        return false;
    }

    PIMAGE_NT_HEADERS pNtHeaders = (PIMAGE_NT_HEADERS)(dllData.data() + pDosHeader->e_lfanew);
    if (pNtHeaders->Signature != IMAGE_NT_SIGNATURE) {
        CloseHandle(hProcess);
        return false;
    }

    LPVOID pRemoteBase = VirtualAllocEx(hProcess, NULL,
        pNtHeaders->OptionalHeader.SizeOfImage,
        MEM_COMMIT | MEM_RESERVE,
        PAGE_EXECUTE_READWRITE);

    if (!pRemoteBase) {
        CloseHandle(hProcess);
        return false;
    }

    // Relokasi DILAKUKAN pada buffer lokal (dllData) SEBELUM ditulis ke remote.
    DWORD_PTR delta = (DWORD_PTR)pRemoteBase - pNtHeaders->OptionalHeader.ImageBase;
    if (delta) {
        RelocateImage(dllData, pNtHeaders, delta);
    }

    WriteProcessMemory(hProcess, pRemoteBase, dllData.data(),
        pNtHeaders->OptionalHeader.SizeOfHeaders, NULL);

    PIMAGE_SECTION_HEADER pSection = IMAGE_FIRST_SECTION(pNtHeaders);
    for (int i = 0; i < pNtHeaders->FileHeader.NumberOfSections; i++) {
        if (pSection[i].SizeOfRawData > 0) {
            WriteProcessMemory(hProcess,
                (BYTE*)pRemoteBase + pSection[i].VirtualAddress,
                dllData.data() + pSection[i].PointerToRawData,
                pSection[i].SizeOfRawData,
                NULL);
        }
    }

    if (!ResolveImports(hProcess, pRemoteBase, pNtHeaders, dllData)) {
        VirtualFreeEx(hProcess, pRemoteBase, 0, MEM_RELEASE);
        CloseHandle(hProcess);
        return false;
    }

    // Resolve base address RobloxPlayerBeta.exe saat runtime (ASLR-safe)
    uint64_t robloxBase = ResolveRobloxBase(hProcess);
    if (robloxBase) {
        g_robloxBase = robloxBase;
    } else {
        g_robloxBase = 0;
        OutputDebugStringA("[ManualMapper] ⚠️ Could not resolve Roblox base! Skipping hyperion bypass.\n");
    }

    if (g_robloxBase) {
        // Patch Roblox memory menggunakan base address asli (bukan alokasi payload)
        BypassHyperionIntegrity(hProcess, (LPVOID)(uintptr_t)g_robloxBase);
    }

    DWORD entryPoint = pNtHeaders->OptionalHeader.AddressOfEntryPoint;
    DWORD threadId;
    HANDLE hThread = CreateRemoteThread(hProcess, NULL, 0,
        (LPTHREAD_START_ROUTINE)((BYTE*)pRemoteBase + entryPoint),
        pRemoteBase, 0, &threadId);

    if (!hThread) {
        VirtualFreeEx(hProcess, pRemoteBase, 0, MEM_RELEASE);
        CloseHandle(hProcess);
        return false;
    }

    WaitForSingleObject(hThread, 5000);
    CloseHandle(hThread);
    CloseHandle(hProcess);

    Sleep(500);

    HANDLE hMarker = CreateFileA("C:\\hermes_payload_active.txt", 
                                  GENERIC_READ, FILE_SHARE_READ, NULL, 
                                  OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    if (hMarker == INVALID_HANDLE_VALUE) {
        OutputDebugStringA("[ManualMapper] ⚠️ Payload marker NOT found, but DLL mapped.\n");
    } else {
        char buffer[256] = {0};
        DWORD read = 0;
        ReadFile(hMarker, buffer, sizeof(buffer) - 1, &read, NULL);
        OutputDebugStringA("[ManualMapper] Payload marker: ");
        OutputDebugStringA(buffer);
        OutputDebugStringA("\n");
        CloseHandle(hMarker);
    }

    OutputDebugStringA("[ManualMapper] ✅ Injection completed.\n");
    return true;
}
