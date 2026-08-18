cmake_minimum_required(VERSION 3.14)
project(FosterPlatform C)

# Platform Variables
option(FOSTER_OPENGL_ENABLED "Make OpenGL Renderer available" ON)
option(FOSTER_OPENGLES "Build the OpenGL Renderer against OpenGL ES 3.0" OFF)
option(FOSTER_LTO "Enable Link Time Optimization (Release)" OFF)
if (WIN32)
	option(FOSTER_D3D11_ENABLED "Make D3D11 Renderer available" ON)
endif()

# Set flag for building a universal binary on macOS 
if(APPLE)
	set(CMAKE_OSX_ARCHITECTURES "arm64;x86_64")
endif()

# Default to Release build for library
if(NOT CMAKE_BUILD_TYPE)
	set(CMAKE_BUILD_TYPE Release)
endif()

# Define target name
set (TARGET_NAME FosterPlatform)

add_library(${TARGET_NAME} SHARED
	include/foster_platform.h
	src/foster_platform.c
	src/foster_image.c
	src/foster_renderer.c
	src/foster_renderer_d3d11.c
	src/foster_renderer_opengl.c
)

target_include_directories(${TARGET_NAME}
	PUBLIC $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
	PRIVATE $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/src>
)

# Get platform target
if (APPLE)
	set(FosterTarget osx)
elseif (WIN32)
	set(FosterTarget x64)
elseif (UNIX)
	if(CMAKE_SYSTEM_PROCESSOR STREQUAL "aarch64")
		set(FosterTarget libarm64)
	elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "armv7l")
		set(FosterTarget libarm)
	elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "armv6l")
		set(FosterTarget libarm)
	else()
		set(FosterTarget lib64)
	endif()
else()
	message(FATAL_ERROR "Unknown Target!")
endif()

# Output libs to platform-specific library
set_target_properties(${TARGET_NAME}
	PROPERTIES
	ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
	ARCHIVE_OUTPUT_DIRECTORY_RELEASE "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
	LIBRARY_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
	LIBRARY_OUTPUT_DIRECTORY_RELEASE "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
	RUNTIME_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
	RUNTIME_OUTPUT_DIRECTORY_RELEASE "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
)

if (MSVC)
    # Set warning level 3
    target_compile_options(${TARGET_NAME} PRIVATE /W3)

    # Warnings as Errors Enabled
    target_compile_options(${TARGET_NAME} PRIVATE /WX)
endif()

if(WIN32)
    target_compile_definitions(${TARGET_NAME} PRIVATE _UNICODE UNICODE _CRT_SECURE_NO_WARNINGS)
    target_compile_definitions(${TARGET_NAME} PRIVATE NOMINMAX)
endif ()

# tracks which libraries we need to link, depends on Options above
set(LIBS "")

# use the OpenGL Renderer Backend
if (FOSTER_OPENGL_ENABLED)
	target_compile_definitions(${TARGET_NAME} PRIVATE FOSTER_OPENGL_ENABLED)
endif()

# use OpenGL ES instead of Desktop OpenGL
if (FOSTER_OPENGLES)
	target_compile_definitions(${TARGET_NAME} PRIVATE FOSTER_OPENGLES)
endif()

# link time optimization for Release builds
if (FOSTER_LTO)
	set_property(TARGET ${TARGET_NAME} PROPERTY INTERPROCEDURAL_OPTIMIZATION_RELEASE TRUE)
endif()

# When using FetchContent, trim SDL2 to only the subsystems we use.
# (Foster handles input/window via SDL2; audio is handled by FMOD.)
set(SDL_AUDIO OFF CACHE BOOL "" FORCE)
set(SDL_HAPTIC OFF CACHE BOOL "" FORCE)
set(SDL_SENSOR OFF CACHE BOOL "" FORCE)
set(SDL_POWER OFF CACHE BOOL "" FORCE)

# use the D3D11 Renderer Backend
if (FOSTER_D3D11_ENABLED)
	target_compile_definitions(${TARGET_NAME} PRIVATE FOSTER_D3D11_ENABLED)
	set(LIBS ${LIBS} d3d11.lib dxguid.lib D3Dcompiler.lib)
endif()

# Emscripten can import SDL2 directly
if (EMSCRIPTEN)
	
	set_target_properties(${TARGET_NAME} PROPERTIES COMPILE_FLAGS "-s USE_SDL=2")
	target_link_libraries(${TARGET_NAME} "-s USE_SDL=2 -s USE_WEBGL2=1")

# Pull SDL2 from its Github repo
else()
	if (NOT DEFINED FOSTER_SDL2_LIBS)
		# prefer an externally installed SDL2 (via pkg-config)
		find_package(PkgConfig QUIET)
		if (PKG_CONFIG_FOUND)
			pkg_check_modules(SDL2_PC IMPORTED_TARGET sdl2)
		endif()

		if (TARGET PkgConfig::SDL2_PC)
			set(FOSTER_SDL2_LIBS PkgConfig::SDL2_PC)
			set(FOSTER_SDL2_INCLUDE "")
		else()
			include(FetchContent)
			set(FETCHCONTENT_QUIET FALSE)
			set(SDL_TEST_ENABLED_BY_DEFAULT OFF)
			set(SDL_SHARED_ENABLED_BY_DEFAULT OFF)
			set(SDL_STATIC_ENABLED_BY_DEFAULT ON)

			FetchContent_Declare(
				SDL2
				GIT_REPOSITORY https://github.com/libsdl-org/SDL
				GIT_TAG release-2.30.0
				GIT_PROGRESS TRUE
			)
			FetchContent_MakeAvailable(SDL2)
			set(FOSTER_SDL2_LIBS SDL2-static)
			set(FOSTER_SDL2_INCLUDE ${sdl2_SOURCE_DIRS}/include)
		endif()
	endif()

	# statically link SDL2 if we're building it ourselves
	set(LIBS ${LIBS} ${FOSTER_SDL2_LIBS})
	if (FOSTER_SDL2_INCLUDE)
		target_include_directories(${TARGET_NAME} PRIVATE ${FOSTER_SDL2_INCLUDE})
	endif()
endif()

# Tell SDL it's going to be a shared lib (only applies to SDL2 builds we made ourselves)
if (TARGET ${FOSTER_SDL2_LIBS} AND NOT TARGET PkgConfig::SDL2_PC)
	set_property(TARGET ${FOSTER_SDL2_LIBS} PROPERTY POSITION_INDEPENDENT_CODE ON)
endif()

# Link SDL
target_link_libraries(${TARGET_NAME} PRIVATE ${LIBS})
