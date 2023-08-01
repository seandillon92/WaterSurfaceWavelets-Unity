#ifdef _WIN32
#define API extern "C" __declspec(dllexport)
#else
#define API extern "C" 
#endif

#include "../WaveGrid.h"
#include "../ProfileBuffer.h"

#pragma region Grid
API WaterWavelets::WaveGrid* createGrid(
    Real size,
    Real max_zeta,
    Real min_zeta,
    int n_x,
    int n_theta,
    int n_zeta,
    Real initial_time,
    int spectumType,
    void* terrain,
    size_t terrain_size,
    void* defaultAmplitude) {


    auto settings = WaterWavelets::WaveGrid::Settings{
        size,
        max_zeta,
        min_zeta,
        n_x,
        n_theta,
        n_zeta,
        initial_time,
        static_cast<WaterWavelets::WaveGrid::Settings::SpectrumType>(spectumType),
        static_cast<float*>(terrain),
        terrain_size,
        static_cast<float*>(defaultAmplitude)
    };

	return new WaterWavelets::WaveGrid(settings);
}

API void destroyGrid(WaterWavelets::WaveGrid* grid) {
    delete grid;
}

API void timeStep(WaterWavelets::WaveGrid* grid, Real dt, bool fullUpdate) {
    grid->timeStep(dt, fullUpdate);
}

API Real clfTimeStep(WaterWavelets::WaveGrid* grid) {
    return grid->cflTimeStep();
}

API size_t profileBuffersSize(WaterWavelets::WaveGrid* grid) {
    return grid->m_profileBuffers.size();
}

API Real idxToPos(WaterWavelets::WaveGrid* grid, int idx, int dim) {
    return grid->idxToPos(idx, dim);
}

API Real levelSet(WaterWavelets::WaveGrid* grid, Vec2 pos) {
    return grid->m_enviroment.levelset(pos);
}

API Real amplitude(WaterWavelets::WaveGrid* grid, Vec4 pos4){
    return grid->amplitude(pos4);
}

API void addPointDisturbance(WaterWavelets::WaveGrid* grid, Vec2 pos, Real disturbance) {
    grid->addPointDisturbance(pos, disturbance);
}

API void addPointDisturbanceDirection(WaterWavelets::WaveGrid* grid, Vec3 pos, float disturbance) {
    grid->addPointDisturbance(pos, disturbance);
}

API void amplitudeData(WaterWavelets::WaveGrid* buffer, void* dest) {
    memcpy(dest, buffer->m_amplitude.m_data.data(), buffer->m_amplitude.m_data.size() * sizeof(float));
}

#pragma endregion

#pragma region Profile Buffer
API WaterWavelets::ProfileBuffer* getProfileBuffer(WaterWavelets::WaveGrid* grid, int index) {
    return &(grid->m_profileBuffers[index]);
}

API size_t profileBufferDataSize(WaterWavelets::ProfileBuffer* buffer) {
    return buffer->m_data.size();
}

API void copyProfileBufferData(WaterWavelets::ProfileBuffer* buffer, void * dest) {
    memcpy(dest, buffer->m_data.data(), buffer->m_data.size() * sizeof(Vec4));
}

API float profileBufferPeriod(WaterWavelets::ProfileBuffer* buffer) {
    return buffer->m_period;
}

#pragma endregion