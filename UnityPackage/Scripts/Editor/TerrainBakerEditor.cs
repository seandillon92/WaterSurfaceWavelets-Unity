using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(TerrainBaker))]
[CanEditMultipleObjects]
internal class TerrainBakerEditor : Editor
{
    private TerrainBaker Target => (TerrainBaker)this.target;

    private Mesh m_waterLevel_mesh;
    private Mesh m_bakingVolume_mesh;
    private Mesh m_terrain_preview_mesh;

    private Material m_volume_material;
    private MeshPreview m_preview;

    private Mesh GenerateWaterTerrainMesh(int visualizationSize, Vector2Int terrainSize, WaveGrid grid)
    {
        var mesh = new Mesh();
        var side = visualizationSize;
        var positions = new Vector3[side * side];
        var indices = new int[(side - 1) * (side - 1) * 4];
        var size = terrainSize;
        for (int i = 0; i < side * side; i++)
        {
            float x = i / (side);
            x = Mathf.Lerp(-1, 1, x / side);

            float y = i % side;
            y = Mathf.Lerp(-1, 1, y / side);

            x *= 0.99f * size.x;
            y *= 0.99f * size.y;
            positions[i] =
                new Vector3(x, -grid.GetTerrainHeight(new Vector2(x, y)), y);
        }

        for (var i = 0; i < side - 1; i++)
        {
            for (var j = 0; j < side - 1; j++)
            {
                indices[i * (side - 1) * 4 + j * 4] = i * side + j;
                indices[i * (side - 1) * 4 + j * 4 + 1] = i * side + j + 1;
                indices[i * (side - 1) * 4 + j * 4 + 2] = (i + 1) * side + j + 1;
                indices[i * (side - 1) * 4 + j * 4 + 3] = (i + 1) * side + j;
            }
        }

        mesh.vertices = positions;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetIndices(indices, MeshTopology.Quads, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        return mesh;
    }

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

    SerializedProperty waterLevel;
    SerializedProperty terrain;

    void OnEnable()
    {
        waterLevel = serializedObject.FindProperty("m_waterLevel");
        terrain = serializedObject.FindProperty("m_terrain");

        SceneView.duringSceneGui -= CustomSceneGUI;
        SceneView.duringSceneGui += CustomSceneGUI;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= CustomSceneGUI;
        m_preview?.Dispose();
    }

    private float WaterLevel
    {
        get => waterLevel.floatValue;
        set => waterLevel.floatValue = value;
    }

    private WaterTerrain Terrain
    {
        get => terrain.objectReferenceValue as WaterTerrain;
        set => terrain.objectReferenceValue = value;
    }


    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();

        if (GUILayout.Button("Bake"))
        {

            Target.Bake(Extends, Target.transform.position);
            m_terrain_preview_mesh = null;

            m_preview?.Dispose();
            m_preview = null;
        }
        serializedObject.ApplyModifiedProperties();
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
        var maxExtension = Mathf.Max(Extends.x, Extends.z);
        var pos = Target.transform.position;
        var targetY = Target.transform.position.y;
        pos.y = WaterLevel;

        Handles.color = new Color(1, 1, 1, 1);

        pos = GetHandlePosition(maxExtension, pos);

        Vector3 newPos = Handles.Slider(pos, Vector3.up);

        newPos.y = Mathf.Clamp(newPos.y, targetY - Extends.y, targetY + Extends.y);
        WaterLevel = newPos.y;

        Handles.Label(newPos, "Water Level");

        Vector3 GetHandlePosition(int maxExtension, Vector3 pos)
        {
            var cameraPos = Camera.current.transform.position;
            var dir = -(Target.transform.position - cameraPos);
            dir.y = 0;
            dir.Normalize();
            pos.x += maxExtension * dir.z;
            pos.z += maxExtension * -dir.x;
            return pos;
        }

        DrawSceneObjects();
        serializedObject.ApplyModifiedProperties();
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

    private void GenerateTerrainPreview()
    {
        if (m_terrain_preview_mesh == null)
        {
            var settings = new WaveGrid.Settings();
            settings.terrain = Terrain;
            var extends = Extends;
            using (var grid = new WaveGrid(settings))
            {
                m_terrain_preview_mesh = GenerateWaterTerrainMesh(100, new Vector2Int(extends.x, extends.z), grid);
            }
        }
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
            Matrix4x4.Scale(Vector3.right * Extends.x * 2 + Vector3.forward * Extends.z * 2);

        Graphics.DrawMesh(m_waterLevel_mesh, matrix, GetVolumeMaterial(), 0);
    }

    public override void OnPreviewSettings()
    {
        base.OnPreviewSettings();

        if (m_preview != null)
            m_preview.OnPreviewSettings();
    }

    public override bool HasPreviewGUI()
    {
        return true;
    }

    public override void OnPreviewGUI(Rect r, GUIStyle background)
    {
        GenerateTerrainPreview();

        if (m_preview == null)
        {
            var bound = m_terrain_preview_mesh.bounds;
            var max = Mathf.Max(Math.Max(bound.extents.x, bound.extents.y), bound.extents.z);
            var normalized = Instantiate(m_terrain_preview_mesh);
            var positions = normalized.vertices;

            for (var i = 0; i < normalized.vertexCount; i++)
            {
                positions[i] /= max;
            }
            normalized.vertices = positions;
            normalized.RecalculateBounds();
            normalized.RecalculateTangents();
            normalized.RecalculateNormals();
            m_preview = new MeshPreview(normalized);
        }

        m_preview.OnPreviewGUI(r, background);
        GUI.Label(r, "Ocean Bed Preview");
    }
}
