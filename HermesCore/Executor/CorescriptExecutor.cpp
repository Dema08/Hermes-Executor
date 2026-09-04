#include "CorescriptExecutor.h"
#include "LuauScanner.h"
#include <windows.h>
#include <psapi.h>
#include <vector>
#include <string>
#include <cstdint>
#include <fstream>
#include <cstdio>

// ==============================================
// GLOBALS
// ==============================================
static HANDLE        g_hRobloxProcess = NULL;
static DWORD         g_dwRobloxPid    = 0;
static LuauScanner*  g_pScanner       = nullptr;
static LuauFunctions g_functions;

// ==============================================
// Forward declarations
// ==============================================
static bool ExecuteViaShellcode(const std::string& script);
static bool ExecuteViaLuauVM(const std::string& script);

// ==============================================
// Status helpers (exported via .h)
// ==============================================
bool IsLuauScannerReady() {
    return g_functions.valid;
}

void* GetLuauLoad() {
    return g_functions.luau_load;
}

void* GetLuaPcall() {
    return g_functions.lua_pcall;
}

// ==============================================
// InitializeLuauScanner
// ==============================================
bool InitializeLuauScanner() {
    if (!g_hRobloxProcess || g_hRobloxProcess == INVALID_HANDLE_VALUE) {
        OutputDebugStringA("[CorescriptExecutor] InitializeLuauScanner: no process handle.");
        return false;
    }

    delete g_pScanner;
    g_pScanner  = nullptr;
    g_functions = LuauFunctions{};

    g_pScanner = new LuauScanner(g_hRobloxProcess);
    bool ok    = g_pScanner->Scan();

    if (ok) {
        g_functions = g_pScanner->GetFunctions();
        char dbg[256];
        snprintf(dbg, sizeof(dbg),
            "[CorescriptExecutor] LuauScanner OK. luau_load=%p  lua_pcall=%p",
            g_functions.luau_load, g_functions.lua_pcall);
        OutputDebugStringA(dbg);
    } else {
        g_functions.valid = false;
        OutputDebugStringA("[CorescriptExecutor] LuauScanner: functions not found yet.");
    }
    return ok;
}

// ==============================================
// SetRobloxProcessHandle
// ==============================================
void SetRobloxProcessHandle(HANDLE hProcess, DWORD pid) {
    if (g_hRobloxProcess && g_hRobloxProcess != INVALID_HANDLE_VALUE)
        CloseHandle(g_hRobloxProcess);

    g_hRobloxProcess = hProcess;
    g_dwRobloxPid    = pid;

    char dbg[128];
    snprintf(dbg, sizeof(dbg),
        "[CorescriptExecutor] Process handle set. PID: %lu", pid);
    OutputDebugStringA(dbg);

    InitializeLuauScanner();
}

