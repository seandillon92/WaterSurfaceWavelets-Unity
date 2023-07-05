using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using WaterWaveSurface;

internal class WaterSurfaceMeshData : IDisposable
{

    internal int size { get; private set; } = 10;
    internal NativeArray<float> amplitude { get; private set; }
    internal NativeArray<Vector3> positions { get; private set; }
    internal int[] indices { get; private set; }
    internal float[] profileBufferData { get; private set; }  
    internal float profileBufferPeriod { get; private set; }  
    internal bool hasProfileData => profileBufferData?.Length > 0;

    internal delegate void WaterSurfaceMeshDelegate(Vector3[] v, float[][] amplitudes, int index);
    internal WaterSurfaceMeshData(int size)
    {
        Init(size);
    }

    public unsafe void LoadProfile(IntPtr buffer)
    {
        this.profileBufferPeriod = API.ProfileBuffer.profileBufferPeriod(buffer);
        this.profileBufferData = new float[API.ProfileBuffer.profileBufferDataSize(buffer) * 4];

        fixed (float* array = this.profileBufferData)
        {
            API.ProfileBuffer.copyProfileBufferData(buffer, this.profileBufferData);
        }
        
    }

    private struct VerticesJob : IJobParallelFor
    {
        public NativeArray<Vector3> positions;
        [NativeDisableParallelForRestriction]
        public NativeArray<float> amplitudes;
        private int direction;
        [NativeDisableUnsafePtrRestriction]
        private IntPtr grid;
        private int grid_resolution;
        private float multiplier;
        private Matrix4x4 cameraMatrix;
        private Matrix4x4 projectionMatrix;

        internal VerticesJob(
            WaveGrid grid, 
            int grid_resolution, 
            float multiplier, 
            Matrix4x4 cameraMatrix, 
            Matrix4x4 projectionMatrix,
            int direction,
            NativeArray<Vector3> positions,
            NativeArray<float> amplitudes)
        {
            this.grid = grid.ptr;
            this.grid_resolution = grid_resolution;
            this.multiplier = multiplier;
            this.cameraMatrix = cameraMatrix;
            this.projectionMatrix = projectionMatrix;
            this.direction = direction;
            this.positions = positions;
            this.amplitudes = amplitudes;
        }

        (Vector3 dir, Vector3 camPos) CameraRayCast(Vector2 screenPos)
        {
            Matrix4x4 trans = cameraMatrix * projectionMatrix.inverse;

            Vector3 point = new Vector3(screenPos[0], screenPos[1], 0) + projectionMatrix.MultiplyPoint(Vector3.forward);
            point = trans.MultiplyPoint(point);
            Vector3 camPos = cameraMatrix.MultiplyPoint(Vector3.zero);
            Vector3 dir = (point - camPos).normalized;
            return (dir, camPos);
        }

        public void Execute(int index)
        {

            int ix = index / (grid_resolution + 1);
            int iy = index % (grid_resolution + 1);

            Vector2 screenPos = new Vector2(
                ix * 2f / grid_resolution - 1f,
                iy * 2f / grid_resolution - 1f);

            var raycast = CameraRayCast(screenPos);
            var dir = raycast.dir;
            var camPos = raycast.camPos;

            float t = -camPos.y / dir.y;

            t = t < 0 ? 1000 : t;

            var position = camPos + t * dir;

            position.y = 0;

            positions[index] = position;

            for (int itheta = 0; itheta < 16; itheta++)
            {
                float theta = API.Grid.idxToPos(grid, itheta, 2);
                Vector4 pos4 = new(position.x, position.z, theta, API.Grid.idxToPos(grid, 0, 3));

                if (direction == -1 || direction == itheta)
                    amplitudes[index * 16 + itheta] = multiplier * API.Grid.amplitude(grid, pos4);
                else
                    amplitudes[index * 16 + itheta] = 0;
            }

        }
    }

    internal void SetVertices(
        WaveGrid grid, 
        int grid_resolution,
        float multiplier,
        Matrix4x4 cameraMatrix,
        Matrix4x4 projectionMatrix,
        int direction)
    {
        var job = new VerticesJob(
            grid,
            grid_resolution,
            multiplier,
            cameraMatrix,
            projectionMatrix,
            direction,
            this.positions, 
            this.amplitude);

        var handle = job.Schedule(this.size * this.size, 1);
        handle.Complete();
    }

    private void Init(int size)
    {
        this.size = size + 1;
        var positions = new Vector3[this.size * this.size];
        this.indices = new int[(this.size - 1) * (this.size - 1) * 4];

        var delta = 2.0f / (this.size -1);
        for (var i = 0; i < this.size; i++)
        {
            for (var j = 0; j < this.size; j++)
            {
                positions[i * this.size + j] = new Vector3(-1f + i * delta, 0f,-1f + j * delta);
            }
        }

        this.positions = new NativeArray<Vector3>(this.size * this.size, Allocator.Persistent);
        this.positions.CopyFrom(positions);

        this.amplitude = new NativeArray<float>(this.size * this.size * 16, Allocator.Persistent);

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

    public void Dispose()
    {
        this.positions.Dispose();
        this.amplitude.Dispose();
    }
}
