using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Paddle : MonoBehaviour
{
    [SerializeField]
    private Animator m_animator;

    [SerializeField]
    private Transform m_end;

    public Animator Animator => m_animator;

    public Transform end => m_end;

    public delegate void RowDelegate();
    public RowDelegate StartRow { get; set; }

    public void OnStartRow()
    {
        StartRow();
    }
}
