using System;
using UnityEngine;

[Serializable]
public class EnvironmentSettings
{
    public Vector2Int size;
    public Matrix4x4 transform;
    public float water_level;
    public Camera camera;

    //TODO remove the following out of this class
    public float[] heightsData;
    public RenderTexture heights;
    public RenderTexture gradients;
}
