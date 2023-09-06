using UnityEngine;
using UnityEditor;
using WaterWaveSurface;
using static UnityEditor.PlayerSettings;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

[CustomEditor(typeof(WaterSurface))]
[CanEditMultipleObjects]
internal class WaterSurfaceEditor : Editor
{
    private WaterSurface Target => (WaterSurface)this.target;

    private Mesh m_waterLevel_mesh;
    private Mesh m_bakingVolume_mesh;

    private Material m_volume_material;

    private Vector3Int Extends
    {
        get
        {
            var halfScale = Target.transform.lossyScale * 0.5f;
            return new Vector3Int(Round(halfScale.x), Round(halfScale.y), Round(halfScale.z));
            int Round(float val)
            {
                return Mathf.Max(Mathf.RoundToInt(val), 1);
            }
        }
    }

    SerializedProperty settings;
    SerializedProperty environment;
    SerializedProperty simulation;
    SerializedProperty waterLevel;
    SerializedProperty windDirection;
    SerializedProperty amplitude;

    void OnEnable()
    {
        settings = serializedObject.FindProperty("m_settings");
        environment = settings.FindPropertyRelative("environment");
        simulation = settings.FindPropertyRelative("simulation");
        amplitude = simulation.FindPropertyRelative("amplitude");
        waterLevel = environment.FindPropertyRelative("water_level");
        windDirection = simulation.FindPropertyRelative("wind_direction");
        SceneView.duringSceneGui -= CustomSceneGUI;
        SceneView.duringSceneGui += CustomSceneGUI;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if(GUILayout.Button("Store Simulation Data"))
        {
            Store(Amplitude, "amplitude");
        }
        if (GUILayout.Button("Load Simulation Data"))
        {
            Load(Amplitude, "amplitude");
        }
    }

    void Store(RenderTexture rt3D, string path)
    {
        int width = rt3D.width, height = rt3D.height, depth = rt3D.volumeDepth;
        var nativeArray = new NativeArray<float>(width * height * depth, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        var request = AsyncGPUReadback.RequestIntoNativeArray(ref nativeArray, rt3D, 0, (_) =>
        {
            Texture3D output = new Texture3D(width, height, depth, rt3D.graphicsFormat, TextureCreationFlags.None);
            output.SetPixelData(nativeArray, 0);
            output.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            AssetDatabase.CreateAsset(output, $"Assets/{path}.asset");
            AssetDatabase.SaveAssetIfDirty(output);
            nativeArray.Dispose();
        });

        request.WaitForCompletion();
    }

    void Load(RenderTexture rt3D, string path)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture3D>($"Assets/{path}.asset");       
        Graphics.CopyTexture(texture, rt3D);
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= CustomSceneGUI;
    }

    private float WaterLevel
    {
        get => waterLevel.floatValue;
        set => waterLevel.floatValue = value;
    }

    private float WindDirection
    {
        get=>windDirection.floatValue;
        set => windDirection.floatValue = value;
    }

    private RenderTexture Amplitude
    {
        get => amplitude.objectReferenceValue as RenderTexture;
        set => amplitude.objectReferenceValue = value;
    }

    private void DrawSceneObjects()
    {
        var shouldDraw = GetAndUpdateShouldDraw();
        if (!shouldDraw)
            return;

        DrawWaterSurface();
        DrawBakingVolume();
    }

    private void CustomSceneGUI(SceneView view)
    {
        DrawControlGizmos();
        DrawSceneObjects();
        serializedObject.ApplyModifiedProperties();
    }


