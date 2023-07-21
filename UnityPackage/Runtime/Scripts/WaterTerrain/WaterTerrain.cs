using System;
using UnityEngine;

[CreateAssetMenu(fileName = "WaterTerrain", menuName = "WaterSurface/Terrain", order = 1)]
internal class WaterTerrain : ScriptableObject
{
    public Vector2Int size;
    public float[] heights;
    public Matrix4x4 transform;
    public float water_level;
}