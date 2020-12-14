using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderScript : MonoBehaviour
{
    public BurstEmitterAuthoring _BurstEmitter;

    void OnCollisionEnter(Collision collision)
    {
        //print("ColliderScript hit");
        _BurstEmitter.Collided(collision);
    }
}
