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

            // Set Vertex Data
            var vertices = new Vector3[count];

            var delta = 2.0f / (resolution-1);
            for (var i = 0; i < resolution; i++)
            {
                for (var j = 0; j < resolution; j++)
                {
                    vertices[i * resolution + j] = new Vector3(-1f + i * delta, -1f + j * delta, 0f);
                }
            }
            mesh.SetVertices(vertices);
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetIndices(indices, MeshTopology.Quads, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000000);
        }
    }
}
