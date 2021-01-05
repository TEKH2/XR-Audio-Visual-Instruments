using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderScript : MonoBehaviour
{
    public BaseEmitterClass[] _Emitters;

    private void Start()
    {
        _Emitters = Helper.FindComponentsInChildrenWithTag<BaseEmitterClass>(this.gameObject, "Emitter", true);
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
