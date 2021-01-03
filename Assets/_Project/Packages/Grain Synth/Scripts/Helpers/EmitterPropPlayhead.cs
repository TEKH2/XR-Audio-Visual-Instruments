using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EmitterPropPlayhead : EmitterPropBase
{
    [Range(0f, 1f)]
    [SerializeField]
    public float _Idle = 0f;
    [Range(0f, 1.0f)]
    [SerializeField]
    public float _Random = 0.01f;
    [Range(-1f, 1f)]
    [SerializeField]
    public float _InteractionAmount = 0f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _InteractionShape = 1f;
    [HideInInspector]
    public float _Min = 0f;
    [HideInInspector]
    public float _Max = 1f;
}