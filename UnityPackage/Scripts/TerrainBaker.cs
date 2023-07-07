using System;
using UnityEngine;
using WaterWaveSurface;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
internal class TerrainBaker : MonoBehaviour
{
    [SerializeField]
    private LayerMask m_layers;

    [SerializeField]
    private WaterSurface m_surface;

    [SerializeField]
    private int m_minHeight = -100;

    [SerializeField]
    private int m_maxHeight = 100;

    [SerializeField]
    private TextAsset dataText;

    [SerializeField]
    private MeshRenderer m_renderer;

    [SerializeField]
    private MeshFilter m_filter;

    private void Awake()
    {
        m_renderer = GetComponent<MeshRenderer>();
        m_filter = GetComponent<MeshFilter>();
        Load();
    }

    public void Start() {
        GenerateMesh();
    }

    public void Load()
    {
        var text = dataText.text;
        var tokens = text.Split('\n', '\t');
        Debug.Log("Tokens: " + tokens.Length);
        var settings = m_surface.Settings;
        settings.terrain = new float[tokens.Length];
        for(int i = 0; i < tokens.Length; i++)
        {
            settings.terrain[i] = float.Parse(tokens[i]);
        }

    }

    public void GenerateMesh()
    {
        var mesh = new Mesh();
        var side = 100;
        var positions = new Vector3[side * side];
        var indices = new int[(side - 1) * (side - 1) * 4];
        var size = m_surface.Settings.size;

        for (int i =  0; i < side * side; i++)
        {
            float x = i / (side);
            x = Mathf.Lerp(-1, 1, x / side);

            float y = i % side;
            y = Mathf.Lerp(-1, 1, y / side);

            x *= 0.99f * size;
            y *= 0.99f * size;
            positions[i] = 
                new Vector3(x, (float)(-10 * Math.Tanh(m_surface.GetTerrainHeight(new Vector2(x,y)) * 0.1f)), y);
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

        m_filter.sharedMesh = mesh;
    }

    public void Bake()
    {
        var settings = m_surface.Settings;

        var size = settings.size;
        settings.terrain = new float[(size * 2 + 1) * (size * 2 + 1)];
        var terrain = settings.terrain;

        for (int i = -size; i <= size; i++)
        {
            for (int j = -size; j <= size; j++)
            {

                int index = (i + size) * (size * 2 + 1) + (j + size);
                Ray ray = new Ray(new Vector3(i, m_maxHeight, j), Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, m_maxHeight - m_minHeight, m_layers))
                {
                    terrain[index] = hit.point.y;
                }
                else
                {
                    terrain[index] = m_minHeight;
                }
            }
        }
    }
}
