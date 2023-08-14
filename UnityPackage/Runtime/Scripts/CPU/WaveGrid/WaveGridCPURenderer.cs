using System;
using System.Linq;
using UnityEngine;
using WaveGrid;
using static WaterWaveSurface.API;

internal class WaveGridCPURenderer
{
    private WaveGridCPUData m_data;
    private Material m_material;
    
    private int m_profile_period_id;
    private int m_profileBuffer_id;
    private int m_camera_pos_id;
    private int m_cameraProjectionForward_id;
    private int m_cameraInverseProjection_id;

    private Texture2D m_profileBuffer;
    private Texture3D m_amplitude;
    private RenderTexture amplitude;
    private Camera m_camera;
    private ComputeShader m_shader;

    private int m_init_kernel;
    private int m_copy_kernel;

    private Settings m_settings;

    internal WaveGridCPURenderer(WaveGridCPUData surfaceData, Settings s,  Material material)
    {
        m_data = surfaceData;
        m_material = material;
        m_camera = s.visualization.camera;
        m_profile_period_id = Shader.PropertyToID("profilePeriod");
        m_profileBuffer_id = Shader.PropertyToID("textureData");
        m_camera_pos_id = Shader.PropertyToID("cameraPos");
        m_cameraProjectionForward_id = Shader.PropertyToID("cameraProjectionForward");
        m_cameraInverseProjection_id = Shader.PropertyToID("cameraInverseProjection");

        m_material.SetFloat("waterLevel", s.environment.water_level);
        m_settings = s;

        var size = new Vector2(s.environment.size.x, s.environment.size.y);
        m_material.SetVector(Shader.PropertyToID("xmin"), -size);
        var resolution = s.simulation.GetResolution();
        var idx = new Vector2(1f / (size.x * 2f / resolution), 1f / (size.x * 2f / resolution));
        m_material.SetVector(Shader.PropertyToID("dx"), idx);
        m_material.SetVector(Shader.PropertyToID("nx"), new Vector2(resolution, resolution));

        var t = s.environment.transform;
        var p = t.GetPosition();
        Matrix4x4 m =
            Matrix4x4.Translate(new Vector3(p.x, 0, p.z)) *
            Matrix4x4.Rotate(Quaternion.Euler(0, t.rotation.eulerAngles.y, 0));

        m_material.SetMatrix("env_trans", m);
        m_material.SetMatrix("env_trans_inv", m.inverse);
        m_material.SetVector("env_size",  new Vector2(s.environment.size.x, s.environment.size.y));

        m_material.SetFloat("env_rotation", t.rotation.eulerAngles.y * Mathf.Deg2Rad);


        m_material.SetFloatArray(
            "defaultAmplitude", 
            s.simulation.GetDefaultAmplitudes(s.environment.transform));
        SetAmplitudeTextures(surfaceData, s);
    }

    private void SetAmplitudeTextures(WaveGridCPUData surfaceData, Settings s)
    {
        // Create texture
        m_amplitude = new Texture3D(s.simulation.GetResolution(), s.simulation.GetResolution(), 16, TextureFormat.RFloat, false);
        m_amplitude.filterMode = FilterMode.Bilinear;
        m_amplitude.wrapMode = TextureWrapMode.Clamp;

        // Create render texture
        RenderTextureDescriptor renderTextureDescriptor = new RenderTextureDescriptor();
        renderTextureDescriptor.useMipMap = false;
        renderTextureDescriptor.width = s.simulation.GetResolution() + 2;
        renderTextureDescriptor.height = s.simulation.GetResolution() + 2;
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
        amplitude.name = "AmplitudeTextureCPU";
        if (!amplitude.Create())
        {
            Debug.LogError("Could not create amplitude texture");
        }

        amplitude.filterMode = FilterMode.Bilinear;
        amplitude.wrapMode = TextureWrapMode.Clamp;
        m_material.SetTexture("amplitude", amplitude);

        //Create shader
        m_shader = (ComputeShader)Resources.Load("Advection");

        m_init_kernel = m_shader.FindKernel("Init");
        m_shader.SetTexture(m_init_kernel, "Write", amplitude);

        m_copy_kernel = m_shader.FindKernel("Copy");
        m_shader.SetTexture(m_copy_kernel, "Read", m_amplitude);
        m_shader.SetTexture(m_copy_kernel, "Write", amplitude);

        SetDefaultAmplitudes(s);
    }

    void SetDefaultAmplitudes(Settings s)
    {
        SetFloats(
            m_shader, 
            "Default", 
            s.simulation.GetDefaultAmplitudes(s.environment.transform).ToArray());

        m_shader.GetKernelThreadGroupSizes(m_init_kernel, out uint x, out uint y, out uint z);
        m_shader.Dispatch(m_init_kernel, (int)((s.simulation.GetResolution() + 2) / x), (int)((s.simulation.GetResolution() + 2) / y), (int)(16 / z));
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

    internal void Update(UpdateSettings settings)
    {
        if (m_data.hasProfileData && m_data.hasAmplitudeData)
        {
            SetProfileBuffer();
            SetAmplitude(settings);
            SetCamera(settings);
        }

    }

    private void SetCamera(UpdateSettings settings)
    {
        m_material.SetVector(m_camera_pos_id, m_camera.transform.position);
        m_material.SetMatrix(
            m_cameraInverseProjection_id,
            m_camera.transform.localToWorldMatrix * m_camera.projectionMatrix.inverse);
        m_material.SetVector(m_cameraProjectionForward_id, m_camera.projectionMatrix.MultiplyPoint(Vector3.forward));
    }

    private void SetAmplitude(UpdateSettings settings)
    {
        m_amplitude.SetPixelData(m_data.amplitudes, 0);
        m_amplitude.Apply(updateMipmaps: false);

        {
            m_shader.GetKernelThreadGroupSizes(m_copy_kernel, out uint x, out uint y, out uint z);
            m_shader.Dispatch(m_copy_kernel, (int)(m_settings.simulation.GetResolution() / x), (int)(m_settings.simulation.GetResolution() / y), (int)(16 / z));
        }

        m_material.SetFloat("amp_mult", settings.amplitudeMultiplier);
        m_material.SetFloat("renderOutsideBorders", settings.renderOutsideBorders ?1.0f: 0.0f);
    }

    private void SetProfileBuffer()
    {
        if (!m_profileBuffer)
        {
            int size = m_data.profileBufferData.Length / 4;

            m_profileBuffer = new Texture2D(size, 1, TextureFormat.RGBAFloat, false);
            m_profileBuffer.wrapMode = TextureWrapMode.Repeat;
            m_profileBuffer.filterMode = FilterMode.Bilinear;
        }
        m_material.SetFloat(m_profile_period_id, m_data.profileBufferPeriod);
        m_profileBuffer.SetPixelData(m_data.profileBufferData, 0);
        m_profileBuffer.Apply();
        m_material.SetTexture(m_profileBuffer_id, m_profileBuffer);
    }
}
