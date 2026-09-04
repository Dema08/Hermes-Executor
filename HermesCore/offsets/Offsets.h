#pragma once
#include <cstdint>

namespace Offsets {
    // === BASE ADDRESS (dari dumper) ===
    constexpr uint64_t ROBLOX_BASE = 0x7ff66a9f0000;

    // === LUAU FUNCTIONS ===
    constexpr uint64_t luau_load     = 0x79e2b0;   // [static]
    constexpr uint64_t lua_pcall     = 0x86f420;   // [static]
    // constexpr uint64_t luau_compile = 0x????????; // NOT FOUND
    constexpr uint64_t lua_State     = 0x2d9bd50;  // [static]

    // === ROBOX SCRIPT CONTEXT ===
    constexpr uint64_t ScriptContext = 0x41784a0;  // [static]
    // constexpr uint64_t ScriptContextResume = 0x????????; // NOT FOUND

    // === HYPERION BYPASS ===
    constexpr uint64_t Offset_InsertSet        = 0x5974800;  // [static]
    constexpr uint64_t Offset_WhitelistedPages = 0x6d8a286;  // [dynamic]
    constexpr uint64_t Offset_CFG_Check        = 0x5f29cb8;  // [dynamic]
    constexpr uint64_t Offset_CFG_Dispatch     = 0x0;        // Not found

    // === PAGE HASH ===
    constexpr uint64_t kPageHash = 0x84B3A57D90E73527;
    constexpr uint64_t kPageMask = 0xfffffffffffff000;
    constexpr uint8_t  kPageShift = 0xc;

    // === LUAU STRING REFERENCES ===
    constexpr uint64_t LuauTelemetry = 0x80480F8;
    constexpr uint64_t LuauGc = 0x6E413B4;
    constexpr uint64_t LuauFastpcall = 0x6CF3BD0;
    constexpr uint64_t pcall = 0x6CF3C90;
    constexpr uint64_t xpcall = 0x6CF3C98;
    constexpr uint64_t isUrlWhitelisted = 0x6C4A168;
}
