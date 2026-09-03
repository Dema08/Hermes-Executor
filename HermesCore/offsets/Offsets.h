#pragma once
#include <cstdint>

namespace Offsets {
    // === BASE ADDRESS (dari dumper) ===
    constexpr uint64_t ROBLOX_BASE = 0x7ff7dc680000;
    
    // === LUAU FUNCTIONS ===
    constexpr uint64_t luau_load = 0x73d900;     // ✅ VALID
    constexpr uint64_t lua_pcall = 0x5928a20;    // ✅ VALID
    constexpr uint64_t lua_State = 0x418cb60;    // ✅ VALID
    
    // === HYPERION BYPASS ===
    constexpr uint64_t Offset_InsertSet = 0x606aea0;       // ✅ VALID
    constexpr uint64_t Offset_WhitelistedPages = 0x6c4d168; // ✅ VALID
    constexpr uint64_t Offset_CFG_Check = 0x66da82a;       // ✅ VALID
    constexpr uint64_t Offset_CFG_Dispatch = 0x0;          // Not found
    
    // === PAGE HASH ===
    constexpr uint64_t kPageHash = 0x84B3A57D90E73527;
    constexpr uint64_t kPageMask = 0xfffffffffffff000;
    constexpr uint8_t kPageShift = 0xc;
    
    // === LUAU STRING REFERENCES ===
    constexpr uint64_t LuauTelemetry = 0x80480F8;
    constexpr uint64_t LuauGc = 0x6E413B4;
    constexpr uint64_t LuauFastpcall = 0x6CF3BD0;
    constexpr uint64_t pcall = 0x6CF3C90;
    constexpr uint64_t xpcall = 0x6CF3C98;
    constexpr uint64_t isUrlWhitelisted = 0x6C4A168;
}
