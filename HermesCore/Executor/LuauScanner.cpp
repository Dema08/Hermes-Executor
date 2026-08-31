#include "LuauScanner.h"
#include <psapi.h>
#include <algorithm>
#include <cstring>

// -------------------------------------------------------
// Constructor
// -------------------------------------------------------
LuauScanner::LuauScanner(HANDLE hProcess) : m_hProcess(hProcess) {}

// -------------------------------------------------------
// ScanBuffer - inner pattern matching over a single buffer
// -------------------------------------------------------
void* LuauScanner::ScanBuffer(const BYTE*              buffer,
                               size_t                   bufSize,
                               BYTE*                    moduleBase,
                               const std::vector<BYTE>& pattern,
                               const char*              mask) {
    size_t patLen = pattern.size();
    if (bufSize < patLen) return nullptr;

    for (size_t i = 0; i <= bufSize - patLen; ++i) {
        bool match = true;
        for (size_t k = 0; k < patLen; ++k) {
            if (mask[k] == 'x' && buffer[i + k] != pattern[k]) {
                match = false;
                break;
            }
        }
        if (match) return moduleBase + i;
    }
    return nullptr;
}

// -------------------------------------------------------
// FindPatternInModule
// -------------------------------------------------------
void* LuauScanner::FindPatternInModule(const std::string&       moduleName,
                                        const std::vector<BYTE>& pattern,
                                        const char*              mask) {
    if (!m_hProcess || m_hProcess == INVALID_HANDLE_VALUE) return nullptr;

    HMODULE hMods[1024];
    DWORD   cbNeeded = 0;
    if (!EnumProcessModules(m_hProcess, hMods, sizeof(hMods), &cbNeeded)) return nullptr;

    char    szName[MAX_PATH];
    std::string target = moduleName;
    std::transform(target.begin(), target.end(), target.begin(), ::tolower);

    DWORD count = cbNeeded / sizeof(HMODULE);
    for (DWORD i = 0; i < count; ++i) {
        if (!GetModuleBaseNameA(m_hProcess, hMods[i], szName, sizeof(szName))) continue;

        std::string current(szName);
        std::transform(current.begin(), current.end(), current.begin(), ::tolower);
        if (current.find(target) == std::string::npos) continue;

        MODULEINFO mi{};
        if (!GetModuleInformation(m_hProcess, hMods[i], &mi, sizeof(mi))) continue;
        if (!mi.lpBaseOfDll || mi.SizeOfImage == 0) continue;

        char dbg[256];
        snprintf(dbg, sizeof(dbg), "[LuauScanner] Scanning module: %s  base=%p  size=%lu",
                 szName, mi.lpBaseOfDll, mi.SizeOfImage);
        OutputDebugStringA(dbg);

        // Read in 4 MB chunks
        constexpr SIZE_T CHUNK = 4 * 1024 * 1024;
        std::vector<BYTE> buf;
        SIZE_T remaining = mi.SizeOfImage;
        BYTE*  base      = static_cast<BYTE*>(mi.lpBaseOfDll);
        SIZE_T offset    = 0;

        while (remaining > 0) {
            SIZE_T toRead    = (remaining > CHUNK) ? CHUNK : remaining;
            buf.resize(toRead);
            SIZE_T bytesRead = 0;
            if (!ReadProcessMemory(m_hProcess, base + offset, buf.data(), toRead, &bytesRead)
                || bytesRead == 0) {
                offset    += toRead;
                remaining -= toRead;
                continue;
            }
            void* hit = ScanBuffer(buf.data(), bytesRead, base + offset, pattern, mask);
            if (hit) return hit;
            offset    += toRead;
            remaining -= toRead;
        }
    }
    return nullptr;
}

// -------------------------------------------------------
// FindPatternAllModules
// -------------------------------------------------------
void* LuauScanner::FindPatternAllModules(const std::vector<BYTE>& pattern,
                                          const char*              mask) {
    if (!m_hProcess || m_hProcess == INVALID_HANDLE_VALUE) return nullptr;

    HMODULE hMods[1024];
    DWORD   cbNeeded = 0;
    if (!EnumProcessModules(m_hProcess, hMods, sizeof(hMods), &cbNeeded)) return nullptr;

    DWORD count = cbNeeded / sizeof(HMODULE);
    for (DWORD i = 0; i < count; ++i) {
        MODULEINFO mi{};
        if (!GetModuleInformation(m_hProcess, hMods[i], &mi, sizeof(mi))) continue;
        if (!mi.lpBaseOfDll || mi.SizeOfImage == 0) continue;

        constexpr SIZE_T CHUNK = 4 * 1024 * 1024;
        std::vector<BYTE> buf;
        SIZE_T remaining = mi.SizeOfImage;
        BYTE*  base      = static_cast<BYTE*>(mi.lpBaseOfDll);
        SIZE_T offset    = 0;

        while (remaining > 0) {
            SIZE_T toRead    = (remaining > CHUNK) ? CHUNK : remaining;
            buf.resize(toRead);
            SIZE_T bytesRead = 0;
            if (!ReadProcessMemory(m_hProcess, base + offset, buf.data(), toRead, &bytesRead)
                || bytesRead == 0) {
                offset    += toRead;
                remaining -= toRead;
                continue;
            }
            void* hit = ScanBuffer(buf.data(), bytesRead, base + offset, pattern, mask);
            if (hit) return hit;
            offset    += toRead;
            remaining -= toRead;
        }
    }
    return nullptr;
}

