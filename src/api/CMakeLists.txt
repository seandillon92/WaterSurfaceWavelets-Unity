# Build API lib
add_library(WaterSurfaceWavelets SHARED API.cpp)
target_link_libraries(WaterSurfaceWavelets WaveGrid)

add_custom_command(
	TARGET WaterSurfaceWavelets POST_BUILD        
    	COMMAND ${CMAKE_COMMAND} -E copy_if_different
	$<TARGET_FILE:WaterSurfaceWavelets>
        "${PROJECT_SOURCE_DIR}/UnityPackage/Runtime/Plugins/x86_64/watersurfacewavelets.dll")


