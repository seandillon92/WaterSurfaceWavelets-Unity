using UnityEngine;
using WaterWaveSurface;

internal class TerrainBaker : MonoBehaviour
{
    [SerializeField]
    private WaterTerrain m_terrain;

    [SerializeField]
    private LayerMask m_layers;

    [SerializeField]
    private Vector2Int m_samples;

    [SerializeField]
    private float m_waterLevel;

    public void Bake(Vector3Int extends, Vector3 position)
    {
        m_terrain.heights = new float[(m_samples.x + 1) * (m_samples.y + 1)];
        var heights = m_terrain.heights;

        float dx = 2f / (m_samples.x);
        float dz = 2f / (m_samples.y);

        int index = 0;
        for (float x = -1f ; x <= 1f; x+= dx)
        {
            for (float z = -1f; z <= 1f; z += dz)
            {
                Ray ray = new Ray(new Vector3(x * extends.x + position.x, position.y + extends.y, z * extends.z + position.z), Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, extends.y * 2, m_layers))
                {
                    heights[index] = m_waterLevel-hit.point.y;
                }
                else
                {
                    heights[index] = m_waterLevel-(position.y - extends.y);
                }

                index++;
            }
        }

        m_terrain.size = new Vector2Int(extends.x, extends.z);
        m_terrain.transform = transform.localToWorldMatrix;
        m_terrain.water_level = m_waterLevel;
    }
}
