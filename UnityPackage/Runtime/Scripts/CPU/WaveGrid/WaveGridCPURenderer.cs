using UnityEngine;
using WaveGrid;

internal class WaveGridCPURenderer
{
    private WaveGridCPUData m_data;
    private Material m_material;
    
    private int m_profile_period_id;
    private int m_texture_data_id;
    private int m_camera_pos_id;
    private int m_cameraProjectionForward_id;
    private int m_cameraInverseProjection_id;
    private int m_waterLevel_id;

    private Texture2D m_textureData;
    private Camera m_camera;

    private float m_waterLevel;
    
    internal WaveGridCPURenderer(WaveGridCPUData surfaceData, Material material, Camera camera, float waterLevel)
    {
        m_data = surfaceData;
        m_material = material;
        m_camera = camera;
        m_waterLevel = waterLevel;
        m_profile_period_id = Shader.PropertyToID("profilePeriod");
        m_texture_data_id = Shader.PropertyToID("textureData");
        m_camera_pos_id = Shader.PropertyToID("cameraPos");
        m_cameraProjectionForward_id = Shader.PropertyToID("cameraProjectionForward");
        m_cameraInverseProjection_id = Shader.PropertyToID("cameraInverseProjection");
        m_waterLevel_id = Shader.PropertyToID("waterLevel");
    }

    internal void Update(UpdateSettings settings)
    {
        if (m_data.hasProfileData)
        {
            if (!m_textureData)
            {
                int size = m_data.profileBufferData.Length/4;
                
                m_textureData = new Texture2D(size, 1, TextureFormat.RGBAFloat, false);
                m_textureData.wrapMode = TextureWrapMode.Repeat;
                m_textureData.filterMode = FilterMode.Bilinear;
            }
            m_material.SetVector(m_camera_pos_id, m_camera.transform.position);
            m_material.SetMatrix(
                m_cameraInverseProjection_id, 
                m_camera.transform.localToWorldMatrix * m_camera.projectionMatrix.inverse);
            m_material.SetVector(m_cameraProjectionForward_id, m_camera.projectionMatrix.MultiplyPoint(Vector3.forward));
            m_material.SetFloat(m_profile_period_id, m_data.profileBufferPeriod);
            m_textureData.SetPixelData(m_data.profileBufferData, 0);
            m_textureData.Apply();
            m_material.SetTexture(m_texture_data_id, m_textureData);
            m_material.SetFloat(m_waterLevel_id, m_waterLevel);
            m_material.SetFloat("direction", settings.direction);
        }
    }

}
