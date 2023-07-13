using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Profiling;
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
    static readonly ProfilerMarker marker1 = new ProfilerMarker("Marker_1");
    static readonly ProfilerMarker marker2 = new ProfilerMarker("Marker_2");
    static readonly ProfilerMarker marker3 = new ProfilerMarker("Marker_3");
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
        private Matrix4x4 projectionMatrix_Inverse;
        private Vector2 translation;
        private Vector2 size;
        private float waterLevel;
        private bool renderOutsideBorders;
        public ProfilerMarker marker1;
        public ProfilerMarker marker2;
        public ProfilerMarker marker3;
        private float zeta;
        private Vector3 cameraPos;
        private Vector3 projMatrixForward;
        private Matrix4x4 cameraProjInverseMatrix;

        internal VerticesJob(
            WaveGrid grid,
            int grid_resolution,
            float multiplier,
            Matrix4x4 cameraMatrix,
            Matrix4x4 projectionMatrix,
            int direction,
            NativeArray<Vector3> positions,
            NativeArray<float> amplitudes,
            Vector2 translation,
            Vector2 size,
            float waterLevel,
            bool renderOutsideBorders,
            float zeta)
        {
            var projInverse = projectionMatrix.inverse;
            this.grid = grid.ptr;
            this.grid_resolution = grid_resolution;
            this.multiplier = multiplier;
            this.cameraMatrix = cameraMatrix;
            this.projectionMatrix = projectionMatrix;
            this.direction = direction;
            this.positions = positions;
            this.amplitudes = amplitudes;
            this.translation = translation;
            this.size = size;
            this.waterLevel = waterLevel;
            this.renderOutsideBorders = renderOutsideBorders;
            this.marker1 = WaterSurfaceMeshData.marker1;
            this.marker2 = WaterSurfaceMeshData.marker2;
            this.marker3 = WaterSurfaceMeshData.marker3;
            this.zeta = zeta;
            this.projectionMatrix_Inverse = projInverse;
            this.cameraPos = cameraMatrix.GetPosition();
            this.projMatrixForward = projectionMatrix.MultiplyPoint(Vector3.forward);
            this.cameraProjInverseMatrix = cameraMatrix * projInverse;
        }

        public void Execute(int index)
        {
            const float tau = 6.28318530718f;
            const float d_theta = tau / 16f;

            marker1.Begin();
            int ix = index / (grid_resolution + 1);
            int iy = index % (grid_resolution + 1);

            //Raycast
            Vector2 screenPos = new Vector2(
                ix * 2f / grid_resolution - 1f,
                iy * 2f / grid_resolution - 1f);

            //Matrix4x4 trans = cameraMatrix * projectionMatrix_Inverse;

            Vector3 point = new Vector3(screenPos[0], screenPos[1], 0) + this.projMatrixForward; //projectionMatrix.MultiplyPoint(Vector3.forward);
            point = cameraProjInverseMatrix.MultiplyPoint(point);
            Vector3 dir = (point - cameraPos).normalized;
            // End Raycast

            var camY = cameraPos.y - waterLevel;
            float t = -camY / dir.y;

            t = t < 0 ? 1000 : t;

            var position = cameraPos + t * dir;

            if (!renderOutsideBorders)
            {
                position.x = Mathf.Clamp(position.x, translation.x + -size.x, translation.x + size.x);
                position.z = Mathf.Clamp(position.z, translation.y + - size.y, translation.y + size.y);
            }

            position.y = waterLevel;

            positions[index] = position;
            for (int itheta = 0; itheta < 16; itheta++)
            {
                float theta = (itheta + 0.5f) * d_theta;

                Vector4 pos4 = 
                    new(position.x - translation.x, position.z - translation.y, theta, zeta);

                if (direction == -1 || direction == itheta)
                    amplitudes[index * 16 + itheta] = multiplier * API.Grid.amplitude(grid, pos4);
                else
                    amplitudes[index * 16 + itheta] = 0;
            }
            marker1.End();
        }
    }

    internal void SetVertices(
        WaveGrid grid, 
        int grid_resolution,
        float multiplier,
        Matrix4x4 cameraMatrix,
        Matrix4x4 projectionMatrix,
        Vector2 translation,
        Vector2 size,
        int direction,
        float waterLevel,
        bool renderOutsideBorders,
        float zeta)
    {
        var job = new VerticesJob(
            grid,
            grid_resolution,
            multiplier,
            cameraMatrix,
            projectionMatrix,
            direction,
            this.positions, 
            this.amplitude,
            translation,
            size,
            waterLevel, 
            renderOutsideBorders,
            zeta);

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
