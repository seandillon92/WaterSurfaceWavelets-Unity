using System;
using UnityEngine;
using WaterWaveSurface;

internal class WaveGrid : IDisposable
{
    private IntPtr m_grid;

    internal IntPtr ptr => m_grid;

    [Serializable]
    internal class Settings
    {
        public float max_zeta = Mathf.Log(10, 2);
        public float min_zeta = Mathf.Log(0.03f, 2);
        public int n_x = 100;
        public int n_theta = 16;
        public int n_zeta = 1;
        public float initial_time = 100;
        public int spectrum_type = 1;
        public WaterTerrain terrain;
    }

    internal WaveGrid(Settings settings)
    {
        m_grid = API.Grid.createGrid(
            settings.terrain.size.x,
            settings.max_zeta,
            settings.min_zeta,
            settings.n_x,
            settings.n_theta,
            settings.n_zeta,
            settings.initial_time,
            settings.spectrum_type,
            settings.terrain.heights, 
            settings.terrain.heights.Length);
    }

    internal float ClfTimeStep()
    {
        return API.Grid.clfTimeStep(m_grid);
    }

    internal void Timestep(float dt, bool fullUpdate)
    {
        API.Grid.timeStep(m_grid, dt, fullUpdate);
    }

    internal float Amplitude(Vector4 pos)
    {
        return API.Grid.amplitude(m_grid, pos);
    }

    internal float IdxToPos(int idx, int dim)
    {
        return API.Grid.idxToPos(m_grid, idx, dim);
    }

    internal IntPtr GetProfileBuffer(int index)
    {
        return API.Grid.getProfileBuffer(m_grid, index);
    }

    internal void AddPointDisturbance(Vector3 pos, float value)
    {
        API.Grid.addPointDisturbanceDirection(m_grid, pos, value);
    }

    internal void AddPointDisturbance(Vector2 pos, float value)
    {
        API.Grid.addPointDisturbance(m_grid, pos, value);
    }

    internal float GetTerrainHeight(Vector2 pos)
    {
        return API.Grid.levelSet(m_grid, pos);
    }

    public void Dispose()
    {
        API.Grid.destroyGrid(m_grid);
    }
}
