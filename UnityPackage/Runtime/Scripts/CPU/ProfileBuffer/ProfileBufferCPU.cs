

using System;
using WaterWaveSurface;

namespace ProfileBuffer
{
    internal class ProfileBufferCPU : IProfileBuffer
    {
        private IntPtr m_ptr;

        private float[] m_data;

        internal ProfileBufferCPU(IntPtr ptr)
        {
            m_ptr = ptr;
        }

        internal float Period => API.ProfileBuffer.profileBufferPeriod(m_ptr);

        internal float[] Data => m_data;

        internal unsafe void Update()
        {
            m_data = new float[API.ProfileBuffer.profileBufferDataSize(m_ptr) * 4];

            fixed (float* array = m_data)
            {
                API.ProfileBuffer.copyProfileBufferData(m_ptr, m_data);
            }
        }

        void IProfileBuffer.Update() => Update();
    }
}
