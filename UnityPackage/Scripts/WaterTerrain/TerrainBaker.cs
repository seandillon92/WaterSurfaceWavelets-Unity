using UnityEngine;
using WaterWaveSurface;

internal class TerrainBaker : MonoBehaviour
{
    [SerializeField]
    private WaterTerrain m_terrain;

    [SerializeField]
    private LayerMask m_layers;

    [SerializeField]
    private int m_minHeight = -100;

    [SerializeField]
    private int m_maxHeight = 100;

    [SerializeField]
    private Vector2Int m_samples;

    [SerializeField]
    private int m_size;

    internal WaterTerrain WaterTerrain => m_terrain;

    private Mesh Debug_GenerateMesh(WaterSurface surface)
    {
        var mesh = new Mesh();
        var side = 100;
        var positions = new Vector3[side * side];
        var indices = new int[(side - 1) * (side - 1) * 4];
        var size = m_terrain.size;

        for (int i =  0; i < side * side; i++)
        {
            float x = i / (side);
            x = Mathf.Lerp(-1, 1, x / side);

            float y = i % side;
            y = Mathf.Lerp(-1, 1, y / side);

            x *= 0.99f * size;
            y *= 0.99f * size;
            positions[i] = 
                new Vector3(x, -surface.GetTerrainHeight(new Vector2(x,y)), y);
        }

        for (var i = 0; i < side - 1; i++)
        {
            for (var j = 0; j < side - 1; j++)
            {
                indices[i * (side - 1) * 4 + j * 4] = i * side + j;
                indices[i * (side - 1) * 4 + j * 4 + 1] = i * side + j + 1;
                indices[i * (side - 1) * 4 + j * 4 + 2] = (i + 1) * side + j + 1;
                indices[i * (side - 1) * 4 + j * 4 + 3] = (i + 1) * side + j;
            }   
        }

        mesh.vertices = positions;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetIndices(indices, MeshTopology.Quads, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        return mesh;
    }

    public void Bake()
    {
        m_terrain.heights = new float[(m_samples.x + 1) * (m_samples.y + 1)];
        var heights = m_terrain.heights;

        float dx = 1f / m_samples.x;
        float dy = 1f / m_samples.y;

        int index = 0;
        for (float x = -1f ; x <= 1f; x+= dx * 2)
        {
            for (float y = -1f; y <= 1f; y += dy * 2)
            {
                Ray ray = new Ray(new Vector3(x * m_size, m_maxHeight, y * m_size), Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, m_maxHeight - m_minHeight, m_layers))
                {
                    heights[index] = -hit.point.y;
                }
                else
                {
                    heights[index] = m_minHeight;
                }

                index++;
            }
        }

        m_terrain.size = m_size;
    }
}
