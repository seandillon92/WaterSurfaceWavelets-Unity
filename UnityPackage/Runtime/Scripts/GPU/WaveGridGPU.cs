using ProfileBuffer;
using System;
using System.Collections.Generic;

namespace WaveGrid
{
    internal class WaveGridGPU : IWaveGrid
    {
        private List<ProfileBufferGPU> m_profileBuffers;
        private Settings m_settings;
        private float m_dz;
        WaveGridGPU(Settings settings)
        {
            m_dz = (settings.max_zeta - settings.min_zeta)/settings.n_zeta;
            m_settings = settings;
            m_profileBuffers = new List<ProfileBufferGPU>();
            for (int izeta = 0; izeta < settings.n_zeta; izeta++)
            {
                float zeta_min = idxToPosZeta(izeta) - 0.5f * m_dz;
                float zeta_max = idxToPosZeta(izeta) + 0.5f * m_dz;
                m_profileBuffers.Add(new ProfileBufferGPU(zeta_min, zeta_max, new Spectrum(10)));
            }
        }

        private float idxToPosZeta(int idx)
        {
            return m_settings.min_zeta + (idx + 0.5f) * m_dz;
        }

        void IDisposable.Dispose()
        {
            for (int i = 0; i < m_profileBuffers.Count; i++)
            {
                m_profileBuffers[i].Dispose();
            }

            m_profileBuffers.Clear();
        }

        void IWaveGrid.Update(UpdateSettings settings)
        {
            //Advection step //TODO
            //Diffusion step //TODO

            //Precompute step
            m_profileBuffers[0].Update();
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
