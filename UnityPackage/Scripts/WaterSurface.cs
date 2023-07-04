using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace WaterWaveSurface
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent (typeof(MeshRenderer))]
    public class WaterSurface : MonoBehaviour
    {
        [SerializeField]
        private int m_size = 50;

        [SerializeField]
        private float m_max_zeta = Mathf.Log(10, 2);

        [SerializeField]
        private float m_min_zeta = Mathf.Log(0.03f, 2);

        [SerializeField]
        private int m_n_x = 100;

        [SerializeField]
        private int m_n_theta = 16;

        [SerializeField]
        private int m_n_zeta = 1;

        [SerializeField]
        private int m_initial_time = 100;

        [SerializeField]
        private int m_spectrum_type = 1;

        [SerializeField]
        private bool m_update_simulation = true;

        [SerializeField]
        private int m_visualization_grid_resolution = 10;

        [SerializeField]
        private int m_direction_to_show = -1;

        [SerializeField]
        float m_amplitude_mult = 4.0f;

        [SerializeField]
        Light m_directional_light;

        private float logdt = -0.9f;

        private IntPtr m_grid;

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

        // Start is called before the first frame update
        void Start()
        {
            m_grid = API.Grid.createGrid(
                m_size, 
                m_max_zeta, 
                m_min_zeta, 
                m_n_x, 
                m_n_theta, 
                m_n_zeta, 
                m_initial_time, 
                m_spectrum_type);

            m_data = new WaterSurfaceMeshData(m_visualization_grid_resolution);
            m_mesh = new WaterSurfaceMesh(m_data);

            m_meshRenderer = new WaterSurfaceMeshRenderer(m_data, m_renderer.sharedMaterial, m_directional_light);

            m_filter.sharedMesh = m_mesh.mesh;

            Debug.Log(m_grid);
        }

        void Update()
        {
            if (m_update_simulation)
            {
                m_data.SetVertices(
                    m_grid,
                    m_visualization_grid_resolution,
                    m_amplitude_mult, 
                    Camera.main.transform.localToWorldMatrix,
                    Camera.main.projectionMatrix,
                    m_direction_to_show);

                m_data.LoadProfile(API.Grid.getProfileBuffer(m_grid, 0));

                m_mesh.Update();
                m_meshRenderer.Update();
            }
            
            API.Grid.timeStep(m_grid, API.Grid.clfTimeStep(m_grid) * (float)Math.Pow(10, logdt), m_update_simulation);
        }

        void OnDestroy()
        {
            API.Grid.destroyGrid(m_grid);
            m_data.Dispose();
        }
    }
}
