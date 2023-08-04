﻿using System;
using UnityEngine;

namespace WaveGrid
{
    internal interface IWaveGrid : IDisposable
    {
        internal void Update(UpdateSettings settings);
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

        public Texture environment;
    }
}