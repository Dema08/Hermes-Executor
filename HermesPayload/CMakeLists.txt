cmake_minimum_required(VERSION 3.28)
project(HermesPayload)

set(CMAKE_CXX_STANDARD 20)

add_library(HermesPayload SHARED Payload.cpp)

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${CMAKE_SOURCE_DIR}/../HermesUI/bin/Release/net9.0-windows/")
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY "${CMAKE_SOURCE_DIR}/../HermesUI/bin/Release/net9.0-windows/")
