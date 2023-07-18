using UnityEngine;
using WaveGrid;

namespace WaterWaveSurface
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent (typeof(MeshRenderer))]
    public class WaterSurface : MonoBehaviour
    {
        [SerializeField]
        private Settings m_settings;

        [SerializeField]
        private UpdateSettings m_updateSettings;

        private IWaveGrid m_grid;

        private MeshFilter m_filter;
        private MeshRenderer m_renderer;

        internal Settings Settings { get { return m_settings; } }

        private void Awake()
        {
            m_filter = GetComponent<MeshFilter>();
            m_renderer = GetComponent<MeshRenderer>();
        }

        void Start()
        {
            m_grid = new WaveGridCPU(m_settings, m_renderer.sharedMaterial, m_filter);
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
        }

        void LateUpdate()
        {
            m_updateSettings.dt = Time.deltaTime;
            m_grid.Update(m_updateSettings);
        }
        /// <summary>
        /// Add disturbance at a point in all directions
        /// </summary>
        /// <param name="pos">x,y are position coordinates</param>
        public void AddPointDisturbance(Vector2 pos, float value)
        {
            //m_grid.AddPointDisturbance(pos, value);
        }

        /// <summary>
        /// Add disturbance at a point with a specific direction
        /// </summary>
        /// <param name="pos">x,y are position coordinates. z is the angle of the wave in radians</param>
        public void AddPointDirectionDisturbance(Vector3 pos, float value)
        {
            //m_grid.AddPointDisturbance(pos, value);
        }


        void OnDestroy()
        {
            m_grid.Dispose();
        }
    }
}