// -------------------------------------------------------
// FindLuaState
// -------------------------------------------------------
void* LuauScanner::FindLuaState() {
    // "Luau" ASCII marker
    std::vector<BYTE> pattern = { 0x4C, 0x75, 0x61, 0x75 };
    const char*       mask    = "xxxx";

    void* hit = FindPatternInModule("RobloxPlayerBeta.exe", pattern, mask);
    if (!hit) hit = FindPatternAllModules(pattern, mask);
    return hit;
}

// -------------------------------------------------------
// ValidateFunctions
// -------------------------------------------------------
bool LuauScanner::ValidateFunctions() {
    return (m_functions.luau_load != nullptr && m_functions.lua_pcall != nullptr);
}

// -------------------------------------------------------
// Scan - main entry point
//
// PATTERNS LEGEND:
//   'x' = exact byte match
//   '?' = wildcard (any byte)
//   mask must be same length as pattern vector
//
// HOW TO UPDATE PATTERNS (x64dbg):
//   1. Attach x64dbg to RobloxPlayerBeta.exe
//   2. Symbols tab → search "luau_load" or "lua_pcall"
//   3. Follow in Disassembler → copy first 20-30 bytes
//   4. Replace unknowns (addresses, offsets) with 0x00 + '?' in mask
// -------------------------------------------------------
bool LuauScanner::Scan() {
    if (!m_hProcess || m_hProcess == INVALID_HANDLE_VALUE) return false;

    char dbg[256];

    // ============================================================
    // PATTERN: luau_load
    // Signature: 40 55 53 56 57 41 54 41 55 41 56 41 57 48 8D 6C 24 C9 48 81
    // Source: Common Roblox x64 build (push-heavy function prologue)
    // ============================================================
    std::vector<BYTE> pat_luau_load = {
        0x40, 0x55, 0x53, 0x56, 0x57, 0x41, 0x54, 0x41, 0x55,
        0x41, 0x56, 0x41, 0x57, 0x48, 0x8D, 0x6C, 0x24, 0xC9, 0x48, 0x81
    };
    const char* mask_luau_load = "xxxxxxxxxxxxxxxxxxxx";

    m_functions.luau_load = FindPatternInModule("RobloxPlayerBeta.exe",
                                                pat_luau_load, mask_luau_load);
    snprintf(dbg, sizeof(dbg), "[LuauScanner] luau_load @ %p", m_functions.luau_load);
    OutputDebugStringA(dbg);

    // ============================================================
    // PATTERN: lua_pcall
    // Signature: 48 89 5C 24 08 57 48 83 EC 30 48 8B D9 48 8B F9
    // ============================================================
    std::vector<BYTE> pat_lua_pcall = {
        0x48, 0x89, 0x5C, 0x24, 0x08, 0x57, 0x48, 0x83,
        0xEC, 0x30, 0x48, 0x8B, 0xD9, 0x48, 0x8B, 0xF9
    };
    const char* mask_lua_pcall = "xxxxxxxxxxxxxxxx";

    m_functions.lua_pcall = FindPatternInModule("RobloxPlayerBeta.exe",
                                                pat_lua_pcall, mask_lua_pcall);
    snprintf(dbg, sizeof(dbg), "[LuauScanner] lua_pcall @ %p", m_functions.lua_pcall);
    OutputDebugStringA(dbg);

    // ============================================================
    // PATTERN: lua_tolstring
    // Signature: 48 89 5C 24 08 48 89 74 24 10 57 48 83 EC 20 48 8B DA
    // ============================================================
    std::vector<BYTE> pat_lua_tolstring = {
        0x48, 0x89, 0x5C, 0x24, 0x08, 0x48, 0x89, 0x74, 0x24, 0x10,
        0x57, 0x48, 0x83, 0xEC, 0x20, 0x48, 0x8B, 0xDA
    };
    const char* mask_lua_tolstring = "xxxxxxxxxxxxxxxxxx";

    m_functions.lua_tolstring = FindPatternInModule("RobloxPlayerBeta.exe",
                                                    pat_lua_tolstring, mask_lua_tolstring);
    snprintf(dbg, sizeof(dbg), "[LuauScanner] lua_tolstring @ %p", m_functions.lua_tolstring);
    OutputDebugStringA(dbg);

    // ============================================================
    // PATTERN: lua_newthread
    // Signature: 48 89 5C 24 10 57 48 83 EC 20 48 8B F9 E8 ?? ?? ?? ??
    // ============================================================
    std::vector<BYTE> pat_lua_newthread = {
        0x48, 0x89, 0x5C, 0x24, 0x10, 0x57, 0x48, 0x83,
        0xEC, 0x20, 0x48, 0x8B, 0xF9, 0xE8, 0x00, 0x00, 0x00, 0x00
    };
    const char* mask_lua_newthread = "xxxxxxxxxxxxxx????";

    m_functions.lua_newthread = FindPatternInModule("RobloxPlayerBeta.exe",
                                                    pat_lua_newthread, mask_lua_newthread);
    snprintf(dbg, sizeof(dbg), "[LuauScanner] lua_newthread @ %p", m_functions.lua_newthread);
    OutputDebugStringA(dbg);

    // ============================================================
    // lua_State pointer
    // ============================================================
    m_functions.lua_State_ptr = FindLuaState();
    snprintf(dbg, sizeof(dbg), "[LuauScanner] lua_State marker @ %p", m_functions.lua_State_ptr);
    OutputDebugStringA(dbg);

    // ============================================================
    // Validate & report
    // ============================================================
    m_functions.valid = ValidateFunctions();

    if (m_functions.valid) {
        OutputDebugStringA("[LuauScanner] SUCCESS: luau_load and lua_pcall found!");
    } else {
        OutputDebugStringA("[LuauScanner] WARNING: One or more functions not found.");
        OutputDebugStringA("[LuauScanner] Update patterns using x64dbg on current Roblox build.");
    }

    return m_functions.valid;
}
