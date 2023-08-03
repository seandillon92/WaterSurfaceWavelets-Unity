using ProfileBuffer;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WaveGrid
{


    internal class WaveGridGPU : IWaveGrid
    {
        private List<ProfileBufferGPU> m_profileBuffers;

        private Settings m_settings;
        private float m_dz;

        private WaveGridGPUMesh m_mesh;
        private WaveGridGPUEnvironment m_environment;
        private WaveGridGPUAdvection m_advection;

        private Material m_material;

        private int m_cameraPos_id;
        private int m_cameraProjectionForward_id;
        private int m_cameraInverseProjection_id;
        private Camera m_camera;

        Mesh IWaveGrid.Mesh => m_mesh.mesh;

        internal WaveGridGPU(Settings settings, Material mat)
        {
            m_camera = settings.camera;
            //Create Profile buffers
            m_dz = (settings.max_zeta - settings.min_zeta)/settings.n_zeta;
            m_settings = settings;
            m_profileBuffers = new List<ProfileBufferGPU>();
            for (int izeta = 0; izeta < settings.n_zeta; izeta++)
            {
                float zeta_min = idxToPosZeta(izeta) - 0.5f * m_dz;
                float zeta_max = idxToPosZeta(izeta) + 0.5f * m_dz;
                m_profileBuffers.Add(new ProfileBufferGPU(zeta_min, zeta_max, new Spectrum(10), settings.initial_time));
            }

            // Create environment and advection
            m_environment = new WaveGridGPUEnvironment(settings);
            m_advection = new WaveGridGPUAdvection(settings, m_environment, m_profileBuffers[0]);

            
            //Create the Mesh
            m_mesh = new WaveGridGPUMesh(settings);

            //Update the material
            m_material = mat;
            m_material.SetFloat(Shader.PropertyToID("waterLevel"), settings.terrain.water_level);
            m_material.SetTexture(Shader.PropertyToID("amplitude"), m_advection.amplitude);
            var size = new Vector2(settings.terrain.size.x, settings.terrain.size.y);
            m_material.SetVector(Shader.PropertyToID("xmin"), -size);
            var idx = new Vector2(1f/(size.x * 2f / settings.n_x),1f/ (size.x * 2f / settings.n_x));
            m_material.SetVector(Shader.PropertyToID("dx"), idx);
            m_material.SetTexture(Shader.PropertyToID("textureData"), m_profileBuffers[0].data);
            m_material.SetFloat(Shader.PropertyToID("profilePeriod"), m_profileBuffers[0].period);
            m_material.SetFloat(Shader.PropertyToID("nx"), settings.n_x);
            
            var terrainPosition = settings.terrain.transform.GetPosition();
            var terrainTranslationXZ = new Vector2(terrainPosition.x, terrainPosition.z);
            m_material.SetVector(Shader.PropertyToID("translation"), terrainTranslationXZ);

            m_material.SetFloatArray("defaultAmplitude", settings.defaultAmplitude);

            m_cameraPos_id = Shader.PropertyToID("cameraPos");
            m_cameraProjectionForward_id = Shader.PropertyToID("cameraProjectionForward");
            m_cameraInverseProjection_id = Shader.PropertyToID("cameraInverseProjection");
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
            m_material.SetFloatArray("directions", settings.amplitudeTracks.IncludeValues.Select(x => x ? 1.0f : 0.0f).ToArray());
            m_material.SetVector(m_cameraPos_id, m_camera.transform.position);
            m_material.SetVector(m_cameraProjectionForward_id, m_camera.projectionMatrix.MultiplyPoint(Vector3.forward));
            m_material.SetMatrix(m_cameraInverseProjection_id,
                m_camera.transform.localToWorldMatrix * Matrix4x4.Scale(new Vector3(1.1f, 1.1f, 1f)) * m_camera.projectionMatrix.inverse);
            m_material.SetFloat("amp_mult", settings.amplitudeMultiplier);

            if (settings.updateSimulation)
            {
                //Advection step
                m_advection.Update();
                m_profileBuffers[0].Update();

            }

            //Precompute step
        }
    }
}
