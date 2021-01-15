using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderScript : MonoBehaviour
{
    public BaseEmitterClass[] _Emitters;
    public InteractionBase[] _Interactions;
    public bool _StaticSurface = false;
    private int _CollidingCount = 0;

    private void Start()
    {
        _Emitters = GetComponentsInChildren<BaseEmitterClass>();
        _Interactions = GetComponentsInChildren<InteractionBase>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        foreach (var emitter in _Emitters)
        {
            emitter.IsStaticSurface(_StaticSurface);
            emitter.NewCollision(collision);
            _CollidingCount++;
        }

        foreach (var interaction in _Interactions)
        {
            interaction.SetCollisionData(collision);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        foreach (var emitter in _Emitters)
        {
            emitter.UpdateCurrentCollisionStatus(true);
        }

        foreach (var interaction in _Interactions)
        {
            interaction.SetColliding(true, collision.collider.material);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        foreach (var emitter in _Emitters)
        {
            _CollidingCount--;

            if (_CollidingCount == 0)
                emitter.UpdateCurrentCollisionStatus(false);
        }

        foreach (var interaction in _Interactions)
        {
            interaction.SetColliding(false, collision.collider.material);
        }
    }
}
