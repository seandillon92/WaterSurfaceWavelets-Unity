using System;
using UnityEngine;

namespace WaveGrid
{
    internal interface IWaveGrid : IDisposable
    {
        internal void Update(UpdateSettings settings);

        internal void AddPointDisturbance(Vector3 pos, float value);

        internal Mesh Mesh { get; }
    }

    [Serializable]
    internal class UpdateSettings
    {
        [HideInInspector]
        public float dt;

        public bool updateSimulation = true;
        public float amplitudeMultiplier = 4f;

        public bool renderOutsideBorders = true;
    }
}