using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaveGrid
{
    [Serializable]
    public class EnvironmentSettings
    {
        [HideInInspector]
        public Vector2Int size;
        [HideInInspector]
        public Matrix4x4 transform;

        public float water_level;
        [Delayed]
        public int resolution = 1024;

        public LayerMask cullingMask;
        public Material material;

        [HideInInspector]
        public RenderTexture heights;
        [HideInInspector]
        public RenderTexture gradients;

        internal void OnValidate()
        {
            resolution = Mathf.ClosestPowerOfTwo(resolution);
        }
    }

    [Serializable]
    public class VisualizationSettings
    {
        public int resolution = 100;
        public Camera camera;
        public Texture skybox;
        public Material material;
    }

    [Serializable]
    public class SimulationSettings
    {
        public float wind_strength = 10f;
        public float wind_direction = 0f;
        public float max_zeta = Mathf.Log(10, 2);
        public float min_zeta = Mathf.Log(0.03f, 2);

        [Delayed]
        public int n_x = 100;
        public int n_theta = 16;
        public int n_zeta = 1;
        public float initial_time = 100;
        public int spectrum_type = 1;

        public List<float> GetDefaultAmplitudes(Matrix4x4 transform)
        {
            var result = new List<float> {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0};

            var rotation = transform.rotation.eulerAngles.y;

            var correctedDir = (360 - wind_direction + rotation + 90) % 360;
            if (correctedDir <= 0)
            {
                correctedDir = 360 + correctedDir;
            }

            var iTheta = (correctedDir / 360f) * 16f - 1f;

            var t = Mathf.Abs(iTheta  % 1f);

            var lowItheta = (Mathf.FloorToInt(iTheta)%16 + 16) % 16;
            var highItheta = (Mathf.CeilToInt(iTheta)%16 + 16) % 16;

            if (lowItheta == highItheta)
            {
                result[lowItheta] = 0.1f;
            }
            else
            {
                result[lowItheta] = Mathf.Lerp(0f, 0.1f, 1-t);
                result[highItheta] = Mathf.Lerp(0, 0.1f, t);
            }

            return result;
        }

        public void OnValidate()
        {
            n_x = Mathf.ClosestPowerOfTwo(n_x);
        }
    }

    [Serializable]
    public class Settings
    {
        public SimulationSettings simulation;
        public EnvironmentSettings environment;
        public VisualizationSettings visualization;
        internal void OnValidate()
        {
            environment.OnValidate();
            simulation.OnValidate();
        }
    }
}
