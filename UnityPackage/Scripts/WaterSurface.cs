using System;
using UnityEngine;

namespace WaterWaveSurface
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent (typeof(MeshRenderer))]
    public class WaterSurface : MonoBehaviour
    {
        [SerializeField]
        private WaveGrid.Settings m_settings;

        [SerializeField]
        private bool m_updateSimulation = true;

        [SerializeField]
        private int m_visualizationGridResolution = 100;

        [SerializeField]
        private int m_directionToShow = -1;

        [SerializeField]
        float m_amplitudeMultiplier = 4.0f;

        private float logdt = -0.9f;

        private WaveGrid m_grid;

        private WaterSurfaceMeshData m_data;
        private WaterSurfaceMesh m_mesh;
        private WaterSurfaceMeshRenderer m_meshRenderer;

        private MeshFilter m_filter;
        private MeshRenderer m_renderer;

        private void Awake()
        {
            m_filter = GetComponent<MeshFilter>();
            m_renderer = GetComponent<MeshRenderer>();
        }


        void Start()
        {
            m_grid = new WaveGrid(m_settings);

            m_data = new WaterSurfaceMeshData(m_visualizationGridResolution);
            m_mesh = new WaterSurfaceMesh(m_data);

            m_meshRenderer = new WaterSurfaceMeshRenderer(m_data, m_renderer.sharedMaterial);

            m_filter.sharedMesh = m_mesh.mesh;
        }

        void LateUpdate()
        {
            if (m_updateSimulation)
            {
                m_data.SetVertices(
                    m_grid,
                    m_visualizationGridResolution,
                    m_amplitudeMultiplier,
                    Camera.main.transform.localToWorldMatrix,
                    Camera.main.projectionMatrix,
                    m_directionToShow);
            }

            m_data.LoadProfile(m_grid.GetProfileBuffer(0));

            m_mesh.Update();
            m_meshRenderer.Update();

            m_grid.Timestep(m_grid.ClfTimeStep() * (float)Math.Pow(10, logdt), m_updateSimulation);
        }
        /// <summary>
        /// Add disturbance at a point in all directions
        /// </summary>
        /// <param name="pos">x,y are position coordinates</param>
        public void AddPointDisturbance(Vector2 pos, float value)
        {
            m_grid.AddPointDisturbance(pos, value);
        }

        /// <summary>
        /// Add disturbance at a point with a specific direction
        /// </summary>
        /// <param name="pos">x,y are position coordinates. z is the angle of the wave in radians</param>
        public void AddPointDirectionDisturbance(Vector3 pos, float value)
        {
            m_grid.AddPointDisturbance(pos, value);
        }

        /// <summary>
        /// Get the height of the terrain (ocean bed or land).
        /// </summary>
        public float GetTerrainHeight(Vector2 pos)
        {
            return m_grid.GetTerrainHeight(pos);
        }


        void OnDestroy()
        {
            m_grid.Dispose();
            m_data.Dispose();
        }
    }
}
