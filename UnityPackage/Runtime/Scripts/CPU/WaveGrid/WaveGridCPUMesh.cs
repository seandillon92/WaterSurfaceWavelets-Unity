using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

internal class WaveGridCPUMesh
{
    internal Mesh mesh { get; private set; }
    private Vertex[] m_vertices;
    private int m_count;
    private WaveGridCPUData m_data;

    internal WaveGridCPUMesh(WaveGridCPUData data)
    {
        m_data = data;

        var count = m_data.size;
        m_count = count * count;
        mesh = new Mesh();
        var layout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord4, VertexAttributeFormat.Float32, 4),
        };

        mesh.SetVertexBufferParams(m_count, layout);
        mesh.SetIndices(m_data.indices, MeshTopology.Quads, 0);

        m_vertices = new Vertex[m_count];

        for (int i = 0; i < m_vertices.Length; i++)
        {
            m_vertices[i].uv = new Vector2((i + 1) / (float)count, (i + 1) % (float)count);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Vertex
    {
        public Vector4 pos;
        public Vector2 uv;
        public Vector4 amplitude1;
        public Vector4 amplitude2;
        public Vector4 amplitude3;
        public Vector4 amplitude4;
    }

    internal void Update()
    {
        for (int i = 0; i < m_count; i++)
        {
            m_vertices[i].pos = m_data.positions[i];

            m_vertices[i].amplitude1
                = new Vector4(m_data.amplitude[i*16], m_data.amplitude[i*16 + 1], m_data.amplitude[i*16 + 2], m_data.amplitude[i*16 + 3]);
            m_vertices[i].amplitude2
                = new Vector4(m_data.amplitude[i*16 + 4], m_data.amplitude[i*16 + 5], m_data.amplitude[i*16 + 6], m_data.amplitude[i*16 + 7]);
            m_vertices[i].amplitude3
                = new Vector4(m_data.amplitude[i*16 + 8], m_data.amplitude[i*16 + 9], m_data.amplitude[i*16 + 10], m_data.amplitude[i*16 + 11]);
            m_vertices[i].amplitude4
                = new Vector4(m_data.amplitude[i*16 + 12], m_data.amplitude[i*16 + 13], m_data.amplitude[i*16 + 14], m_data.amplitude[i*16 + 15]);
        }

        mesh.SetVertexBufferData(m_vertices, 0, 0, m_count);
        mesh.RecalculateBounds();
    }

}
