using System.Linq;
using UnityEngine;
using WaveGrid;

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
    private Camera m_camera;
    
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

        var size = new Vector2(settings.terrain.size.x, settings.terrain.size.y);
        m_material.SetVector(Shader.PropertyToID("xmin"), -size);
        var idx = new Vector2(1f / (size.x * 2f / settings.n_x), 1f / (size.x * 2f / settings.n_x));
        m_material.SetVector(Shader.PropertyToID("dx"), idx);
        m_material.SetFloat(Shader.PropertyToID("nx"), settings.n_x);

        var terrainPosition = settings.terrain.transform.GetPosition();
        var terrainTranslationXZ = new Vector2(terrainPosition.x, terrainPosition.z);
        m_material.SetVector(Shader.PropertyToID("translation"), terrainTranslationXZ);
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
        if (!m_amplitude)
        {
            var size = m_data.amplitudesSize;
            m_amplitude = new Texture3D(size.x, size.y, size.z, TextureFormat.RFloat, false);
            m_amplitude.filterMode = FilterMode.Bilinear;
            m_amplitude.wrapMode = TextureWrapMode.Clamp;
            m_material.SetTexture("amplitude", m_amplitude);
        }
        m_amplitude.SetPixelData(m_data.amplitudes, 0);
        m_amplitude.Apply(updateMipmaps: false);

        m_material.SetFloat("amp_mult", settings.amplitudeMultiplier);
        m_material.SetFloatArray(
            "directions",
            settings.amplitudeTracks.IncludeValues.Select(x => x ? 1.0f : 0.0f).ToArray());
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
