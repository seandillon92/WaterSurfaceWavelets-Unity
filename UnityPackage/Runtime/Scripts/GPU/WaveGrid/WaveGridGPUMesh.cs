
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using UnityEngine;
namespace WaveGrid
{
    public class WaveGridGPUMesh
    {
        internal Mesh mesh { get;private set; }

        internal WaveGridGPUMesh(Settings settings)
        {
            //Set Vertex Buffer Params
            var resolution = settings.visualization.resolution;
            var count = resolution * resolution;
            
            mesh = new Mesh();
            var layout = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2)
            };

            mesh.SetVertexBufferParams(count, layout);

            //Set Indices
            var indices = new int[(resolution - 1) * (resolution -1) * 4];
            for (var i = 0; i < resolution - 1; i++)
            {
                for (var j = 0; j < resolution - 1; j++)
                {
                    indices[i * (resolution - 1) * 4 + j * 4] = i * resolution + j;
                    indices[i * (resolution - 1) * 4 + j * 4 + 1] = i * resolution + j + 1;
                    indices[i * (resolution - 1) * 4 + j * 4 + 2] = (i + 1) * resolution + j + 1;
                    indices[i * (resolution - 1) * 4 + j * 4 + 3] = (i + 1) * resolution + j;
                }
            }
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetIndices(indices, MeshTopology.Quads, 0);

            // Set Vertex Data
            var vertices = new Vertex[count];

            var delta = 2.0f / (resolution-1);
            for (var i = 0; i < resolution; i++)
            {
                for (var j = 0; j < resolution; j++)
                {
                    vertices[i * resolution + j].uv = new Vector2(-1f + i * delta, -1f + j * delta);
                    vertices[i * resolution + j].pos = new Vector3(-1f + i * delta, -1f + j * delta);
                }
            }


            mesh.SetVertexBufferData(vertices, 0, 0, count);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000000);
        }



        [StructLayout(LayoutKind.Sequential)]
        private struct Vertex
        {
            public Vector3 pos;
            public Vector2 uv;
        }
    }
}
