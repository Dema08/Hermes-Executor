#pragma once
#include <cstdint>

namespace Offsets {
    constexpr uint64_t kPageHash = 0x84B3A57D90E73527;
    constexpr uint16_t SCF_INSERTED_JMP = 0x04EB;
    constexpr uint32_t SCF_END_MARKER = 0xF4CC02EB;
    constexpr uint64_t kPageMask = 0xfffffffffffff000;
    constexpr uint8_t kPageShift = 0xc;
    
    constexpr uint64_t Offset_InsertSet = 0xC43D00;
    constexpr uint64_t Offset_WhitelistedPages = 0x29C758;
    constexpr uint64_t Offset_CheckIntegrity = 0x1A2B3C;
    constexpr uint64_t Offset_CFG_Check = 0x4D5E6F;
}
