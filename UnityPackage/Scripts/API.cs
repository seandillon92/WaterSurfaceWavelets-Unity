using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using static WaterWaveSurface.API;

namespace WaterWaveSurface
{
    public class API
    {
        public const string name =
#if UNITY_IOS
            "__internal";
#else
            "watersurfacewavelets";
#endif
        public class Grid
        {
            [DllImport(name)]
            public static extern IntPtr createGrid(
                float size,
                float max_zeta,
                float min_zeta,
                int n_x,
                int n_theta,
                int n_zeta,
                float initial_time,
                int spectumType);

            [DllImport(name)]
            public static extern void destroyGrid(IntPtr grid);

            [DllImport(name)]
            public static extern void timeStep(IntPtr grid, float dt, bool fullUpdate);

            [DllImport(name)]
            public static extern float clfTimeStep(IntPtr grid);

            [DllImport(name)]
            public static extern long profileBuffersSize(IntPtr grid);

            [DllImport(name)]
            public static extern IntPtr getProfileBuffer(IntPtr grid, int index);

            [DllImport(name)]
            public static extern float idxToPos(IntPtr grid, int idx, int dim);

            [DllImport(name)]
            public static extern float amplitude(IntPtr grid, Vector4 pos4);
        }

        public class ProfileBuffer
        {
            [DllImport(name)]
            public static extern uint profileBufferDataSize(IntPtr buffer);

            [DllImport(name)]
            public static extern void copyProfileBufferData(IntPtr buffer, float[] dest);

            [DllImport(name)]
            public static extern float profileBufferPeriod(IntPtr buffer);
        }
    }
}
