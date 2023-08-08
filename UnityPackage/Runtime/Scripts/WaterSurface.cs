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
        private Material m_material;

        [SerializeField]
        private Material m_depth_material;

        [SerializeField]
        private Settings m_settings;

        [SerializeField]
        private UpdateSettings m_updateSettings;

        private IWaveGrid m_grid;

        [SerializeField]
        private Implementation m_implementation;

        private RenderParams m_renderParams;

        [SerializeField]
        private Texture m_skybox;

        internal Settings Settings { get { return m_settings; } }
        
        private void CreateEnvironmentMaps()
        {
            var cam = m_settings.environmentCamera;
            var heights = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            heights.enableRandomWrite = true;
            if (!heights.Create())
            {
                Debug.LogError("Could not create height texture");
            }

            var gradients = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0, RenderTextureFormat.RGFloat, RenderTextureReadWrite.Linear);
            gradients.enableRandomWrite = true;
            if (!gradients.Create())
            {
                Debug.LogError("Could not create height gradients texture");
            }
            m_settings.terrain.heights = heights;
            m_settings.terrain.gradients = gradients;

            var shader = (ComputeShader)Resources.Load("Environment");
            shader.SetFloat("waterLevel", m_settings.terrain.water_level);
            shader.SetFloat("position", m_settings.terrain.transform.GetPosition().y);
            shader.SetFloat("size", m_settings.terrain.size.x);

            cam.depthTextureMode = DepthTextureMode.Depth;
            cam.RenderWithShader(m_depth_material.shader, "");

            int kernelHandle = shader.FindKernel("Heights");
            shader.SetTexture(kernelHandle, "Read", cam.targetTexture);
            shader.SetTexture(kernelHandle, "Write", m_settings.terrain.heights);
            shader.Dispatch(kernelHandle, cam.pixelWidth / 32, cam.pixelHeight / 32, 1);

            kernelHandle = shader.FindKernel("Gradients");
            shader.SetTexture(kernelHandle, "Read", m_settings.terrain.heights);
            shader.SetTexture(kernelHandle, "Write", m_settings.terrain.gradients);
            shader.Dispatch(kernelHandle, cam.pixelWidth / 32, cam.pixelHeight / 32, 1);

            Texture2D tex = new Texture2D(heights.width, heights.height, TextureFormat.RFloat, false);
            RenderTexture.active = heights;
            tex.ReadPixels(new Rect(0, 0, heights.width, heights.height), 0, 0);
            tex.Apply();

            Settings.terrain.heightsData = tex.GetPixelData<float>(0).ToArray();
        }

        void Start()
        {
            CreateEnvironmentMaps();

            switch (m_implementation)
            {
                case Implementation.CPU:
                    
                    m_grid = new WaveGridCPU(m_settings, m_material);
                    
                    break;
                case Implementation.GPU:
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

        private void Update()
        {
            Graphics.RenderMesh(m_renderParams, m_grid.Mesh, 0, Matrix4x4.identity);
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
