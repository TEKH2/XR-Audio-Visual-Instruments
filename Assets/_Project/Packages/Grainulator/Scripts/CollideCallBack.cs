using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollideCallBack : MonoBehaviour
{
    public ParticleManager _ParticleManager;
    public ParticleSystem _ParticleSystem;
    private List<ParticleCollisionEvent> _CollisionEvents;


    void Start()
    {
        _ParticleSystem = _ParticleManager._EmitterParticleSystem;
        _CollisionEvents = new List<ParticleCollisionEvent>();
    }

    private void OnParticleCollision(GameObject other)
    {
        _ParticleSystem.GetCollisionEvents(other, _CollisionEvents);
        _ParticleManager.Collide(other, _CollisionEvents);
    }
}
