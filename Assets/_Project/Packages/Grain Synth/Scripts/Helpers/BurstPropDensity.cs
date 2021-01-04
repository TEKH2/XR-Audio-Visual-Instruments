using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BurstPropDensity : EmitterPropBase
{
    [Range(0.1f, 10f)]
    [SerializeField]
    public float _Start = 2f;
    [Range(0.1f, 10f)]
    [SerializeField]
    public float _End = 2f;
    [Range(-9.9f, 9.9f)]
    [SerializeField]
    public float _InteractionAmount = 0f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _InteractionShape = 1f;
    [Range(0f, 1.0f)]
    [SerializeField]
    public float _Random = 0f;
    [HideInInspector]
    public float _Min = 0.1f;
    [HideInInspector]
    public float _Max = 10f;
}
