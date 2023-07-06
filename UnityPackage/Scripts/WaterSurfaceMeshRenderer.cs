using System;
using UnityEngine;

internal class WaterSurfaceMeshRenderer
{
    private WaterSurfaceMeshData m_data;
    private Material m_material;
    private int m_profile_period_id;
    private int m_texture_data_id;
    private Texture2D m_textureData;
    
    internal WaterSurfaceMeshRenderer(WaterSurfaceMeshData surfaceData, Material material)
    {
        m_data = surfaceData;
        m_material = material;
        m_profile_period_id = Shader.PropertyToID("profilePeriod");
        m_texture_data_id = Shader.PropertyToID("textureData");
    }

    internal void Update()
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

            m_material.SetFloat(m_profile_period_id, m_data.profileBufferPeriod);
            m_textureData.SetPixelData(m_data.profileBufferData, 0);
            m_textureData.Apply();
            m_material.SetTexture(m_texture_data_id, m_textureData);
        }
    }

}
