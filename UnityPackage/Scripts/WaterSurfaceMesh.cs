using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public class WaterSurfaceMesh
{
    public Mesh mesh { get; private set; }
    private Vertex[] m_vertices;
    private int m_count;
    private WaterSurfaceMeshData m_data;

    public WaterSurfaceMesh(WaterSurfaceMeshData data)
    {
        m_data = data;
        var count = m_data.size;
        m_count = count * count;
        mesh = new Mesh();
        var layout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord4, VertexAttributeFormat.Float32, 4),
        };

        mesh.SetVertexBufferParams(m_count, layout);
        mesh.SetIndices(m_data.indices, MeshTopology.Quads, 0);

        m_vertices = new Vertex[m_count];
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Vertex
    {
        public Vector3 pos;
        public Vector4 amplitude1;
        public Vector4 amplitude2;
        public Vector4 amplitude3;
        public Vector4 amplitude4;
    }

    public void Update()
    {
        for (int i = 0; i < m_count; i++)
        {
            m_vertices[i].pos = m_data.positions[i];

            m_vertices[i].amplitude1
                = new Vector4(m_data.amplitude[i][0], m_data.amplitude[i][1], m_data.amplitude[i][2], m_data.amplitude[i][3]);
            m_vertices[i].amplitude2
                = new Vector4(m_data.amplitude[i][4], m_data.amplitude[i][5], m_data.amplitude[i][6], m_data.amplitude[i][7]);
            m_vertices[i].amplitude3
                = new Vector4(m_data.amplitude[i][8], m_data.amplitude[i][9], m_data.amplitude[i][10], m_data.amplitude[i][11]);
            m_vertices[i].amplitude4
                = new Vector4(m_data.amplitude[i][12], m_data.amplitude[i][13], m_data.amplitude[i][14], m_data.amplitude[i][15]);
        }

        mesh.SetVertexBufferData(m_vertices, 0, 0, m_count);
    }

}
