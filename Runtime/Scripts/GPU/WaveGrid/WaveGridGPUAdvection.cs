using ProfileBuffer;
using Unity.Profiling;
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



            m_amplitude = m_settings.simulation.amplitude;
            m_amplitude.wrapMode = TextureWrapMode.Clamp;
            m_amplitude.filterMode = FilterMode.Bilinear;
            m_amplitude.name = "AmplitudeTexture";
            if (!m_amplitude.Create())
            {
                Debug.LogError("Could not create amplitude texture");
            }

            m_newAmplitude = new RenderTexture(m_amplitude);
            m_newAmplitude.wrapMode = TextureWrapMode.Clamp;
            m_newAmplitude.filterMode = FilterMode.Bilinear;
            m_newAmplitude.name = "NewAmplitudeTexture";
            if (!m_newAmplitude.Create())
            {
                Debug.LogError("Could not create new amplitude texture");
            }

            m_manualAmplitude = new RenderTexture(m_amplitude);
            m_manualAmplitude.wrapMode = TextureWrapMode.Clamp;
            m_manualAmplitude.filterMode = FilterMode.Bilinear;
            m_manualAmplitude.name = "ManualAmplitudeTexture";
            if (!m_manualAmplitude.Create())
            {
                Debug.LogError("Could not create manual amplitude texture");
            }

            m_newManualAmplitude = new RenderTexture(m_amplitude);
            m_newManualAmplitude.wrapMode = TextureWrapMode.Clamp;
            m_newManualAmplitude.filterMode = FilterMode.Bilinear;
            m_newManualAmplitude.name = "NewManualAmplitudeTexture";
            if (!m_newManualAmplitude.Create())
            {
                Debug.LogError("Could not create new manual amplitude texture");
            }
            amplitude = new RenderTexture(m_amplitude);

            amplitude.width = s.simulation.GetResolution() + 2;
            amplitude.height = s.simulation.GetResolution() + 2;
            if (!amplitude.Create())
            {
                Debug.LogError("Could not create amplitude texture");
            }


            //Create shader
            m_shader = (ComputeShader)Resources.Load("Advection");
            var factor = m_settings.simulation.GetResolution() / 128;
            m_shader.SetFloat("resolution_factor", factor);
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

        static readonly ProfilerMarker mk_adv = new ProfilerMarker("advection1");
        static readonly ProfilerMarker mk_adv2 = new ProfilerMarker("advection2");
        static readonly ProfilerMarker mk_diff = new ProfilerMarker("diffusion");
        static readonly ProfilerMarker mk_diss = new ProfilerMarker("dissipation");
        static readonly ProfilerMarker mk_copy = new ProfilerMarker("copy");
        static readonly ProfilerMarker mk_edge = new ProfilerMarker("edge");
        private readonly int Read = Shader.PropertyToID("Read");
        private readonly int Write = Shader.PropertyToID("Write");


        internal void Update(UpdateSettings settings)
        {
            m_shader.SetFloat(m_deltaTime_id, cflTimestep() * Time.deltaTime);
            mk_adv.Begin();
            if (settings.updateSimulation)
            {
                var s = m_settings;
                SetDefaultAmplitudes(s.simulation.GetDefaultAmplitudes(s.environment.transform).ToArray());
                m_shader.SetTexture(m_advection_kernel, Read, m_amplitude);
                m_shader.SetTexture(m_advection_kernel, Write, m_newAmplitude);
                m_shader.GetKernelThreadGroupSizes(m_advection_kernel, out uint x, out uint y, out uint z);
                m_shader.Dispatch(m_advection_kernel, (int)(m_settings.simulation.GetResolution() / x), (int)(m_settings.simulation.GetResolution() / y), (int)(16 / z));
            }
            mk_adv.End();

            mk_diff.Begin();
            if (settings.updateSimulation)
            {
                m_shader.GetKernelThreadGroupSizes(m_diffusion_kernel, out uint x, out uint y, out uint z);
                m_shader.Dispatch(m_diffusion_kernel, (int)(m_settings.simulation.GetResolution() / x), (int)(m_settings.simulation.GetResolution() / y), (int)(16 / z));
            }
            mk_diff.End();

            mk_adv2.Begin();
            {
                float[] amplitudes = new float[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                SetDefaultAmplitudes(amplitudes);
                m_shader.SetTexture(m_advection_kernel, Read, m_manualAmplitude);
                m_shader.SetTexture(m_advection_kernel, Write, m_newManualAmplitude);

                uint x, y, z;

                m_shader.GetKernelThreadGroupSizes(m_advection_kernel, out x, out y, out z);
                m_shader.Dispatch(m_advection_kernel, (int)(m_settings.simulation.GetResolution() / x), (int)(m_settings.simulation.GetResolution() / y), (int)(16 / z));

            }
            mk_adv2.End();

            mk_diss.Begin();
            {
                m_shader.GetKernelThreadGroupSizes(m_dissipation_kernel, out uint x, out uint y, out uint z);
                m_shader.Dispatch(m_dissipation_kernel, (int)(m_settings.simulation.GetResolution() / x), (int)(m_settings.simulation.GetResolution() / y), (int)(16 / z));
            }
            mk_diss.End();

            mk_copy.Begin();
            {

                m_shader.GetKernelThreadGroupSizes(m_copy_kernel, out uint x, out uint y, out uint z);
                m_shader.Dispatch(m_copy_kernel, (int)(m_settings.simulation.GetResolution() / x), (int)(m_settings.simulation.GetResolution() / y), (int)(16 / z));

            }
            mk_copy.End();


            mk_edge.Begin();
            if (settings.updateSimulation)
            {
                m_shader.GetKernelThreadGroupSizes(m_updateEdge_kernel, out uint x, out uint y, out uint z);
                float resolution = m_settings.simulation.GetResolution() + 2; 
                m_shader.Dispatch(m_updateEdge_kernel, 
                    Mathf.CeilToInt(resolution/ x), 
                    Mathf.CeilToInt(resolution / y), 
                    Mathf.CeilToInt(16f / z));
            }
            mk_edge.End();

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