    private void DrawControlGizmos()
    {
        var maxExtension = Mathf.Max(Extends.x, Extends.z);
        var pos = Target.transform.position;
        var targetY = Target.transform.position.y;
        pos.y = WaterLevel;

        Handles.color = new Color(1, 1, 1, 1);

        Vector3 newPos = Handles.Slider(pos, Vector3.up, maxExtension, Handles.ArrowHandleCap, 1);

        newPos.y = Mathf.Clamp(newPos.y, targetY - Extends.y, targetY + Extends.y);
        WaterLevel = newPos.y;

        Handles.Label(newPos + Vector3.up * maxExtension, "Water Level");

        
        var q =
            Handles.Disc(
                Quaternion.Euler(0, WindDirection, 0),
                newPos,
                Vector3.up,
                maxExtension, false, 1);

        WindDirection = q.eulerAngles.y;

        Handles.ArrowHandleCap(
            0,
            newPos,
            Quaternion.Euler(0, q.eulerAngles.y, 0),
        maxExtension, EventType.Repaint);

        Handles.Label(newPos +  q * Vector3.forward * maxExtension, "Wind Direction");
    }

    private Material GetVolumeMaterial()
    {

        if (m_volume_material == null)
        {
            m_volume_material = new Material(Shader.Find("Unlit/WaterWaveSurfaces/BakeVolume"));
        }
        m_volume_material.SetFloat(Shader.PropertyToID("WaterLevel"), WaterLevel);
        return m_volume_material;
    }


    private int m_last_frame;
    private bool GetAndUpdateShouldDraw()
    {
        var lastFrame = m_last_frame;
        m_last_frame = Time.frameCount;
        return Time.frameCount != lastFrame;
    }

    private void DrawBakingVolume()
    {
        if (m_bakingVolume_mesh == null)
        {
            Vector3[] verts = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),

                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(0.5f,  0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, -0.5f)
            };

            int[] indices = { 3, 2, 1 , 0,
                              4, 5, 6, 7,
                              2, 3, 7, 6,
                              0,1, 5, 4,
                              1, 2, 6, 5,
                              7, 3, 0, 4 };

            m_bakingVolume_mesh = new Mesh();
            m_bakingVolume_mesh.vertices = verts;

            m_bakingVolume_mesh.SetIndices(indices, MeshTopology.Quads, 0);
            m_bakingVolume_mesh.RecalculateNormals();
            m_bakingVolume_mesh.RecalculateTangents();
            m_bakingVolume_mesh.RecalculateBounds();
        }

        var pos = Target.transform.position;

        var matrix =
            Matrix4x4.Translate(Vector3.right * pos.x + Vector3.forward * pos.z + Vector3.up * pos.y) *
            Matrix4x4.Rotate(Quaternion.Euler(0, Target.transform.rotation.eulerAngles.y, 0)) *
            Matrix4x4.Scale(Extends * 2);

        Graphics.DrawMesh(m_bakingVolume_mesh, matrix, GetVolumeMaterial(), 0);
    }

    private void DrawWaterSurface()
    {
        if (m_waterLevel_mesh == null)
        {
            Vector3[] verts = new Vector3[]
            {
                new Vector3(-0.5f, 0, -0.5f),
                new Vector3(-0.5f, 0, 0.5f),
                new Vector3(0.5f, 0, 0.5f),
                new Vector3(0.5f, 0, -0.5f)
            };

            int[] indices = { 0, 1, 2, 3 };

            m_waterLevel_mesh = new Mesh();
            m_waterLevel_mesh.vertices = verts;

            m_waterLevel_mesh.SetIndices(indices, MeshTopology.Quads, 0);
            m_waterLevel_mesh.RecalculateNormals();
            m_waterLevel_mesh.RecalculateTangents();
            m_waterLevel_mesh.RecalculateBounds();
        }

        var pos = Target.transform.position;

        var matrix =
            Matrix4x4.Translate(Vector3.right * pos.x + Vector3.forward * pos.z) *
            Matrix4x4.Translate(Vector3.up * WaterLevel) *
            Matrix4x4.Rotate(Quaternion.Euler(0, Target.transform.rotation.eulerAngles.y, 0)) * 
            Matrix4x4.Scale(Vector3.right * Extends.x * 2 + Vector3.forward * Extends.z * 2);

        Graphics.DrawMesh(m_waterLevel_mesh, matrix, GetVolumeMaterial(), 0);
    }
}
