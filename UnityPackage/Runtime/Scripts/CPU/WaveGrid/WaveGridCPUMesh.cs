using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

internal class WaveGridCPUMesh
{
    internal Mesh mesh { get; private set; }
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
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
        };

        var vdata = new Vertex[m_count];
        for (int i = 0; i < m_count; i++)
        {
            vdata[i].uv = data.uvs[i];
        }

        mesh.SetVertexBufferParams(m_count, layout);
        mesh.SetIndices(m_data.indices, MeshTopology.Quads, 0);
        mesh.SetVertexBufferData(vdata, 0, 0, m_count);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000000);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Vertex
    {
        public Vector3 pos;
        public Vector2 uv;
    }
}
