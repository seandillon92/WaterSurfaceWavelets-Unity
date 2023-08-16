using ProfileBuffer;
using UnityEngine;

namespace WaveGrid
{
    internal class WaveGridGPUAdvection
    {
        internal RenderTexture amplitude { get; private set; }
        internal RenderTexture m_amplitude { get; private set; }
        private RenderTexture m_newAmplitude;

        internal RenderTexture m_manualAmplitude { get; private set; }
        private RenderTexture m_newManualAmplitude;

        private ComputeShader m_shader;
        private int m_advection_kernel;
        private int m_diffusion_kernel;
        private int m_copy_kernel;
        private int m_manualPoint_kernel;
        private int m_dissipation_kernel;
        private int m_updateEdge_kernel;


        private Settings m_settings;
        private int m_deltaTime_id;
        private ProfileBufferGPU m_profileBuffer;

        internal WaveGridGPUAdvection(
            Settings s,
            ProfileBufferGPU profileBuffer)
        {
            m_settings = s;
            m_profileBuffer = profileBuffer;

            // Create amplitude render textures
            RenderTextureDescriptor renderTextureDescriptor = new RenderTextureDescriptor();
            renderTextureDescriptor.useMipMap = false;
            renderTextureDescriptor.width = s.simulation.GetResolution();
            renderTextureDescriptor.height = s.simulation.GetResolution();
            renderTextureDescriptor.volumeDepth = 16;
            renderTextureDescriptor.enableRandomWrite = true;
            renderTextureDescriptor.colorFormat = RenderTextureFormat.RFloat;
            renderTextureDescriptor.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            renderTextureDescriptor.msaaSamples = 1;
            renderTextureDescriptor.sRGB = false;
            renderTextureDescriptor.autoGenerateMips = false;

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
            m_newAmplitude.filterMode = FilterMode.Bilinear;
            m_newAmplitude.name = "NewAmplitudeTexture";
            if (!m_newAmplitude.Create())
            {
                Debug.LogError("Could not create new amplitude texture");
            }

            m_manualAmplitude = new RenderTexture(renderTextureDescriptor);
            m_manualAmplitude.wrapMode = TextureWrapMode.Clamp;
            m_manualAmplitude.filterMode = FilterMode.Bilinear;
            m_manualAmplitude.name = "ManualAmplitudeTexture";
            if (!m_manualAmplitude.Create())
            {
                Debug.LogError("Could not create manual amplitude texture");
            }

            m_newManualAmplitude = new RenderTexture(renderTextureDescriptor);
            m_newManualAmplitude.wrapMode = TextureWrapMode.Clamp;
            m_newManualAmplitude.filterMode = FilterMode.Bilinear;
            m_newManualAmplitude.name = "NewManualAmplitudeTexture";
            if (!m_newManualAmplitude.Create())
            {
                Debug.LogError("Could not create new manual amplitude texture");
            }

            renderTextureDescriptor.width = s.simulation.GetResolution() + 2;
            renderTextureDescriptor.height = s.simulation.GetResolution() + 2;
            amplitude = new RenderTexture(renderTextureDescriptor);
            if (!amplitude.Create())
            {
                Debug.LogError("Could not create amplitude texture");
            }


            //Create shader
            m_shader = (ComputeShader)Resources.Load("Advection");
            m_shader.SetFloat("groupSpeed", profileBuffer.groupSpeed);
            m_shader.SetVector(
                "dx",
                new Vector2(
                    s.environment.size.x * 2f / s.simulation.GetResolution(),
                    s.environment.size.y * 2f / s.simulation.GetResolution()
                ));

            m_shader.SetVector("x_min" , new Vector2(-s.environment.size.x, -s.environment.size.y));
            m_shader.SetVector("env_dx", 
                new Vector2(
                    s.environment.size.x * 2.0f / s.environment.heights.width,
                    s.environment.size.y * 2.0f/ s.environment.heights.height));
            m_shader.SetFloat("dissipation", s.simulation.dissipation);

            m_advection_kernel = m_shader.FindKernel("Advection");
            m_shader.SetTexture(m_advection_kernel, "Read", m_amplitude);
            m_shader.SetTexture(m_advection_kernel, "Write", m_newAmplitude);
            m_shader.SetTexture(m_advection_kernel, "heights", s.environment.heights);
            m_shader.SetTexture(m_advection_kernel, "gradients", s.environment.gradients);

            m_diffusion_kernel = m_shader.FindKernel("Diffusion");
            m_shader.SetTexture(m_diffusion_kernel, "Read", m_newAmplitude);
            m_shader.SetTexture(m_diffusion_kernel, "Write", m_amplitude);
            m_shader.SetTexture(m_diffusion_kernel, "heights", s.environment.heights);
            m_shader.SetTexture(m_diffusion_kernel, "gradients", s.environment.gradients);

            m_updateEdge_kernel = m_shader.FindKernel("UpdateEdge");
            m_shader.SetTexture(m_updateEdge_kernel, "Read", m_amplitude);
            m_shader.SetTexture(m_updateEdge_kernel, "Write", amplitude);

            m_copy_kernel = m_shader.FindKernel("Copy");
            m_shader.SetTexture(m_copy_kernel, "Read", m_amplitude);
            m_shader.SetTexture(m_copy_kernel, "Read2", m_manualAmplitude);
            m_shader.SetTexture(m_copy_kernel, "Write", amplitude);

            m_manualPoint_kernel = m_shader.FindKernel("ManualPoint");
            m_shader.SetTexture(m_manualPoint_kernel, "Write", m_manualAmplitude);

            m_dissipation_kernel = m_shader.FindKernel("Dissipation");
            m_shader.SetTexture(m_dissipation_kernel, "Read", m_newManualAmplitude);
            m_shader.SetTexture(m_dissipation_kernel, "Write", m_manualAmplitude);

            m_deltaTime_id = Shader.PropertyToID("deltaTime");

            SetDefaultAmplitudes(s.simulation.GetDefaultAmplitudes(s.environment.transform).ToArray());
        }

