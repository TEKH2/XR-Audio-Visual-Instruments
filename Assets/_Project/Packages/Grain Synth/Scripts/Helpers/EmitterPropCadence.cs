using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EmitterPropCadence : EmitterPropBase
{
    [Range(4f, 500f)]
    [SerializeField]
    public float _Idle = 20f;
    [Range(0f, 1.0f)]
    [SerializeField]
    public float _Random = 0f;
    [Range(-496f, 496f)]
    [SerializeField]
    public float _InteractionAmount = 0f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _InteractionShape = 1f;
    [HideInInspector]
    public float _Min = 4f;
    [HideInInspector]
    public float _Max = 500f;
}