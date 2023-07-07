using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainBaker))]
[CanEditMultipleObjects]
internal class TerrainBakerEditor : Editor
{
    private TerrainBaker Target => (TerrainBaker)this.target;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Bake"))
        {
            Target.Bake();
            EditorUtility.SetDirty(Target.WaterTerrain);
        }
    }
}
