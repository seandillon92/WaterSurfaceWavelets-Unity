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
        internal IntPtr Ptr => m_ptr;

        Mesh IWaveGrid.Mesh => m_mesh.mesh;

        private WaveGridCPUMesh m_mesh;
        private WaveGridCPUData m_data;
        private WaveGridCPURenderer m_renderer;
        private int m_visualizationResolution;
        private float m_zeta;
        private Vector2 m_terrain_translation;
        private Vector2 m_terrain_size;
        private float m_waterLevel;
        private Camera m_camera;


        private List<ProfileBufferCPU> m_buffers = new List<ProfileBufferCPU>();
        private float m_timeStep = 0;

        internal WaveGridCPU(Settings settings, Material material = null)
        {
            m_ptr = API.Grid.createGrid(
                settings.terrain.size.x,
                settings.max_zeta,
                settings.min_zeta,
                settings.n_x,
                settings.n_theta,
                settings.n_zeta,
                settings.initial_time,
                settings.spectrum_type,
                settings.terrain.heights,
                settings.terrain.heights.Length);

            var buffersNum = API.Grid.profileBuffersSize(m_ptr);
            for (int i = 0; i < buffersNum; i++)
            {
                var ptr = API.Grid.getProfileBuffer(m_ptr, i);
                m_buffers.Add(new ProfileBufferCPU(ptr));
            }

            m_timeStep = API.Grid.clfTimeStep(m_ptr);

            m_data = new WaveGridCPUData(settings.visualizationResolution);
            m_mesh = new WaveGridCPUMesh(m_data);
            m_renderer = new WaveGridCPURenderer(m_data, material, settings.camera, settings.terrain.water_level);
            m_visualizationResolution = settings.visualizationResolution;
            m_zeta = settings.min_zeta + 0.5f * (settings.max_zeta - settings.min_zeta) / settings.n_zeta;

            var terrainPosition = settings.terrain.transform.GetPosition();

            m_terrain_translation = new Vector2(terrainPosition.x, terrainPosition.z);
            m_terrain_size = settings.terrain.size;
            m_waterLevel = settings.terrain.water_level;

            m_camera = settings.camera;
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

        public void Dispose()
        {
            API.Grid.destroyGrid(m_ptr);
            m_data.Dispose();
        }

        void IWaveGrid.Update(UpdateSettings s)
        {
            m_data.SetVertices(
                this,
                m_visualizationResolution,
                s.amplitudeMultiplier,
                m_camera.transform.localToWorldMatrix * Matrix4x4.Scale(new Vector3(1.1f, 1.1f, 1f)),
                m_camera.projectionMatrix,
                m_terrain_translation,
                m_terrain_size,
                s.direction,
                m_waterLevel,
                s.renderOutsideBorders,
                m_zeta);

            m_data.LoadProfileBufferData(m_buffers[0]);

            m_buffers[0].Update();
            m_mesh.Update();
            m_renderer.Update(s);
            API.Grid.timeStep(m_ptr, s.dt * m_timeStep, s.updateSimulation);
        }
    }
}