// ==============================================
// ExecuteViaLuauVM
//
// Membaca lua_State* yang benar (nilai yang tersimpan di global Roblox),
// lalu menjalankan shellcode: luau_load(script) -> lua_pcall(script).
// ==============================================
static bool ExecuteViaLuauVM(const std::string& script) {
    if (!g_functions.valid) {
        OutputDebugStringA("[CorescriptExecutor] LuauVM: retrying scan...");
        InitializeLuauScanner();
        if (!g_functions.valid) {
            OutputDebugStringA("[CorescriptExecutor] LuauVM: scan still failed, skipping.");
            return false;
        }
    }

    if (!g_hRobloxProcess || g_hRobloxProcess == INVALID_HANDLE_VALUE) return false;

    // --- Ambil lua_State* yang sebenarnya --------------------------------
    // g_functions.lua_State_ptr = base + Offsets::lua_State (alamat sebuah
    // global yang menyimpan lua_State*). Kita baca nilainya secara runtime.
    uint64_t luaStateAddr = (uint64_t)(uintptr_t)g_functions.lua_State_ptr;
    uint64_t luaState = 0;
    SIZE_T bytesRead = 0;
    bool readOk = ReadProcessMemory(g_hRobloxProcess, (LPVOID)luaStateAddr,
                                    &luaState, sizeof(luaState), &bytesRead) &&
                  bytesRead == sizeof(luaState);

    if (!readOk || luaState == 0) {
        // Fallback: anggap offset menunjuk langsung ke state (bukan pointer global).
        luaState = luaStateAddr;
        OutputDebugStringA("[CorescriptExecutor] LuauVM: global lua_State read failed, using address directly.\n");
    }

    char dbg[192];
    snprintf(dbg, sizeof(dbg), "[CorescriptExecutor] lua_State addr=0x%llX  state=0x%llX\n",
             luaStateAddr, luaState);
    OutputDebugStringA(dbg);

    // --- Shellcode (x64, MS ABI) ----------------------------------------
    // Entry: rcx = lua_State* (dari lpParameter).
    // Menyimpan hasil luau_load ke [results+0] dan lua_pcall ke [results+8].
    // Popup hasil dibaca kembali setelah thread selesai (real status).
    //
    // Patches: results@0x07, chunkname@0x18, source@0x22, size@0x2C,
    //          load@0x3F, pcall@0x5D
    const char chunkName[] = "HermesScript";
    SIZE_T scriptLen    = script.size();        // tanpa null
    SIZE_T chunkNameLen = sizeof(chunkName);    // dengan null

    std::vector<BYTE> sc = {
        0x50,                                     // 0x00 push rbx
        0x41, 0x55,                               // 0x01 push r13
        0x48, 0x89, 0xCB,                         // 0x03 mov rbx, rcx  (save L)
        0x49, 0xBD, 0,0,0,0,0,0,0,0,              // 0x05 mov r13, results   (imm@0x07)
        0x48, 0x83, 0xEC, 0x30,                   // 0x0F sub rsp, 0x30
        0x48, 0x89, 0xD9,                         // 0x13 mov rcx, rbx  (L)
        0x48, 0xBA, 0,0,0,0,0,0,0,0,              // 0x16 mov rdx, chunkname (imm@0x18)
        0x49, 0xB8, 0,0,0,0,0,0,0,0,              // 0x20 mov r8,  source    (imm@0x22)
        0x49, 0xB9, 0,0,0,0,0,0,0,0,              // 0x2A mov r9,  size      (imm@0x2C)
        0x48, 0xC7, 0x44, 0x24, 0x20, 0,0,0,0,    // 0x34 mov [rsp+0x20], 0  (debug=0)
        0x48, 0xB8, 0,0,0,0,0,0,0,0,              // 0x3D mov rax, luau_load (imm@0x3F)
        0xFF, 0xD0,                               // 0x47 call rax
        0x49, 0x89, 0x45, 0x00,                   // 0x49 mov [r13+0], rax   (luau_load result)
        0x48, 0x89, 0xD9,                         // 0x4D mov rcx, rbx  (L)
        0x33, 0xD2,                               // 0x50 xor edx, edx  (nargs=0)
        0x41, 0xB8, 0x01,0,0,0,                   // 0x52 mov r8d, 1    (nresults=1)
        0x45, 0x33, 0xC9,                         // 0x58 xor r9d, r9d  (errfunc=0)
        0x48, 0xB8, 0,0,0,0,0,0,0,0,              // 0x5B mov rax, lua_pcall (imm@0x5D)
        0xFF, 0xD0,                               // 0x65 call rax
        0x49, 0x89, 0x45, 0x08,                   // 0x67 mov [r13+8], rax   (lua_pcall result)
        0x48, 0x83, 0xC4, 0x30,                   // 0x6B add rsp, 0x30
        0x41, 0x5D,                               // 0x6F pop r13
        0x5B,                                     // 0x71 pop rbx
        0x33, 0xC0,                               // 0x72 xor eax, eax
        0xC3                                      // 0x74 ret
    };

    if (sc.size() != 0x75) {
        OutputDebugStringA("[CorescriptExecutor] LuauVM: shellcode size mismatch!\n");
        return false;
    }

    // --- Alokasi remote memory -----------------------------------------
    SIZE_T scSize = sc.size();
    const size_t RESULTS_SLOT_SIZE = 16;
    SIZE_T total = scSize + scriptLen + 1 + chunkNameLen + RESULTS_SLOT_SIZE;

    LPVOID pRemote = VirtualAllocEx(g_hRobloxProcess, NULL, total,
                                    MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
    if (!pRemote) {
        OutputDebugStringA("[CorescriptExecutor] LuauVM: VirtualAllocEx failed.");
        return false;
    }

    BYTE* pScriptRemote    = static_cast<BYTE*>(pRemote) + scSize;
    BYTE* pChunkNameRemote = pScriptRemote + scriptLen + 1;
    BYTE* pResultsRemote   = pChunkNameRemote + chunkNameLen;

    WriteProcessMemory(g_hRobloxProcess, pScriptRemote, script.c_str(), scriptLen + 1, NULL);
    WriteProcessMemory(g_hRobloxProcess, pChunkNameRemote, chunkName, chunkNameLen, NULL);

    auto patch64 = [&](size_t off, uint64_t value) { memcpy(sc.data() + off, &value, 8); };
    patch64(0x07, reinterpret_cast<uint64_t>(pResultsRemote));    // r13 = results
    patch64(0x18, reinterpret_cast<uint64_t>(pChunkNameRemote));  // rdx = chunkname
    patch64(0x22, reinterpret_cast<uint64_t>(pScriptRemote));     // r8  = source
    patch64(0x2C, static_cast<uint64_t>(scriptLen));              // r9  = size
    patch64(0x3F, reinterpret_cast<uint64_t>(g_functions.luau_load)); // luau_load
    patch64(0x5D, reinterpret_cast<uint64_t>(g_functions.lua_pcall)); // lua_pcall

    WriteProcessMemory(g_hRobloxProcess, pRemote, sc.data(), scSize, NULL);

    DWORD threadId = 0;
    HANDLE hThread = CreateRemoteThread(g_hRobloxProcess, NULL, 0,
        reinterpret_cast<LPTHREAD_START_ROUTINE>(pRemote),
        (LPVOID)(uintptr_t)luaState, 0, &threadId);

    if (!hThread) {
        snprintf(dbg, sizeof(dbg), "[CorescriptExecutor] LuauVM: CreateRemoteThread failed! Error: %lu", GetLastError());
        OutputDebugStringA(dbg);
        VirtualFreeEx(g_hRobloxProcess, pRemote, 0, MEM_RELEASE);
        return false;
    }

    WaitForSingleObject(hThread, 5000);

    // --- Baca status Luau yang sebenarnya --------------------------------
    struct LuauResults { uint64_t load_ret; uint64_t pcall_ret; };
    LuauResults results = {};
    ReadProcessMemory(g_hRobloxProcess, pResultsRemote, &results, sizeof(results), NULL);

    DWORD exitCode = 0;
    GetExitCodeThread(hThread, &exitCode);
    CloseHandle(hThread);
    VirtualFreeEx(g_hRobloxProcess, pRemote, 0, MEM_RELEASE);

    // LUA_OK = 0. pcall_ret != 0 => ada error. load_ret == 0 => gagal kompilasi/load.
    snprintf(dbg, sizeof(dbg),
        "[CorescriptExecutor] LuauVM: load_ret=0x%llX pcall_ret=0x%llX (%s)\n",
        results.load_ret, results.pcall_ret,
        (results.pcall_ret == 0) ? "LUA_OK - script berjalan" : "LUA_ERR - script gagal");
    OutputDebugStringA(dbg);

    // Tulis status ke file agar mudah dicek tanpa DebugView.
    FILE* f = nullptr;
    if (fopen_s(&f, "C:\\hermes_exec_status.txt", "w") == 0 && f) {
        fprintf(f, "load_ret=0x%llX\npcall_ret=0x%llX\nlua_state=0x%llX\n",
                results.load_ret, results.pcall_ret, luaState);
        fclose(f);
    }

    return (results.pcall_ret == 0);
}

// ==============================================
// ExecuteViaShellcode (clean fallback)
// Runs a no-op stub in Roblox — proves remote thread works
// ==============================================
static bool ExecuteViaShellcode(const std::string& script) {
    if (!g_hRobloxProcess || g_hRobloxProcess == INVALID_HANDLE_VALUE) {
        OutputDebugStringA("[CorescriptExecutor] Shellcode: no process handle.");
        return false;
    }

    // sub rsp,28 / xor eax,eax / add rsp,28 / ret
    BYTE stub[] = {
        0x48, 0x83, 0xEC, 0x28,
        0x33, 0xC0,
        0x48, 0x83, 0xC4, 0x28,
        0xC3
    };
    SIZE_T scriptLen = script.size() + 1;
    SIZE_T total     = sizeof(stub) + scriptLen;

    LPVOID pRemote = VirtualAllocEx(g_hRobloxProcess, NULL, total,
                                    MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
    if (!pRemote) {
        OutputDebugStringA("[CorescriptExecutor] Shellcode: VirtualAllocEx failed.");
        return false;
    }

    WriteProcessMemory(g_hRobloxProcess, pRemote, stub, sizeof(stub), NULL);
    WriteProcessMemory(g_hRobloxProcess,
        static_cast<BYTE*>(pRemote) + sizeof(stub),
        script.c_str(), scriptLen, NULL);

    DWORD  threadId = 0;
    HANDLE hThread  = CreateRemoteThread(g_hRobloxProcess, NULL, 0,
        reinterpret_cast<LPTHREAD_START_ROUTINE>(pRemote),
        NULL, 0, &threadId);

    if (!hThread) {
        char dbg[128];
        snprintf(dbg, sizeof(dbg),
            "[CorescriptExecutor] Shellcode: CreateRemoteThread failed! Error: %lu",
            GetLastError());
        OutputDebugStringA(dbg);
        VirtualFreeEx(g_hRobloxProcess, pRemote, 0, MEM_RELEASE);
        return false;
    }

    WaitForSingleObject(hThread, 3000);
    CloseHandle(hThread);
    VirtualFreeEx(g_hRobloxProcess, pRemote, 0, MEM_RELEASE);

    OutputDebugStringA("[CorescriptExecutor] Shellcode: stub thread completed.");
    return true;
}

// ==============================================
// ExecuteCorescript - main entry called by HermesCore.cpp
// ==============================================
bool ExecuteCorescript(const std::string& script) {
    if (script.empty()) {
        OutputDebugStringA("[CorescriptExecutor] Empty script.");
        return false;
    }

    if (!g_hRobloxProcess || g_hRobloxProcess == INVALID_HANDLE_VALUE) {
        OutputDebugStringA("[CorescriptExecutor] No process handle!");
        return false;
    }

    // Satu-satunya jalur eksekusi nyata: panggil Luau VM di Roblox.
    OutputDebugStringA("[CorescriptExecutor] Trying ExecuteViaLuauVM...");
    bool ok = ExecuteViaLuauVM(script);
    if (ok) {
        OutputDebugStringA("[CorescriptExecutor] LuauVM: SUCCESS (LUA_OK).");
    } else {
        OutputDebugStringA("[CorescriptExecutor] LuauVM: FAILED (script tidak berjalan).");
    }
    return ok;
}
