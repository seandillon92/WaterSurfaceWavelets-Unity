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
        public float max_zeta = Mathf.Log(10, 2);
        public float min_zeta = Mathf.Log(0.03f, 2);

        [Delayed]
        public int n_x = 100;
        public int n_theta = 16;
        public int n_zeta = 1;
        public float initial_time = 100;
        public int spectrum_type = 1;

        public List<float> defaultAmplitude;

        public void OnValidate()
        {
            n_x = Mathf.ClosestPowerOfTwo(n_x);

            defaultAmplitude ??= new List<float>();

            while (defaultAmplitude.Count < 16)
            {
                defaultAmplitude.Add(0);
            }
            while (defaultAmplitude.Count > 16)
            {
                defaultAmplitude.RemoveAt(defaultAmplitude.Count - 1);
            }
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
