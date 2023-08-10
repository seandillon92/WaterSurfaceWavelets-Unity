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
        private WaveGridGPUAdvection m_advection;

        private Material m_material;

        private int m_cameraPos_id;
        private int m_cameraProjectionForward_id;
        private int m_cameraInverseProjection_id;
        private Camera m_camera;

        Mesh IWaveGrid.Mesh => m_mesh.mesh;

        internal WaveGridGPU(Settings s, Material mat)
        {
            m_camera = s.visualization.camera;
            //Create Profile buffers
            m_dz = (s.simulation.max_zeta - s.simulation.min_zeta)/s.simulation.n_zeta;
            m_settings = s;
            m_profileBuffers = new List<ProfileBufferGPU>();
            for (int izeta = 0; izeta < s.simulation.n_zeta; izeta++)
            {
                float zeta_min = idxToPosZeta(izeta) - 0.5f * m_dz;
                float zeta_max = idxToPosZeta(izeta) + 0.5f * m_dz;
                m_profileBuffers.Add(
                    new ProfileBufferGPU(
                        zeta_min, 
                        zeta_max, 
                        new Spectrum(s.simulation.wind_speed), s.simulation.initial_time));
            }

            // Create environment and advection
            m_advection = new WaveGridGPUAdvection(s, m_profileBuffers[0]);

            
            //Create the Mesh
            m_mesh = new WaveGridGPUMesh(s);

            //Update the material
            m_material = mat;
            m_material.SetFloat(Shader.PropertyToID("waterLevel"), s.environment.water_level);
            m_material.SetTexture(Shader.PropertyToID("amplitude"), m_advection.amplitude);
            var size = new Vector2(s.environment.size.x, s.environment.size.y);
            m_material.SetVector(Shader.PropertyToID("xmin"), -size);
            var idx = new Vector2(1f/(size.x * 2f / s.simulation.n_x),1f/ (size.x * 2f / s.simulation.n_x));
            m_material.SetVector(Shader.PropertyToID("dx"), idx);
            m_material.SetTexture(Shader.PropertyToID("textureData"), m_profileBuffers[0].data);
            m_material.SetFloat(Shader.PropertyToID("profilePeriod"), m_profileBuffers[0].period);
            m_material.SetFloat(Shader.PropertyToID("nx"), s.simulation.n_x);
            var t = s.environment.transform;
            var p = t.GetPosition();
            Matrix4x4 m = 
                Matrix4x4.Translate(new Vector3(p.x, 0, p.z)) *
                Matrix4x4.Rotate(Quaternion.Euler(0, t.rotation.eulerAngles.y, 0));

            m_material.SetMatrix("env_trans", m);
            m_material.SetMatrix("env_trans_inv", m.inverse);

            m_material.SetFloat("env_rotation", t.rotation.eulerAngles.y * Mathf.Deg2Rad);

            m_material.SetFloatArray(
                "defaultAmplitude", 
                s.simulation.GetDefaultAmplitudes(s.environment.transform));

            m_cameraPos_id = Shader.PropertyToID("cameraPos");
            m_cameraProjectionForward_id = Shader.PropertyToID("cameraProjectionForward");
            m_cameraInverseProjection_id = Shader.PropertyToID("cameraInverseProjection");
        }

        private float idxToPosZeta(int idx)
        {
            return m_settings.simulation.min_zeta + (idx + 0.5f) * m_dz;
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
            m_material.SetVector(m_cameraPos_id, m_camera.transform.position);
            m_material.SetVector(m_cameraProjectionForward_id, m_camera.projectionMatrix.MultiplyPoint(Vector3.forward));
            m_material.SetMatrix(m_cameraInverseProjection_id,
                m_camera.transform.localToWorldMatrix * Matrix4x4.Scale(new Vector3(1.1f, 1.1f, 1f)) * m_camera.projectionMatrix.inverse);
            m_material.SetFloat("amp_mult", settings.amplitudeMultiplier);
            m_material.SetFloat("renderOutsideBorders", settings.renderOutsideBorders ? 1.0f : 0.0f);

            if (settings.updateSimulation)
            {
                //Advection step
                m_advection.Update();
            }

            //Precompute step
            m_profileBuffers[0].Update();
            
        }
    }
}
