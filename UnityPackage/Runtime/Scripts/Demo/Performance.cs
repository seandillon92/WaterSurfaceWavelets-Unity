using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Performance : MonoBehaviour
{
    private Rect m_rect;
    private GUIStyle m_style;
    private int m_framerate;
    private float m_total_time;

    private void Start()
    {
        m_rect = new Rect(Vector2.zero, Vector2.one * 1000);
        m_style = new GUIStyle();
        m_style.fontSize = 180;
    }

    private void Update()
    {
        m_total_time += Time.deltaTime;
        if (m_total_time > 1.0f)
        {
            m_framerate = Mathf.RoundToInt(1f / Time.deltaTime);
            m_total_time = 0f;
        }
    }

    private void OnGUI()
    {
        GUI.Label(m_rect, m_framerate.ToString(), m_style);
    }
}
