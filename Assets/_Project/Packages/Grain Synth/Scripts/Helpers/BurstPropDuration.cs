using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BurstPropDuration : EmitterPropBase
{
    [Range(10f, 1000f)]
    [SerializeField]
    public float _Default = 100f;
    [Range(-990f, 990f)]
    [SerializeField]
    public float _InteractionAmount = 0f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _InteractionShape = 1f;
    [Range(0f, 1.0f)]
    [SerializeField]
    public float _Random = 0f;
    [HideInInspector]
    public float _Min = 10f;
    [HideInInspector]
    public float _Max = 1000f;
}
