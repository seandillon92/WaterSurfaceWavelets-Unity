using UnityEngine;
using UnityEngine.Experimental.Rendering;
using WaveGrid;

namespace WaterWaveSurface
{
    public class WaterSurface : MonoBehaviour
    {
        [SerializeField]
        private Settings m_settings;

        [SerializeField]
        private UpdateSettings m_updateSettings;

        private IWaveGrid m_grid;

        private RenderParams m_renderParams;

        internal Settings Settings { get { return m_settings; } }

        private void PrepareEnvironmentMaps()
        {
            var cam = new GameObject().AddComponent<Camera>();
            cam.orthographic = true;
            cam.transform.position = transform.position + Vector3.up * transform.lossyScale.y * 0.5f;
            cam.transform.rotation = Quaternion.Euler(90, 0, -transform.rotation.eulerAngles.y);
            cam.transform.localScale = Vector3.one;
            cam.orthographicSize = m_settings.environment.size.y;
            cam.aspect = m_settings.environment.size.x / (float)m_settings.environment.size.y;
            cam.nearClipPlane = 0f;
            cam.farClipPlane = transform.lossyScale.y;

            var desc = CreateDepthTextureDesc(
                m_settings.environment.GetResolution(),
                m_settings.environment.GetResolution());

            var rt = new RenderTexture(desc);
            if (!rt.Create())
            {
                Debug.LogError("Could not create depth texture");
            }

            cam.depthTextureMode = DepthTextureMode.Depth;
            cam.targetTexture = rt;
            cam.cullingMask = m_settings.environment.cullingMask;
            cam.RenderWithShader(m_settings.environment.material.shader, "");

            var maps = 
                RenderHeightGradient(
                    cam, 
                    m_settings.environment.transform.GetPosition().y, 
                    transform.lossyScale.y * 0.5f);

            m_settings.environment.heights = maps.heights;
            m_settings.environment.gradients = maps.gradients;

            Destroy(cam.targetTexture);
            Destroy(cam.gameObject);
        }

        private void PrepareBoatMaps()
        {
            var boat = m_settings.boat.boat;
            var mesh = boat.GetComponent<MeshFilter>().sharedMesh;
            var height = mesh.bounds.size.x;
            var width = mesh.bounds.size.z;
            var ratio = width / height;

            var cam = new GameObject().AddComponent<Camera>();
            cam.orthographic = true;
            var position = boat.transform.position;
            position.y = boat.transform.TransformPoint(mesh.bounds.min).y;
            cam.transform.position = position;
            
            cam.transform.rotation = Quaternion.Euler(-90, boat.transform.rotation.eulerAngles.y + 90, 0);
            cam.transform.localScale = Vector3.one;
            cam.orthographicSize = height / 2f;
            cam.aspect = ratio;
            cam.nearClipPlane = 0f;
            cam.farClipPlane = boat.transform.lossyScale.y * mesh.bounds.size.y;

            var height_res = m_settings.boat.GetResolution();
            var width_res = Mathf.ClosestPowerOfTwo((int)(height_res * ratio));
            var desc = CreateDepthTextureDesc(width_res, height_res);

            var rt = new RenderTexture(desc);
            if (!rt.Create())
            {
                Debug.LogError("Could not create texture");
                return;
            }

            cam.depthTextureMode = DepthTextureMode.Depth;
            cam.targetTexture = rt;
            cam.cullingMask = m_settings.boat.cullingMask;
            cam.RenderWithShader(m_settings.boat.material.shader, "");

            var center = boat.transform.TransformPoint(mesh.bounds.center);
            var maps = RenderHeightGradient(cam, center.y, mesh.bounds.extents.y);

            m_settings.boat.heights = maps.heights;
            m_settings.boat.gradients = maps.gradients;

            Destroy(cam.targetTexture);
            Destroy(cam.gameObject);
        }

        private RenderTextureDescriptor CreateDepthTextureDesc(int width, int height)
        {
            var desc = new RenderTextureDescriptor();
            desc.useMipMap = false;
            desc.width = width;
            desc.height = height;
            desc.volumeDepth = 1;
            desc.graphicsFormat = GraphicsFormat.None;
            desc.depthStencilFormat = GraphicsFormat.D32_SFloat_S8_UInt;
            desc.depthBufferBits = 32;
            desc.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            desc.msaaSamples = 1;
            desc.autoGenerateMips = false;
            return desc;
        }

        private (RenderTexture heights, RenderTexture gradients) 
            RenderHeightGradient(Camera cam, float position, float size)
        {

            var heights = 
                new RenderTexture(
                    cam.pixelWidth, 
                    cam.pixelHeight, 
                    0, 
                    RenderTextureFormat.RFloat, 
                    RenderTextureReadWrite.Linear);

            heights.enableRandomWrite = true;
            if (!heights.Create())
            {
                Debug.LogError("Could not create height texture");
            }

            var gradients = 
                new RenderTexture(
                    cam.pixelWidth, 
                    cam.pixelHeight, 
                    0, 
                    RenderTextureFormat.RGFloat, 
                    RenderTextureReadWrite.Linear);

            gradients.enableRandomWrite = true;
            if (!gradients.Create())
            {
                Debug.LogError("Could not create gradients texture");
            }

            var shader = (ComputeShader)Resources.Load("Environment");
            shader.SetFloat("waterLevel", m_settings.environment.water_level);
            shader.SetFloat("position", m_settings.environment.transform.GetPosition().y);
            shader.SetFloat("size", transform.localScale.y * 0.5f);

            int kernelHandle = shader.FindKernel("Heights");
            shader.SetTexture(kernelHandle, "Read", cam.targetTexture);
            shader.SetTexture(kernelHandle, "Write", heights);
            shader.Dispatch(kernelHandle, cam.pixelWidth / 32, cam.pixelHeight / 32, 1);

            kernelHandle = shader.FindKernel("Gradients");
            shader.SetTexture(kernelHandle, "Read", heights);
            shader.SetTexture(kernelHandle, "Write", gradients);
            shader.Dispatch(kernelHandle, cam.pixelWidth / 32, cam.pixelHeight / 32, 1);

            return (heights, gradients);
        }

