using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderScript : MonoBehaviour
{
    public List<BaseEmitterClass> _Emitters;

    private void Start()
    {
        
    }

    void OnCollisionEnter(Collision collision)
    {
        foreach (var emitter in _Emitters)
        {
            if (emitter != null && emitter.enabled)
                emitter.Collided(collision);
        }
    }
}
