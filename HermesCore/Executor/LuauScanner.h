#pragma once
#include <windows.h>
#include <vector>
#include <string>

// ==============================================
// Struct yang menyimpan alamat fungsi Luau VM
// ==============================================
struct LuauFunctions {
    // Fungsi inti Luau
    void* luau_load      = nullptr;  // int luau_load(lua_State*, const char*, const char*, size_t, int)
    void* lua_pcall      = nullptr;  // int lua_pcall(lua_State*, int, int, int)
    void* lua_tolstring  = nullptr;  // const char* lua_tolstring(lua_State*, int, size_t*)
    void* lua_getglobal  = nullptr;  // void lua_getglobal(lua_State*, const char*)
    void* lua_setglobal  = nullptr;  // void lua_setglobal(lua_State*, const char*)
    void* lua_newthread  = nullptr;  // lua_State* lua_newthread(lua_State*)
    void* luaL_loadbuffer = nullptr; // int luaL_loadbuffer(lua_State*, const char*, size_t, const char*)

    // Pointer ke global lua_State
    void* lua_State_ptr  = nullptr;

    bool valid = false;
};

// ==============================================
// LuauScanner - Scan memory Roblox untuk Luau VM
// ==============================================
class LuauScanner {
public:
    explicit LuauScanner(HANDLE hProcess);
    ~LuauScanner() = default;

    // Scan memory Roblox untuk semua fungsi Luau
    bool Scan();

    // Akses hasil scan
    LuauFunctions GetFunctions() const { return m_functions; }

private:
    HANDLE        m_hProcess;
    LuauFunctions m_functions;

    // --- Helpers ---

    // Scan semua modul di proses untuk pola byte
    void* FindPatternAllModules(const std::vector<BYTE>& pattern, const char* mask);

    // Scan modul tertentu (case-insensitive nama)
    void* FindPatternInModule(const std::string& moduleName,
                              const std::vector<BYTE>& pattern,
                              const char* mask);

    // Scan satu blok memori yang sudah dibaca
    void* ScanBuffer(const BYTE*  buffer,
                     size_t       bufSize,
                     BYTE*        moduleBase,
                     const std::vector<BYTE>& pattern,
                     const char*  mask);

    // Cari lua_State global
    void* FindLuaState();

    // Pastikan offset fungsi masuk akal
    bool ValidateFunctions();
};
