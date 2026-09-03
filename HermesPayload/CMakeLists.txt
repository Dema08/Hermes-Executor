cmake_minimum_required(VERSION 3.28)
project(HermesPayload)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Output langsung ke folder UI (hindari subfolder Release\ dari MSBuild)
set(OUTPUT_DIR "${CMAKE_SOURCE_DIR}/../HermesUI/bin/Release/net9.0-windows/")
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${OUTPUT_DIR}")
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY "${OUTPUT_DIR}")
foreach(CONFIG Release Debug RelWithDebInfo MinSizeRel)
    string(TOUPPER ${CONFIG} UCONFIG)
    set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_${UCONFIG} "${OUTPUT_DIR}")
    set(CMAKE_LIBRARY_OUTPUT_DIRECTORY_${UCONFIG} "${OUTPUT_DIR}")
endforeach()

add_library(HermesPayload SHARED HermesPayload.cpp)

target_link_libraries(HermesPayload PRIVATE kernel32 user32)
