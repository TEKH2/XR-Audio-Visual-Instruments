using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EmitterPropTranspose : EmitterPropBase
{
    [Range(-3f, 3f)]
    [SerializeField]
    public float _Idle = 1;
    [Range(0f, 1.0f)]
    [SerializeField]
    public float _Random = 0f;
    [Range(-6f, 6f)]
    [SerializeField]
    public float _InteractionAmount = 0f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _InteractionShape = 1f;
    [HideInInspector]
    public float _Min = -3f;
    [HideInInspector]
    public float _Max = 3f;
}