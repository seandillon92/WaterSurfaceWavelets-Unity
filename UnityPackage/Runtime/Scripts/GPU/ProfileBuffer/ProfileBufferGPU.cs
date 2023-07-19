using UnityEngine;
using System;

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
        private ComputeShader m_shader;
        private int m_kernelIndex;
        private RenderTexture m_data;
        private Texture2D m_spectrum_data;
        private int m_integration_nodes;
        private float m_zmin;
        private float m_zmax;
        private int m_time_id;
        private int m_resolution;

        internal ProfileBufferGPU(
            float zmin, 
            float zmax,
            Spectrum spectrum,
            int resolution = 4096, 
            int integrationNodes = 100, 
            int periodicity = 2)
        {
            // Initialize simple values
            m_integration_nodes = integrationNodes;
            m_zmin = zmin;
            m_zmax = zmax;
            m_time_id = Shader.PropertyToID("time");

            // Create data texture
            RenderTextureDescriptor descriptor = new RenderTextureDescriptor();
            descriptor.width = resolution;
            descriptor.height = 1;
            descriptor.useMipMap = false;
            descriptor.volumeDepth =0;
            descriptor.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;
            descriptor.enableRandomWrite = true;

            m_data = new RenderTexture(descriptor);
            if (!m_data.Create())
            {
                Debug.LogError("Could not create ProfileBuffer");
            }

            //Create spectrum texture
            m_spectrum_data = new Texture2D(m_integration_nodes, 1, TextureFormat.RGBA32, false, true);
            InitializeSpectrumData(spectrum);

            // Set shader uniforms
            m_shader = (ComputeShader)Resources.Load("Precompute");
            m_kernelIndex = m_shader.FindKernel("CSMain");
            
            m_shader.SetTexture(m_kernelIndex, Shader.PropertyToID("Result"), m_data);
            m_shader.SetTexture(m_kernelIndex, Shader.PropertyToID("Spectrum"), m_spectrum_data);
            m_shader.SetFloat(Shader.PropertyToID("period"), periodicity * Mathf.Pow(2, zmax));
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

        internal void Update()
        {
            m_shader.SetFloat(m_time_id, Time.time);
            m_shader.Dispatch(m_kernelIndex, m_resolution, 0, 0);
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(this.m_spectrum_data);
            UnityEngine.Object.Destroy(this.m_data);
        }
    }
}
