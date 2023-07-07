using System;
using UnityEngine;

[CreateAssetMenu(fileName = "WaterTerrain", menuName = "WaterSurface/Terrain", order = 1)]
internal class WaterTerrain : ScriptableObject
{
    public int size;
    public float[] heights;
}
