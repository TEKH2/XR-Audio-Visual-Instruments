using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerParticlesPlayhead : MonoBehaviour
{
    List<ParticleCollisionEvent> _ParticleCollisions;

    // Start is called before the first frame update
    void Start()
    {
        _ParticleCollisions = new List<ParticleCollisionEvent>();
    }


    private void OnParticleCollision(GameObject other)
    {
        
    }

    private void OnParticleTrigger()
    {
        
    }




    // Update is called once per frame
    void Update()
    {
        
    }
}
