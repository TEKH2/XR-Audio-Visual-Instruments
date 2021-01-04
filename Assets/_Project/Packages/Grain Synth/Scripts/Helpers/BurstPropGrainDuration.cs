using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BurstPropGrainDuration : EmitterPropBase
{
    [Range(5f, 500f)]
    [SerializeField]
    public float _Start = 20f;
    [Range(5f, 500f)]
    [SerializeField]
    public float _End = 20f;
    [Range(-495f, 495f)]
    [SerializeField]
    public float _InteractionAmount = 0f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _InteractionShape = 1f;
    [Range(0f, 1.0f)]
    [SerializeField]
    public float _Random = 0f;
    [HideInInspector]
    public float _Min = 5f;
    [HideInInspector]
    public float _Max = 500f;
}
