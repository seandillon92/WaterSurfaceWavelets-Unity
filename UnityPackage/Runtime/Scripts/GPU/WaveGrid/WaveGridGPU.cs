using ProfileBuffer;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

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
        private int m_boat_trans_id;
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
            m_material.SetTexture(Shader.PropertyToID("boat"), m_settings.boat.heights);

            var boatMesh = m_settings.boat.boat.GetComponent<MeshFilter>().sharedMesh.bounds;
            m_material.SetVector(Shader.PropertyToID("boat_size"), boatMesh.size);
            m_boat_trans_id = Shader.PropertyToID("boat_trans");

            m_material.SetTexture("unity_SpecCube0", m_settings.reflection.texture);

            var size = new Vector2(s.environment.size.x, s.environment.size.y);
            m_material.SetVector(Shader.PropertyToID("xmin"), -size);
            var idx = new Vector2(1f/(size.x * 2f / s.simulation.GetResolution()),1f/ (size.y * 2f / s.simulation.GetResolution()));
            m_material.SetVector(Shader.PropertyToID("dx"), idx);
            m_material.SetTexture(Shader.PropertyToID("textureData"), m_profileBuffers[0].data);
            m_material.SetFloat(Shader.PropertyToID("profilePeriod"), m_profileBuffers[0].period);
            m_material.SetVector(Shader.PropertyToID("nx"), new Vector2(s.simulation.GetResolution(), s.simulation.GetResolution()));
            var t = s.environment.transform;
            var p = t.GetPosition();
            Matrix4x4 m = 
                Matrix4x4.Translate(new Vector3(p.x, 0, p.z)) *
                Matrix4x4.Rotate(Quaternion.Euler(0, t.rotation.eulerAngles.y, 0));

            m_material.SetMatrix("env_trans", m);
            m_material.SetMatrix("env_trans_inv", m.inverse);
            m_material.SetVector("env_size", new Vector2(s.environment.size.x, s.environment.size.y));

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

            m_material.SetMatrix(Shader.PropertyToID("boat_trans"), m_settings.boat.boat.transform.worldToLocalMatrix);

            m_material.SetTexture("_Reflection", m_settings.reflection.texture);

            if (settings.updateSimulation)
            {
                //Advection step
                m_advection.Update();
            }

            //Precompute step
            m_profileBuffers[0].Update();
            
        }

        void IWaveGrid.AddPointDisturbance(Vector3 pos, float value)
        {
            // convert from world to local space
            var terrainPos = m_settings.environment.transform.GetPosition();
            var terrainRot = m_settings.environment.transform.rotation;

            Matrix4x4 terrainTR = Matrix4x4.Translate(terrainPos) * Matrix4x4.Rotate(terrainRot);
            var inverse = terrainTR.inverse;
            var worldPos = new Vector3(pos.x, 0, pos.y);
            var localPos = inverse.MultiplyPoint(worldPos);
            localPos.x += m_settings.environment.size.x;
            localPos.z += m_settings.environment.size.y;

            pos.x = localPos.x/(m_settings.environment.size.x * 2f);
            pos.y = localPos.z / (m_settings.environment.size.y * 2f);

            pos.z += terrainRot.eulerAngles.y;
            pos.z = (pos.z % 360 + 360) % 360;
            pos.z /= 360f;
            

            Assert.IsTrue(pos.x >= 0 && pos.x <= 1);
            Assert.IsTrue(pos.y >= 0 && pos.y <= 1);
            Assert.IsTrue(pos.z >= 0 && pos.z <= 1);

            m_advection.IncreaseAmplitude(value, pos);
        }
    }
}
