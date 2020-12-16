using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderScript : MonoBehaviour
{
    public BurstEmitterAuthoring _BurstEmitter;

    private void Start()
    {
        if (_BurstEmitter == null)
            _BurstEmitter = GetComponent<BurstEmitterAuthoring>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (_BurstEmitter != null)
            _BurstEmitter.Collided(collision);
    }
}
