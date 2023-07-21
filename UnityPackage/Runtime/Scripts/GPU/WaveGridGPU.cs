using ProfileBuffer;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaveGrid
{
    internal class WaveGridGPU : IWaveGrid
    {
        private List<ProfileBufferGPU> m_profileBuffers;

        private Settings m_settings;
        private float m_dz;

        private RenderTexture m_amplitude;
        private RenderTexture m_newAmplitude;
        private Texture2D m_environment;

        private WaveGridGPUMesh m_mesh;

        private ComputeShader m_shader;
        private int m_advection_kernel;
        private int m_init_kernel;
        private int m_diffusion_kernel;
        private int m_copy_kernel;
        private Material m_material;

        private int m_cameraPos_id;
        private int m_cameraProjectionForward_id;
        private int m_cameraInverseProjection_id;
        private int m_deltaTime_id;
        private Camera m_camera;


        internal WaveGridGPU(Settings settings, Material mat, MeshFilter filter)
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

            // Create amplitude render textures
            RenderTextureDescriptor renderTextureDescriptor = new RenderTextureDescriptor();
            renderTextureDescriptor.useMipMap = false;
            renderTextureDescriptor.width = settings.n_x + 2;
            renderTextureDescriptor.height = settings.n_x + 2;
            renderTextureDescriptor.volumeDepth = 16;
            renderTextureDescriptor.enableRandomWrite = true;
            renderTextureDescriptor.colorFormat = RenderTextureFormat.RFloat;
            renderTextureDescriptor.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            renderTextureDescriptor.msaaSamples = 1;
            
            m_amplitude = new RenderTexture(renderTextureDescriptor);
            m_amplitude.wrapMode = TextureWrapMode.Clamp;
            m_amplitude.filterMode = FilterMode.Bilinear;
            m_amplitude.name = "AmplitudeTexture";
            if (!m_amplitude.Create())
            {
                Debug.LogError("Could not create amplitude texture");
            }
            
            m_newAmplitude = new RenderTexture(renderTextureDescriptor);
            m_newAmplitude.wrapMode = TextureWrapMode.Clamp;
            m_newAmplitude.filterMode = FilterMode .Bilinear;
            m_newAmplitude.name = "NewAmplitudeTexture";
            if (!m_newAmplitude.Create())
            {
                Debug.LogError("Could not create new amplitude texture");
            }

            //Create shader
            m_shader = (ComputeShader)Resources.Load("Advection");
            m_shader.SetFloat("groupSpeed", m_profileBuffers[0].groupSpeed);

            m_init_kernel = m_shader.FindKernel("SetDefaulAmplitude");
            
            m_advection_kernel = m_shader.FindKernel("Advection");
            m_shader.SetTexture(m_advection_kernel, "Read", m_amplitude);
            m_shader.SetTexture(m_advection_kernel, "Write", m_newAmplitude);

            m_copy_kernel = m_shader.FindKernel("Copy");
            m_shader.SetTexture(m_copy_kernel, "Read", m_newAmplitude);
            m_shader.SetTexture(m_copy_kernel, "Write", m_amplitude);

            //Set the default amplitude
            SetDefaultAmplitudes();

            //Create the environment texture
            m_environment = 
                new Texture2D(settings.terrain.size.x, settings.terrain.size.y, TextureFormat.RFloat, false, true);
            m_environment.SetPixelData(settings.terrain.heights, 0);
            m_environment.Apply();

            //Create the Mesh
            m_mesh = new WaveGridGPUMesh(settings);
            filter.sharedMesh = m_mesh.mesh;

            //Update the material
            m_material = mat;
            m_material.SetFloat(Shader.PropertyToID("waterLevel"), settings.terrain.water_level);
            m_material.SetTexture(Shader.PropertyToID("amplitude"), m_amplitude);
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

            m_cameraPos_id = Shader.PropertyToID("cameraPos");
            m_cameraProjectionForward_id = Shader.PropertyToID("cameraProjectionForward");
            m_cameraInverseProjection_id = Shader.PropertyToID("cameraInverseProjection");
            m_deltaTime_id = Shader.PropertyToID("deltaTime");
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

        void SetFloats(ComputeShader shader, string id,  float[] f)
        {
            var v = new float[f.Length * 4];
            for(int i = 0 ; i < f.Length; i++)
            {
                v[i * 4] = f[i];
            }
            shader.SetFloats(id, v);
        }

        void IWaveGrid.Update(UpdateSettings settings)
        {

            m_material.SetVector(m_cameraPos_id, m_camera.transform.position);
            m_material.SetVector(m_cameraProjectionForward_id, m_camera.projectionMatrix.MultiplyPoint(Vector3.forward));
            m_material.SetMatrix(m_cameraInverseProjection_id,
                m_camera.transform.localToWorldMatrix * m_camera.projectionMatrix.inverse);

            m_shader.SetFloat(m_deltaTime_id, Time.deltaTime);

            if (settings.updateSimulation)
            {
                //Advection step
                AdvectionStep();
                //Diffusion step //TODO
            }

            //Precompute step
            m_profileBuffers[0].Update();
        }

        void SetDefaultAmplitudes()
        {
            float[] defaultValues = { 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f};
            m_shader.SetTexture(m_init_kernel, "Write", m_amplitude);
            SetFloats(m_shader, "Default", defaultValues);
            m_shader.GetKernelThreadGroupSizes(m_init_kernel, out uint x, out uint y,out uint z);
            m_shader.Dispatch(m_init_kernel, (int)((m_settings.n_x + 2) / x), (int)((m_settings.n_x + 2) / y), (int)(16 / z));
            
            m_shader.SetTexture(m_init_kernel, "Write", m_newAmplitude);
            m_shader.Dispatch(m_init_kernel, (int)((m_settings.n_x + 2) / x), (int)((m_settings.n_x + 2) / y), (int)(16 / z));
        }

        void AdvectionStep()
        {
            
            {
                m_shader.GetKernelThreadGroupSizes(m_advection_kernel, out uint x, out uint y, out uint z);
                m_shader.Dispatch(m_advection_kernel, (int)(m_settings.n_x/x), (int)(m_settings.n_x/y), (int)(16 / z));
            }


            {
                m_shader.GetKernelThreadGroupSizes(m_copy_kernel, out uint x, out uint y, out uint z);
                m_shader.Dispatch(m_copy_kernel, (int)(m_settings.n_x / x), (int)(m_settings.n_x / y), (int)(16 / z));
            }
        }       
    }
}
