using UnityEngine;
using WaveGrid;
using ProfileBuffer;
using UnityEngine.UIElements;

internal class WaveGridCPUData
{

    internal int size { get; private set; } = 10;
    internal int[] indices { get; private set; }

    internal Vector2[] uvs { get; private set; }

    internal float[] profileBufferData { get; private set; }  
    internal float profileBufferPeriod { get; private set; }  
    internal bool hasProfileData => profileBufferData?.Length > 0;
    internal bool hasAmplitudeData => amplitudes?.Length > 0;

    internal float[] amplitudes { get; private set; }

    internal Vector3Int amplitudesSize { get;private set; }

    private Settings m_settings;

    internal WaveGridCPUData(Settings s)
    {
        m_settings = s;
        amplitudesSize = new Vector3Int(s.simulation.n_x, s.simulation.n_x, s.simulation.n_theta);
        Init(s.visualization.resolution);
    }
    private void Init(int size)
    {
        this.size = size;
        this.uvs = new Vector2[this.size * this.size];
        this.indices = new int[(this.size - 1) * (this.size - 1) * 4];

        var delta = 2.0f / (this.size - 1);
        for (var i = 0; i < this.size; i++)
        {
            for (var j = 0; j < this.size; j++)
            {
                this.uvs[i * this.size + j] = new Vector2(-1f + i * delta, -1f + j * delta);
            }
        }

        for (var i = 0; i < this.size - 1; i++)
        {
            for (var j = 0; j < this.size - 1; j++)
            {
                this.indices[i * (this.size - 1) * 4 + j * 4] = i * this.size + j;
                this.indices[i * (this.size - 1) * 4 + j * 4 + 1] = i * this.size + j + 1;
                this.indices[i * (this.size - 1) * 4 + j * 4 + 2] = (i + 1) * this.size + j + 1;
                this.indices[i * (this.size - 1) * 4 + j * 4 + 3] = (i + 1) * this.size + j;
            }
        }

        this.amplitudes = 
            new float[
            m_settings.simulation.n_x * m_settings.simulation.n_x * m_settings.simulation.n_theta];
    }

    internal void LoadProfileBufferData(ProfileBufferCPU buffer)
    {
        this.profileBufferPeriod = buffer.Period;
        this.profileBufferData = buffer.Data;
    }

    internal void LoadAmplitudeData(WaveGridCPU grid)
    {
        grid.GetAmplitudeData(amplitudes);
    }
}