        void Start()
        {
            if (m_settings.visualization.camera == null)
            {
                m_settings.visualization.camera = Camera.main;
            }

            m_settings.environment.size =
                new Vector2Int(
                    Mathf.RoundToInt(transform.localScale.x / 2),
                    Mathf.RoundToInt(transform.localScale.z / 2));

            m_settings.environment.transform = transform.localToWorldMatrix;

            PrepareEnvironmentMaps();
            if (m_settings.boat.useBoat)
            {
                PrepareBoatMaps();
            }
            InitializeReflections();
            InitializeAmplitude();

            m_grid = new WaveGridGPU(m_settings, m_settings.visualization.material);

            m_renderParams = new RenderParams(m_settings.visualization.material);

            m_renderParams.camera = m_settings.visualization.camera;
            m_settings.visualization.material.name = "WaterSurfaceMaterial";

            if (m_settings.simulation.stored_amplitude != null)
            {
                Graphics.CopyTexture(m_settings.simulation.stored_amplitude, m_settings.simulation.amplitude);
            }
        }

        private void InitializeAmplitude()
        {
            // Create amplitude render textures
            RenderTextureDescriptor renderTextureDescriptor = new RenderTextureDescriptor();
            renderTextureDescriptor.useMipMap = false;
            renderTextureDescriptor.width = m_settings.simulation.GetResolution();
            renderTextureDescriptor.height = m_settings.simulation.GetResolution();
            renderTextureDescriptor.volumeDepth = 16;
            renderTextureDescriptor.enableRandomWrite = true;
            renderTextureDescriptor.colorFormat = RenderTextureFormat.RFloat;
            renderTextureDescriptor.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            renderTextureDescriptor.msaaSamples = 1;
            renderTextureDescriptor.sRGB = false;
            renderTextureDescriptor.autoGenerateMips = false;

            m_settings.simulation.amplitude = new RenderTexture(renderTextureDescriptor);
        }

        private void InitializeReflections()
        {
            var resolution = m_settings.reflection.GetResolution();
            var rt = new RenderTexture(resolution, resolution, 1);
            rt.dimension = UnityEngine.Rendering.TextureDimension.Cube;
            rt.hideFlags = HideFlags.HideAndDontSave;
            m_settings.reflection.texture_lights = rt;
            m_settings.reflection.texture_noLights = new RenderTexture(rt);
            var cam = new GameObject().AddComponent<Camera>();
            cam.transform.SetParent(m_settings.visualization.camera.transform, false);
            cam.CopyFrom(m_settings.visualization.camera);
            cam.cullingMask = m_settings.reflection.cullingMask;
            cam.enabled = false;
            cam.gameObject.SetActive(false);
            cam.name = "ReflectionsCamera";
            m_settings.reflection.camera = cam;
            m_settings.reflection.lights = FindObjectsOfType<Light>();

            if (m_settings.reflection.onlySkybox)
            {
                cam.cullingMask = 0;
                RenderReflections(63);
            }
        }

        private void RenderReflections(int mask)
        {
            var rt = m_settings.reflection.texture_lights;
            var cam = m_settings.reflection.camera;
            cam.RenderToCubemap(rt, mask);

            rt = m_settings.reflection.texture_noLights;
            m_settings.reflection.StoreLights();
            var lights = m_settings.reflection.lights;
            for (int i = 0; i < lights.Length; i++)
            {
                var light = lights[i];
                light.enabled = false;
            }

            cam.RenderToCubemap(rt, mask);
            m_settings.reflection.LoadLights();
        }


        private void Update()
        {
            Graphics.RenderMesh(m_renderParams, m_grid.Mesh, 0, Matrix4x4.identity);
        }

        private void UpdateSimulation(float dt)
        {
            m_updateSettings.dt = dt;
            m_grid.Update(m_updateSettings);
        }

        void LateUpdate()
        {
            if (!m_settings.reflection.onlySkybox)
            {
                var mask = m_settings.reflection.GetMask(Time.frameCount);
                RenderReflections(mask);
            }
            UpdateSimulation(Time.deltaTime);
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

            
            var d_theta = 1f/4;
            var dir_xz = new Vector3(direction.x,0, direction.z).normalized;
            
            var perpendicular = Vector3.Cross(-dir_xz, Vector3.up);

            for (var i = 0; i <= 4; i++)
            {
                var dir = Vector3.Slerp(-perpendicular, dir_xz, i * d_theta);
                AddPointDirectionDisturbance(pos, dir, value * d_theta);
            }

            for (var i = 0; i <= 4; i++)
            {
                var dir = Vector3.Slerp(perpendicular, dir_xz, i * d_theta);
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
