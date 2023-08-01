using UnityEngine;
using System;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;

namespace ProfileBuffer
{
    internal class Spectrum
    {
        internal Spectrum(double windSpeed = 10.0)
        {
            m_windSpeed = windSpeed;
        }

        private double m_windSpeed;

        public double calculcate(double zeta)
        {
            double A = Math.Pow(1.1, 1.5 * zeta);
            double B = Math.Exp(-1.8038897788076411 * Math.Pow(4, zeta) / Math.Pow(m_windSpeed, 4));
            return 0.139098 * Math.Sqrt(A* B);
        }
    }

    public class ProfileBufferGPU : IDisposable
    {
        internal RenderTexture data { get; private set; }
        internal float period { get;private set; }

        private ComputeShader m_shader;
        private int m_kernelIndex;
        private Texture2D m_spectrum_data;
        private int m_integration_nodes;
        private float m_zmin;
        private float m_zmax;
        private int m_time_id;
        private int m_resolution;
        private float m_initial_time;


        internal float groupSpeed { get; private set; }

        internal ProfileBufferGPU(
            float zmin, 
            float zmax,
            Spectrum spectrum,
            float initial_time,
            int resolution = 4096, 
            int integrationNodes = 100, 
            int periodicity = 2)
        {
            period = periodicity * Mathf.Pow(2, zmax);
            // Initialize simple values
            m_integration_nodes = integrationNodes;
            m_zmin = zmin;
            m_zmax = zmax;
            m_time_id = Shader.PropertyToID("time");
            m_resolution = resolution;
            m_initial_time = initial_time;

            // Create data texture
            RenderTextureDescriptor descriptor = new RenderTextureDescriptor();
            descriptor.width = resolution;
            descriptor.height = 1;
            descriptor.useMipMap = false;
            descriptor.volumeDepth = 1;
            descriptor.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;
            descriptor.enableRandomWrite = true;
            descriptor.msaaSamples = 1;
            descriptor.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;

            data = new RenderTexture(descriptor);
            data.wrapMode = TextureWrapMode.Repeat;
            data.filterMode = FilterMode.Bilinear;
            if (!data.Create())
            {
                Debug.LogError("Could not create ProfileBuffer");
            }

            //Create spectrum texture
            m_spectrum_data = new Texture2D(m_integration_nodes, 1, TextureFormat.RGBA32, false, true);
            m_spectrum_data.wrapMode = TextureWrapMode.Repeat;
            m_spectrum_data.filterMode = FilterMode.Bilinear;
            InitializeSpectrumData(spectrum);
            InitializeGroupSpeed(spectrum);

            // Set shader uniforms
            m_shader = (ComputeShader)Resources.Load("Precompute");
            m_kernelIndex = m_shader.FindKernel("CSMain");
            
            m_shader.SetTexture(m_kernelIndex, Shader.PropertyToID("Result"), data);
            m_shader.SetTexture(m_kernelIndex, Shader.PropertyToID("Spectrum"), m_spectrum_data);
            m_shader.SetFloat(Shader.PropertyToID("period"), period);
            m_shader.SetFloat(Shader.PropertyToID("resolution"), resolution);
            m_shader.SetFloat(Shader.PropertyToID("z_min"), zmin);
            m_shader.SetFloat(Shader.PropertyToID("z_max"), zmax);
        }

        void InitializeSpectrumData(Spectrum spectrum)
        {
            double[] data = new double[m_integration_nodes];
            double dz = (m_zmax - m_zmax) / m_integration_nodes;
            double z = m_zmin + 0.5 * dz;
            for (uint i = 1; i < m_integration_nodes; i++)
            {
                z += dz;
                data[i] =  spectrum.calculcate(z);
            }

            m_spectrum_data.SetPixelData(data, 0);
            m_spectrum_data.Apply();
        }

        

        void InitializeGroupSpeed(Spectrum spectrum)
        {
            (double , double) func(double zeta)
            {
                const double tau = 6.28318530718f;
                double waveLength = Math.Pow(2, zeta);
                double waveNumber = tau / waveLength;
                double cg = 0.5f * Math.Sqrt(9.81 / waveNumber);
                double density = spectrum.calculcate(zeta);
                return ( cg* density, density);
            }

            double dz = (m_zmax - m_zmin) / m_integration_nodes;
            double z = m_zmin + 0.5 * dz;
            (double, double) result = (0,0);
            var ret = func(z);
            result.Item1 = ret.Item1 * dz;
            result.Item2 = ret.Item2 * dz;
            for (uint i = 1; i < m_integration_nodes; i++)
            {
                z += dz;
                ret = func(z);
                result.Item1 += ret.Item1 * dz;
                result.Item2 += ret.Item2 * dz;
            }
            
            groupSpeed = (float)(3.0 * result.Item1 / result.Item2);
        }

        internal void Update()
        {
            m_shader.SetFloat(m_time_id, Time.time +m_initial_time);
            m_shader.GetKernelThreadGroupSizes(m_kernelIndex, out uint x, out uint _, out uint _);
            m_shader.Dispatch(m_kernelIndex, (int)(m_resolution / x), 1, 1);
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(this.m_spectrum_data);
            UnityEngine.Object.Destroy(this.data);
        }
    }
}
