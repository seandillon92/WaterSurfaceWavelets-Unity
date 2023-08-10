using ProfileBuffer;
using System;
using System.Collections.Generic;
using UnityEngine;
using WaterWaveSurface;

namespace WaveGrid
{
    internal class WaveGridCPU : IWaveGrid
    {
        private IntPtr m_ptr;

        Mesh IWaveGrid.Mesh => m_mesh.mesh;

        private WaveGridCPUMesh m_mesh;
        private WaveGridCPUData m_data;
        private WaveGridCPURenderer m_renderer;
        private int m_visualizationResolution;
        private float m_zeta;
        private Vector2 m_terrain_translation;
        private Vector2 m_terrain_size;
        private float m_waterLevel;

        private List<ProfileBufferCPU> m_buffers = new List<ProfileBufferCPU>();
        private float m_timeStep = 0;

        internal WaveGridCPU(Settings s, Material material = null)
        {
            // Convert height data from render texture to float array
            var env = s.environment;
            Texture2D tex = new Texture2D(env.heights.width, env.heights.height, TextureFormat.RFloat, false);
            RenderTexture.active = env.heights;
            tex.ReadPixels(new Rect(0, 0, env.heights.width, env.heights.height), 0, 0);
            tex.Apply();

            var heightsData = tex.GetPixelData<float>(0).ToArray();
            GameObject.Destroy(tex);

            m_ptr = API.Grid.createGrid(
                s.environment.size.x,
                s.simulation.max_zeta,
                s.simulation.min_zeta,
                s.simulation.n_x,
                s.simulation.n_theta,
                s.simulation.n_zeta,
                s.simulation.initial_time,
                1,
                heightsData,
                heightsData.Length,
                s.simulation.GetDefaultAmplitudes(s.environment.transform).ToArray());

            var buffersNum = API.Grid.profileBuffersSize(m_ptr);
            for (int i = 0; i < buffersNum; i++)
            {
                var ptr = API.Grid.getProfileBuffer(m_ptr, i);
                m_buffers.Add(new ProfileBufferCPU(ptr));
            }

            m_timeStep = API.Grid.clfTimeStep(m_ptr);

            m_data = new WaveGridCPUData(s);
            m_mesh = new WaveGridCPUMesh(m_data);
            m_renderer = new WaveGridCPURenderer(m_data, s, material);
            m_visualizationResolution = s.visualization.resolution;
            m_zeta = 
                s.simulation.min_zeta + 0.5f * (s.simulation.max_zeta - s.simulation.min_zeta) / s.simulation.n_zeta;

            var terrainPosition = s.environment.transform.GetPosition();

            m_terrain_translation = new Vector2(terrainPosition.x, terrainPosition.z);
            m_terrain_size = s.environment.size;
            m_waterLevel = s.environment.water_level;
        }

        internal void AddPointDisturbance(Vector3 pos, float value)
        {
            API.Grid.addPointDisturbanceDirection(m_ptr, pos, value);
        }

        internal void AddPointDisturbance(Vector2 pos, float value)
        {
            API.Grid.addPointDisturbance(m_ptr, pos, value);
        }

        internal float GetTerrainHeight(Vector2 pos)
        {
            return API.Grid.levelSet(m_ptr, pos);
        }

        internal unsafe void GetAmplitudeData(float[] amplitudes)
        {
            fixed (float* array = amplitudes)
            {
                API.Grid.amplitudeData(m_ptr, amplitudes);
            }
        }

        public void Dispose()
        {
            API.Grid.destroyGrid(m_ptr);
        }

        unsafe void IWaveGrid.Update(UpdateSettings s)
        {
            m_data.LoadProfileBufferData(m_buffers[0]);
            m_buffers[0].Update();

            m_data.LoadAmplitudeData(this);

            m_renderer.Update(s);
            API.Grid.timeStep(m_ptr, s.dt * m_timeStep, s.updateSimulation);
        }
    }
}
