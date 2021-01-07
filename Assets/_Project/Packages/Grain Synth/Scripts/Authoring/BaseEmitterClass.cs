using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseEmitterClass : MonoBehaviour
{
    [Range(0.1f, 50f)]
    public float _MaxAudibleDistance = 10f;

    [Header("DEBUG")]
    public float _CurrentDistance = 0;
    public float _DistanceVolume = 0;
    public bool _WithinEarshot = true;
    
    public virtual void Collided(Collision collision) { }
}
