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

    internal WaveGridCPURenderer(WaveGridCPUData surfaceData, Settings settings,  Material material)
    {
        m_data = surfaceData;
        m_material = material;
        m_camera = settings.camera;
        m_profile_period_id = Shader.PropertyToID("profilePeriod");
        m_profileBuffer_id = Shader.PropertyToID("textureData");
        m_camera_pos_id = Shader.PropertyToID("cameraPos");
        m_cameraProjectionForward_id = Shader.PropertyToID("cameraProjectionForward");
        m_cameraInverseProjection_id = Shader.PropertyToID("cameraInverseProjection");

        m_material.SetFloat("waterLevel", settings.terrain.water_level);
        m_settings = settings;

        var size = new Vector2(settings.terrain.size.x, settings.terrain.size.y);
        m_material.SetVector(Shader.PropertyToID("xmin"), -size);
        var idx = new Vector2(1f / (size.x * 2f / settings.n_x), 1f / (size.x * 2f / settings.n_x));
        m_material.SetVector(Shader.PropertyToID("dx"), idx);
        m_material.SetFloat(Shader.PropertyToID("nx"), settings.n_x);

        var terrainPosition = settings.terrain.transform.GetPosition();
        var terrainTranslationXZ = new Vector2(terrainPosition.x, terrainPosition.z);
        m_material.SetVector(Shader.PropertyToID("translation"), terrainTranslationXZ);
        m_material.SetFloatArray("defaultAmplitude", settings.defaultAmplitude);

        SetAmplitudeTextures(surfaceData, settings);
    }

    private void SetAmplitudeTextures(WaveGridCPUData surfaceData, Settings settings)
    {
        // Create texture
        m_amplitude = new Texture3D(settings.n_x, settings.n_x, 16, TextureFormat.RFloat, false);
        m_amplitude.filterMode = FilterMode.Bilinear;
        m_amplitude.wrapMode = TextureWrapMode.Clamp;

        // Create render texture
        RenderTextureDescriptor renderTextureDescriptor = new RenderTextureDescriptor();
        renderTextureDescriptor.useMipMap = false;
        renderTextureDescriptor.width = settings.n_x + 2;
        renderTextureDescriptor.height = settings.n_x + 2;
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

        SetDefaultAmplitudes(settings);
    }

    void SetDefaultAmplitudes(Settings settings)
    {
        SetFloats(m_shader, "Default", settings.defaultAmplitude.ToArray());

        m_shader.GetKernelThreadGroupSizes(m_init_kernel, out uint x, out uint y, out uint z);
        m_shader.Dispatch(m_init_kernel, (int)((settings.n_x + 2) / x), (int)((settings.n_x + 2) / y), (int)(16 / z));
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
            m_shader.Dispatch(m_copy_kernel, (int)(m_settings.n_x / x), (int)(m_settings.n_x / y), (int)(16 / z));
        }

        m_material.SetFloat("amp_mult", settings.amplitudeMultiplier);
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
