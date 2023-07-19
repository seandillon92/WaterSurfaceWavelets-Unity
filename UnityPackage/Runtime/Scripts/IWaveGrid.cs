using System;
using UnityEngine;

namespace WaveGrid
{
    internal interface IWaveGrid : IDisposable
    {
        internal void Update(UpdateSettings settings);
    }

    [Serializable]
    internal class UpdateSettings
    {
        [HideInInspector]
        public float dt;

        public bool updateSimulation = true;
        public float amplitudeMultiplier = 4f;

        public int direction = -1;
        public bool renderOutsideBorders = true;
    }
}