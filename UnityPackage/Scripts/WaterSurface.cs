using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Audio;

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

        [SerializeField]
        private float m_distrurbance = 0.1f;

        private float logdt = -0.9f;

        private IntPtr m_grid;

        private WaterSurfaceMeshData m_data;
        private WaterSurfaceMesh m_mesh;
        private WaterSurfaceMeshRenderer m_meshRenderer;

        private MeshFilter m_filter;
        private MeshRenderer m_renderer;
        private Vector3? m_previous_distrurbance_position;

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

        float Angle(Vector3 v1, Vector3 v2)
        {
            var angle = Vector3.SignedAngle(v1, v2, Vector3.up);
            if (angle < 0)
            {
                angle = 360 + angle;
            }
            return angle;
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

            UpdateControls();

        }

        private void UpdateControls()
        {
            if (Input.GetMouseButton(0))
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                float t = -ray.origin.y / ray.direction.y;
                var pos = ray.origin + t * ray.direction;

                if (m_previous_distrurbance_position != null)
                {
                    float velocity = Vector3.Distance(m_previous_distrurbance_position.Value, pos) * Time.deltaTime;
                    var direction = (pos - m_previous_distrurbance_position).Value.normalized;

                    var angle1 = Angle(direction, Vector3.forward + Vector3.right * 0.5f);
                    var angle2 = Angle(direction, Vector3.back + Vector3.right * 0.5f);

                    API.Grid.addPointDisturbanceDirection(m_grid, new Vector3(pos.x, pos.z, angle1 * Mathf.Deg2Rad), m_distrurbance * velocity);
                    API.Grid.addPointDisturbanceDirection(m_grid, new Vector3(pos.x, pos.z, angle2 * Mathf.Deg2Rad), m_distrurbance * velocity);
                }

                m_previous_distrurbance_position = pos;
            }
            else
            {
                m_previous_distrurbance_position = null;
            }
        }

        void OnDestroy()
        {
            API.Grid.destroyGrid(m_grid);
            m_data.Dispose();
        }
    }
}
