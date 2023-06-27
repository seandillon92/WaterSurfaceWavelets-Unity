using System;
using UnityEngine;
using WaterWaveSurface;

public class WaterSurfaceMeshData
{

    public int size { get; private set; } = 10;
    public float[][] amplitude { get; private set; }
    public Vector3[] positions { get; private set; }
    public int[] indices { get; private set; }
    public float[] profileBufferData { get; private set; }  
    public float profileBufferPeriod { get; private set; }  

    public delegate void WaterSurfaceMeshDelegate(Vector3[] v, float[][] amplitudes, int index);
    public WaterSurfaceMeshData(int size)
    {
        Init(size);
    }

    public unsafe void LoadProfile(IntPtr buffer)
    {
        this.profileBufferPeriod = API.ProfileBuffer.profileBufferPeriod(buffer);
        this.profileBufferData = new float[API.ProfileBuffer.profileBufferDataSize(buffer)];

        fixed (float* array = this.profileBufferData)
        {
            API.ProfileBuffer.copyProfileBufferData(buffer, this.profileBufferData);
        }
        
    }

    public void SetVertices(WaterSurfaceMeshDelegate func)
    {
        for (int i = 0; i < size * size; i++)
        {
            func(this.positions, this.amplitude, i);
        }
    }

    private void Init(int size)
    {
        this.size = size + 1;
        this.amplitude = new float[this.size * this.size][];
        for (int i = 0; i < this.amplitude.Length; i++)
        {
            this.amplitude[i] = new float[16];
        }

        this.positions = new Vector3[this.size * this.size];
        this.indices = new int[(this.size - 1) * (this.size - 1) * 4];
        for (var i = 0; i < this.size; i++)
        {
            for (var j = 0; j < this.size; j++)
            {
                this.positions[i * this.size + j] = new Vector3(i, 0f, j);
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
    }
}
