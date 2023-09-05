using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaveGrid
{
    public enum Resolution
    {
        R_128 = 128,
        R_256 = 256,
        R_512 = 512,
        R_1024 = 1024,
        R_2048 = 2048,
        R_4096 = 4096,
        R_8192 = 8192,
        R_16384 = 16384,
    }

    [Serializable]
    public class TerrainSettings
    {
        [HideInInspector]
        public Vector2Int size;
        [HideInInspector]
        public Matrix4x4 transform;

        public float water_level;

        [SerializeField]
        private Resolution resolution;

        public int GetResolution()
        {
            return (int)resolution;
        }

        public LayerMask cullingMask;
        public Material material;

        [HideInInspector]
        public RenderTexture heights;
        [HideInInspector]
        public RenderTexture gradients;
    }

    [Serializable]
    public class BoatSettings
    {
        public GameObject boat;
        public LayerMask cullingMask;
        //[HideInInspector]
        public RenderTexture heights;
        public RenderTexture gradients;
        public Material material;

        [SerializeField]
        private Resolution resolution;

        public int GetResolution()
        {
            return (int)resolution;
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
        public float wind_speed = 10;
        [HideInInspector]
        public float wind_direction = 0f;
        [Range(0,1)]
        public float wave_amplitude = 1f;
        [Range(0.9f,1)]
        public float dissipation = 0.99f;
        [HideInInspector]
        public float max_zeta = Mathf.Log(10, 2);
        [HideInInspector]
        public float min_zeta = Mathf.Log(0.03f, 2);

        [SerializeField]
        private Resolution resolution = Resolution.R_128;
        public int GetResolution()
        {
            return (int)resolution;
        }
        [HideInInspector]
        public int n_theta = 16;
        [HideInInspector]
        public int n_zeta = 1;
        [HideInInspector]
        public float initial_time = 100;

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
                result[lowItheta] = wave_amplitude;
            }
            else
            {
                result[lowItheta] = Mathf.Lerp(0f, wave_amplitude, 1-t);
                result[highItheta] = Mathf.Lerp(0, wave_amplitude, t);
            }

            return result;
        }
    }

    [Serializable]
    public class Settings
    {
        public SimulationSettings simulation;
        public TerrainSettings environment;
        public BoatSettings boat;
        public VisualizationSettings visualization;
    }
}
