using System.Linq;
using UnityEngine;
using WaveGrid;

namespace WaterWaveSurface
{
    public enum Implementation
    {
        CPU,
        GPU
    }

    public class WaterSurface : MonoBehaviour
    {
        [SerializeField]
        private Settings m_settings;

        [SerializeField]
        private UpdateSettings m_updateSettings;

        private IWaveGrid m_grid;

        private MeshFilter m_filter;
        private MeshRenderer m_renderer;

        [SerializeField]
        private Implementation m_implementation;

        private Material m_material;

        private RenderParams m_renderParams;

        [SerializeField]
        private Texture m_skybox;

        internal Settings Settings { get { return m_settings; } }

        private void Awake()
        {
            m_filter = GetComponent<MeshFilter>();
            m_renderer = GetComponent<MeshRenderer>();
        }

        private void Update()
        {
            Graphics.RenderMesh(m_renderParams, m_grid.Mesh, 0, Matrix4x4.identity);
        }

        void Start()
        {
            switch (m_implementation)
            {
                case Implementation.CPU:
                    m_material = new Material(Shader.Find("Unlit/WaterWaveSurfaces/waterSurfaceCPU"));
                    m_grid = new WaveGridCPU(m_settings, m_material);
                    
                    break;
                case Implementation.GPU:
                    m_material = new Material(Shader.Find("Unlit/WaterWaveSurfaces/waterSurfaceGPU"));
                    m_grid = new WaveGridGPU(m_settings, m_material);
                    
                    break;
            }

            m_renderParams = new RenderParams(m_material);

            m_renderParams.camera = m_settings.camera;
            m_material.SetTexture("_Skybox", m_skybox);
            m_material.SetFloat("_FresnelExponent", 1.0f);
            m_material.SetFloat("_RefractionIndex", 1.0f);
            m_material.name = "WaterSurfaceMaterial";
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

        private void OnValidate()
        {
            m_settings.OnValidate();
        }

        void OnDestroy()
        {
            m_grid.Dispose();
        }
    }
}
