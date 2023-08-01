using ProfileBuffer;
using UnityEngine;

namespace WaveGrid
{
    internal class WaveGridGPUAdvection
    {
        internal RenderTexture amplitude { get; private set; }

        private ComputeShader m_shader;
        private int m_advection_kernel;
        private int m_diffusion_kernel;

        private RenderTexture m_newAmplitude;
        private Settings m_settings;
        private int m_deltaTime_id;
        private ProfileBufferGPU m_profileBuffer;

        internal WaveGridGPUAdvection(
            Settings settings,
            WaveGridGPUEnvironment environment,
            ProfileBufferGPU profileBuffer)
        {
            m_settings = settings;
            m_profileBuffer = profileBuffer;

            // Create amplitude render textures
            RenderTextureDescriptor renderTextureDescriptor = new RenderTextureDescriptor();
            renderTextureDescriptor.useMipMap = false;
            renderTextureDescriptor.width = settings.n_x;
            renderTextureDescriptor.height = settings.n_x;
            renderTextureDescriptor.volumeDepth = 16;
            renderTextureDescriptor.enableRandomWrite = true;
            renderTextureDescriptor.colorFormat = RenderTextureFormat.RFloat;
            renderTextureDescriptor.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            renderTextureDescriptor.msaaSamples = 1;
            renderTextureDescriptor.sRGB = false;
            renderTextureDescriptor.autoGenerateMips = false;

            amplitude = new RenderTexture(renderTextureDescriptor);
            amplitude.wrapMode = TextureWrapMode.Clamp;
            amplitude.filterMode = FilterMode.Bilinear;
            amplitude.name = "AmplitudeTexture";
            if (!amplitude.Create())
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

            //Create shader
            m_shader = (ComputeShader)Resources.Load("Advection");
            m_shader.SetFloat("groupSpeed", profileBuffer.groupSpeed);
            m_shader.SetFloat("dx",settings.terrain.size.x * 2f / settings.n_x);
            m_shader.SetFloat("x_min", -settings.terrain.size.x);
            m_shader.SetFloat("env_dx", settings.terrain.size.x * 2.0f / environment.heights.width);

            m_advection_kernel = m_shader.FindKernel("Advection");
            m_shader.SetTexture(m_advection_kernel, "Read", amplitude);
            m_shader.SetTexture(m_advection_kernel, "Write", m_newAmplitude);
            m_shader.SetTexture(m_advection_kernel, "heights", environment.heights);
            m_shader.SetTexture(m_advection_kernel, "gradients", environment.gradients);


            m_diffusion_kernel = m_shader.FindKernel("Diffusion");
            m_shader.SetTexture(m_diffusion_kernel, "Read", m_newAmplitude);
            m_shader.SetTexture(m_diffusion_kernel, "Write", amplitude);
            m_shader.SetTexture(m_diffusion_kernel, "heights", environment.heights);
            m_shader.SetTexture(m_diffusion_kernel, "gradients", environment.gradients);

            m_deltaTime_id = Shader.PropertyToID("deltaTime");

            SetDefaultAmplitudes(m_settings);
        }

        void SetDefaultAmplitudes(Settings settings)
        {
            float[] defaultValues = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.1f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
            SetFloats(m_shader, "Default", defaultValues);
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
            var dx = (m_settings.terrain.size.x * 2f / m_settings.n_x);
            return dx / m_profileBuffer.groupSpeed;
        }

        internal void Update()
        {
            var ts = cflTimestep();
            m_shader.SetFloat(m_deltaTime_id, cflTimestep() * Time.deltaTime);

            {
                m_shader.GetKernelThreadGroupSizes(m_advection_kernel, out uint x, out uint y, out uint z);
                m_shader.Dispatch(m_advection_kernel, (int)(m_settings.n_x / x), (int)(m_settings.n_x / y), (int)(16 / z));
            }


            {
                m_shader.GetKernelThreadGroupSizes(m_diffusion_kernel, out uint x, out uint y, out uint z);
                m_shader.Dispatch(m_diffusion_kernel, (int)(m_settings.n_x / x), (int)(m_settings.n_x / y), (int)(16 / z));
            }
        }
    }
}
