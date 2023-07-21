using System;
using UnityEngine;

namespace WaveGrid
{
    [Serializable]
    internal class Settings
    {
        public float max_zeta = Mathf.Log(10, 2);
        public float min_zeta = Mathf.Log(0.03f, 2);

        [Delayed]
        public int n_x = 100;
        public int n_theta = 16;
        public int n_zeta = 1;
        public float initial_time = 100;
        public int spectrum_type = 1;
        public WaterTerrain terrain;
        public int visualizationResolution = 100;
        public Camera camera;

        public void OnValidate()
        {
            n_x = Mathf.ClosestPowerOfTwo(n_x);
        }
    }
}
