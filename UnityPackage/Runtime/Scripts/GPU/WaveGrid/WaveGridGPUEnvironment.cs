using UnityEngine;
using WaterWaveSurface;

namespace WaveGrid
{
    internal class WaveGridGPUEnvironment
    {
        internal Texture2D heights { get; private set; }
        internal Texture2D gradients { get; private set; }

        internal WaveGridGPUEnvironment(Settings settings)
        {
            //Create the environment textures
            var heightsSize = Mathf.RoundToInt(Mathf.Sqrt(settings.terrain.heights.Length));
            heights =
                new Texture2D(heightsSize, heightsSize, TextureFormat.RFloat, false, true);
            heights.wrapMode = TextureWrapMode.Clamp;
            heights.filterMode = FilterMode.Bilinear;
            heights.SetPixelData(settings.terrain.heights, 0);
            heights.Apply(updateMipmaps:false);
            
            this.gradients = new Texture2D(heightsSize, heightsSize, TextureFormat.RGFloat, false, true);
            var gradients = new float[(heightsSize) * (heightsSize) * 2];
            for (int i = 0; i < heightsSize - 1; i++)
            {
                for (int j = 0; j < heightsSize - 1; j++)
                {
                    var height = settings.terrain.heights[i * heightsSize + j];
                    var height_x = settings.terrain.heights[i * heightsSize + j + 1];
                    var height_y = settings.terrain.heights[(i + 1) * heightsSize + j];
                    Vector2 gradient = new Vector2(height_x - height, height_y - height).normalized;

                    gradients[(i * heightsSize + j) * 2] = gradient.x;
                    gradients[(i * heightsSize + j) * 2 + 1] = gradient.y;
                }
            }
            this.gradients.SetPixelData(gradients, 0);
            this.gradients.wrapMode = TextureWrapMode.Clamp;
            this.gradients.filterMode = FilterMode.Bilinear;
            this.gradients.Apply(updateMipmaps:false);
        }

    }
}
