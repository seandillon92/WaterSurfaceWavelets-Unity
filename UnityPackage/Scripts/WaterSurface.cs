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
        private float m_max_zeta = 0.01f;

        [SerializeField]
        private float m_min_zeta = 10;

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

        private float logdt = -0.9f;

        private IntPtr m_grid;

        private WaterSurfaceMeshData m_data;
        private WaterSurfaceMesh m_mesh;
        private MeshFilter m_filter;
        private MeshRenderer m_renderer;

        (Vector3 dir, Vector3 camPos) CameraRayCast(Vector2 screenPos, Camera cam)
        {
            Matrix4x4 camTrans = cam.transform.localToWorldMatrix;
            Matrix4x4 camProj = cam.projectionMatrix;
            Matrix4x4 trans = camTrans * camProj.inverse;

            Vector3 point = new Vector3(screenPos[0], screenPos[1], 0) + camProj.MultiplyPoint(Vector3.forward);
            point = trans.MultiplyPoint(point);
            Vector3 camPos = camTrans.MultiplyPoint(Vector3.zero);
            Vector3 dir = (point - camPos).normalized;
            return (dir, camPos);
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

            m_filter = GetComponent<MeshFilter>();
            m_renderer = GetComponent<MeshRenderer>();

            m_filter.sharedMesh = m_mesh.mesh;

            Debug.Log(m_grid);
        }

        void Update()
        {
            if (m_update_simulation)
            {
                m_data.SetVertices((Vector3[] positions, float[][] amplitudes, int index) =>
                {
                    var position = positions[index];
                    var amplitude = amplitudes[index];

                    int ix = index / (m_visualization_grid_resolution + 1);
                    int iy = index % (m_visualization_grid_resolution + 1);
                    
                    Vector2 screenPos = new Vector2(
                        ix * 2f / m_visualization_grid_resolution - 1f, 
                        iy * 2f / m_visualization_grid_resolution - 1f);

                    var cam = Camera.main;

                    var raycast = CameraRayCast(screenPos, cam);
                    var dir = raycast.dir;
                    var camPos = raycast.camPos;
                   
                    float t = -camPos.y / dir.y;

                    t = t < 0 ? 100 : t;
                    
                    position = camPos + t * dir;

                    position.y = 0;

                    positions[index] = position;

                    for (int itheta = 0; itheta < 16; itheta++)
                    {
                        float theta = API.Grid.idxToPos(m_grid, itheta, 2);
                        Vector4 pos4 = new(position.x, position.y, theta, API.Grid.idxToPos(m_grid, 0, 3));

                        if (m_direction_to_show == -1 || m_direction_to_show== itheta)
                            amplitude[itheta] = m_amplitude_mult * API.Grid.amplitude(m_grid, pos4);
                        else
                            amplitude[itheta] = 0;
                    }

                });
            }

            m_data.LoadProfile(API.Grid.getProfileBuffer(m_grid, 0));

            m_mesh.Update();
            
            API.Grid.timeStep(m_grid, API.Grid.clfTimeStep(m_grid) * (float)Math.Pow(10, logdt), m_update_simulation);
        }

        void OnDestroy()
        {
            API.Grid.destroyGrid(m_grid);
        }
    }
}
