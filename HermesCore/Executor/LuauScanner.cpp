#include "LuauScanner.h"
#include "../Offsets/Offsets.h"
#include <windows.h>
#include <psapi.h>
#include <algorithm>
#include <cstring>

// -------------------------------------------------------
// Constructor
// -------------------------------------------------------
LuauScanner::LuauScanner(HANDLE hProcess) : m_hProcess(hProcess) {}

void* LuauScanner::ScanBuffer(const BYTE* buffer, size_t bufSize, BYTE* moduleBase, const std::vector<BYTE>& pattern, const char* mask) {
    return nullptr;
}

void* LuauScanner::FindPatternInModule(const std::string& moduleName, const std::vector<BYTE>& pattern, const char* mask) {
    return nullptr;
}

void* LuauScanner::FindPatternAllModules(const std::vector<BYTE>& pattern, const char* mask) {
    return nullptr;
}

void* LuauScanner::FindLuaState() {
    return nullptr;
}

bool LuauScanner::ValidateFunctions() {
    return (m_functions.luau_load != nullptr && m_functions.lua_pcall != nullptr);
}

bool LuauScanner::Scan() {
    if (!m_hProcess || m_hProcess == INVALID_HANDLE_VALUE) {
        OutputDebugStringA("[LuauScanner] ERROR: Invalid process handle.\n");
        return false;
    }

    OutputDebugStringA("[LuauScanner] Starting static offset resolution...\n");
    
    uint64_t base = Offsets::ROBLOX_BASE;
    
    m_functions.luau_load = (void*)(base + Offsets::luau_load);
    m_functions.lua_pcall = (void*)(base + Offsets::lua_pcall);
    m_functions.lua_State_ptr = (void*)(base + Offsets::lua_State);
    
    char dbg[256];
    snprintf(dbg, sizeof(dbg), "[LuauScanner] luau_load @ %p\n", m_functions.luau_load);
    OutputDebugStringA(dbg);
    snprintf(dbg, sizeof(dbg), "[LuauScanner] lua_pcall @ %p\n", m_functions.lua_pcall);
    OutputDebugStringA(dbg);
    snprintf(dbg, sizeof(dbg), "[LuauScanner] lua_State @ %p\n", m_functions.lua_State_ptr);
    OutputDebugStringA(dbg);
    
    m_functions.valid = ValidateFunctions();
    
    if (m_functions.valid) {
        OutputDebugStringA("[LuauScanner] ✅ All Luau functions resolved via static offsets!\n");
    } else {
        OutputDebugStringA("[LuauScanner] ❌ Some functions not found!\n");
    }
    
    return m_functions.valid;
}
