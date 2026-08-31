cmake_minimum_required(VERSION 3.28)
project(HermesCore VERSION 1.0.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Set output directory ke folder HermesUI (agar mudah integrasi)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${CMAKE_SOURCE_DIR}/../HermesUI/bin/Release/net9.0-windows/")
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY "${CMAKE_SOURCE_DIR}/../HermesUI/bin/Release/net9.0-windows/")

# Tambahkan semua file .cpp
file(GLOB SOURCES "*.cpp" "Injector/*.cpp" "Executor/*.cpp" "Offsets/*.cpp")
file(GLOB HEADERS "*.h" "Injector/*.h" "Executor/*.h" "Offsets/*.h")

# Buat shared library (DLL)
add_library(HermesCore SHARED ${SOURCES} ${HEADERS})

# Link ke Windows libraries
target_link_libraries(HermesCore 
    PRIVATE
    kernel32
    user32
    ntdll
    advapi32
    psapi
)

# Enable optimasi untuk release build
target_compile_definitions(HermesCore PRIVATE _CRT_SECURE_NO_WARNINGS)
