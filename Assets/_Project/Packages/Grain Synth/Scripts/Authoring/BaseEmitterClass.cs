using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseEmitterClass : MonoBehaviour
{
    [Range(0.1f, 50f)]
    public float _MaxAudibleDistance = 20f;
    public virtual void Collided(Collision collision) { }
}