        void SetDefaultAmplitudes(float[] amplitudes)
        {

            SetFloats(
                m_shader, 
                "Default", 
                amplitudes);
        }

        void SetFloats(ComputeShader shader, string id, float[] f)
        {
            var v = new float[f.Length * 4];
            for (int i = 0; i < f.Length; i++)
            {
                v[i * 4] = f[i];
            }
            shader.SetFloats(id, v);
        }

        float cflTimestep()
        {
            var dx = (m_settings.environment.size.x * 2f / m_settings.simulation.GetResolution());
            return dx / m_profileBuffer.groupSpeed;
        }

        internal void Update()
        {
            var ts = cflTimestep();
            m_shader.SetFloat(m_deltaTime_id, cflTimestep() * Time.deltaTime);

            {
                var s = m_settings;
                SetDefaultAmplitudes(s.simulation.GetDefaultAmplitudes(s.environment.transform).ToArray());
                m_shader.SetTexture(m_advection_kernel, "Read", m_amplitude);
                m_shader.SetTexture(m_advection_kernel, "Write", m_newAmplitude);
                m_shader.GetKernelThreadGroupSizes(m_advection_kernel, out uint x, out uint y, out uint z);
                m_shader.Dispatch(m_advection_kernel, (int)(m_settings.simulation.GetResolution() / x), (int)(m_settings.simulation.GetResolution() / y), (int)(16 / z));
            }


            {
                m_shader.GetKernelThreadGroupSizes(m_diffusion_kernel, out uint x, out uint y, out uint z);
                m_shader.Dispatch(m_diffusion_kernel, (int)(m_settings.simulation.GetResolution() / x), (int)(m_settings.simulation.GetResolution() / y), (int)(16 / z));
            }

            {
                float[] amplitudes = new float[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                SetDefaultAmplitudes(amplitudes);
                m_shader.SetTexture(m_advection_kernel, "Read", m_manualAmplitude);
                m_shader.SetTexture(m_advection_kernel, "Write", m_newManualAmplitude);

                uint x, y, z;

                m_shader.GetKernelThreadGroupSizes(m_advection_kernel, out x, out y, out z);
                m_shader.Dispatch(m_advection_kernel, (int)(m_settings.simulation.GetResolution() / x), (int)(m_settings.simulation.GetResolution() / y), (int)(16 / z));

            }

            {
                m_shader.GetKernelThreadGroupSizes(m_dissipation_kernel, out uint x, out uint y, out uint z);
                m_shader.Dispatch(m_dissipation_kernel, (int)(m_settings.simulation.GetResolution() / x), (int)(m_settings.simulation.GetResolution() / y), (int)(16 / z));
            }

            {
                m_shader.GetKernelThreadGroupSizes(m_copy_kernel, out uint x, out uint y, out uint z);
                m_shader.Dispatch(m_copy_kernel, (int)(m_settings.simulation.GetResolution() / x), (int)(m_settings.simulation.GetResolution() / y), (int)(16 / z));

                m_shader.GetKernelThreadGroupSizes(m_updateEdge_kernel, out x, out y, out z);
                m_shader.Dispatch(m_updateEdge_kernel, 
                    (int)((m_settings.simulation.GetResolution() + 2) / x), 
                    (int)((m_settings.simulation.GetResolution() + 2) / y), 
                    (int)(16 / z));
            }
        }

        internal void IncreaseAmplitude(float amplitude, Vector3 point)
        {
            point.x = point.x * (m_settings.simulation.GetResolution() - 1);
            point.y = point.y * (m_settings.simulation.GetResolution() - 1);
            point.z = point.z * 16 - 0.5f;

            m_shader.SetVector("manual_point", point);
            m_shader.SetFloat("manual_point_value", amplitude);
            m_shader.Dispatch(m_manualPoint_kernel, 1, 1, 1);
        }
    }
}
