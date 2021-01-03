using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EmitterPropDuration : EmitterPropBase
{
    [Range(2f, 500f)]
    [SerializeField]
    public float _Idle = 50f;
    [Range(0f, 1.0f)]
    [SerializeField]
    public float _Random = 0.01f;
    [Range(-502f, 502f)]
    [SerializeField]
    public float _InteractionAmount = 0f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _InteractionShape = 1f;
    [HideInInspector]
    public float _Min = 2f;
    [HideInInspector]
    public float _Max = 500f;
}