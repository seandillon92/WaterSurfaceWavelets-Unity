using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using WaveGrid;
using static UnityEditor.PlayerSettings;

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

        [SerializeField]
        private Implementation m_implementation;

        private RenderParams m_renderParams;

        internal Settings Settings { get { return m_settings; } }

        private void PrepareEnvironmentMaps()
        {
            var cam = new GameObject().AddComponent<Camera>();
            cam.orthographic = true;
            cam.transform.position = transform.position + Vector3.up* transform.lossyScale.y * 0.5f;
            cam.transform.rotation = Quaternion.Euler(90,0, -transform.rotation.eulerAngles.y);
            cam.transform.localScale = Vector3.one;
            cam.orthographicSize = m_settings.environment.size.y;
            cam.aspect = m_settings.environment.size.x / (float)m_settings.environment.size.y;
            cam.nearClipPlane = 0f;
            cam.farClipPlane = transform.lossyScale.y;

            var desc = new RenderTextureDescriptor();
            desc.useMipMap = false;
            desc.width = m_settings.environment.GetResolution();
            desc.height = desc.width;
            desc.volumeDepth = 1;
            desc.graphicsFormat = GraphicsFormat.None;
            desc.depthStencilFormat = GraphicsFormat.D32_SFloat_S8_UInt;
            desc.depthBufferBits = 32;
            desc.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            desc.msaaSamples = 1;
            desc.autoGenerateMips = false;

            var rt = new RenderTexture(desc); 
            if (!rt.Create())
            {
                Debug.LogError("Could not create depth texture");
            }

            cam.depthTextureMode = DepthTextureMode.Depth;
            cam.targetTexture = rt;
            cam.cullingMask = m_settings.environment.cullingMask;
            cam.RenderWithShader(m_settings.environment.material.shader, "");

            RenderEnvironmentMaps(cam);

            Destroy(cam.targetTexture);
            Destroy(cam.gameObject);
        }

        private void RenderEnvironmentMaps(Camera cam)
        {

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
            m_settings.environment.heights = heights;
            m_settings.environment.gradients = gradients;

            var shader = (ComputeShader)Resources.Load("Environment");
            shader.SetFloat("waterLevel", m_settings.environment.water_level);
            shader.SetFloat("position", m_settings.environment.transform.GetPosition().y);
            shader.SetFloat("size", transform.localScale.y * 0.5f);

            int kernelHandle = shader.FindKernel("Heights");
            shader.SetTexture(kernelHandle, "Read", cam.targetTexture);
            shader.SetTexture(kernelHandle, "Write", m_settings.environment.heights);
            shader.Dispatch(kernelHandle, cam.pixelWidth / 32, cam.pixelHeight / 32, 1);

            kernelHandle = shader.FindKernel("Gradients");
            shader.SetTexture(kernelHandle, "Read", m_settings.environment.heights);
            shader.SetTexture(kernelHandle, "Write", m_settings.environment.gradients);
            shader.Dispatch(kernelHandle, cam.pixelWidth / 32, cam.pixelHeight / 32, 1);
        }

        void Start()
        {
            if(m_settings.visualization.camera == null)
            {
                m_settings.visualization.camera = Camera.main;
            }

            m_settings.environment.size =
                new Vector2Int(
                    Mathf.RoundToInt(transform.localScale.x / 2),
                    Mathf.RoundToInt(transform.localScale.z / 2));

            m_settings.environment.transform = transform.localToWorldMatrix;

            PrepareEnvironmentMaps();

            switch (m_implementation)
            {
                case Implementation.CPU:
                    
                    m_grid = new WaveGridCPU(m_settings, m_settings.visualization.material);
                    
                    break;
                case Implementation.GPU:
                    m_grid = new WaveGridGPU(m_settings, m_settings.visualization.material);
                    
                    break;
            }

            m_renderParams = new RenderParams(m_settings.visualization.material);

            m_renderParams.camera = m_settings.visualization.camera;
            
            if (m_settings.visualization.material.GetTexture("_Skybox") == null)
            {
                m_settings.visualization.material.SetTexture("_Skybox", m_settings.visualization.skybox);
            }
            m_settings.visualization.material.name = "WaterSurfaceMaterial";
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
            var d_theta = 360f / 16f;
            for (int i = 0; i < 16; i++)
            {
                m_grid.AddPointDisturbance(new Vector3(pos.x,pos.y, (float)(i * d_theta)), value);
            }
        }

        /// <summary>
        /// Add disturbance at a point with a specific direction
        /// </summary>
        /// <param name="pos">global position coordinates</param>#
        /// <param name="direction"> direction of the distrurbance in global coordinates</param>
        /// 

        public void AddPointDirectionDisturbance(
            Vector3 pos, 
            Vector3 direction, 
            float value, 
            bool sideDirections = false)
        {
            if (!sideDirections)
            {
                AddPointDirectionDisturbance(pos, direction, value);
                return;
            }

            var d_theta = 1f/16;
            var dir_xz = new Vector3(direction.x,0, direction.z);
            var perpendicular = Vector3.Cross(dir_xz, Vector3.up);

            for (float theta = 0f; theta < 1f; theta+= d_theta)
            {
                var dir = Vector3.Slerp(-perpendicular, perpendicular, theta);
                AddPointDirectionDisturbance(pos, dir, value * d_theta);
            }
        }

        private void AddPointDirectionDisturbance(Vector3 pos, Vector3 direction, float value)
        {
            direction.y = 0;
            var angle = -Vector3.SignedAngle(Vector3.right, direction.normalized, Vector3.up);
            var position = new Vector3(pos.x, pos.z, angle);
            m_grid.AddPointDisturbance(position, value);
        }

        void OnDestroy()
        {
            m_grid.Dispose();
        }
    }
}
