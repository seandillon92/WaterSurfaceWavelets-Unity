using System;
using UnityEngine;
using WaterWaveSurface;

namespace WaveGrid
{
    internal class WaveGridGPU : IWaveGrid
    {
        WaveGridGPU(Settings settings)
        {
        }

        void IDisposable.Dispose()
        {
            throw new NotImplementedException();
        }

        void IWaveGrid.Update(UpdateSettings settings)
        {
        }

        /*void IWaveGrid.Update(UpdateSettings s)
        {
            m_data.SetVertices(
                this,
                m_visualizationResolution,
                s.amplitudeMultiplier,
                s.camera.transform.localToWorldMatrix * Matrix4x4.Scale(new Vector3(1.1f, 1.1f, 1f)),
                s.camera.projectionMatrix,
                m_terrain_translation,
                m_terrain_size,
                s.direction,
                m_waterLevel,
                s.renderOutsideBorders,
                m_zeta);

            m_data.LoadProfileBufferData(m_buffers[0]);

            m_buffers[0].Update();
            m_mesh.Update();
            m_renderer.Update();
            API.Grid.timeStep(m_ptr, s.dt * m_timeStep, s.updateSimulation);
        }*/
    }
}
