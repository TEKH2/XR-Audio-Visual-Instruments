using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    public ParticleSystem _TriggerParticleSystem;
    public ParticleSystem _EmitterParticleSystem;
    public ParticleSystem _CollisionParticleSystem;
    public ParticleSystem _ConstantParticleSystem;
    private ParticleSystem.Particle[] _Particles;
    private ParticleSystem.Particle[] _TempParticle;

    private Granulator _Granulator;

    public enum ParticleGroup { Trigger, Emitter, Collision, Constant };



    //---------------------------------------------------------------------
    private void Awake()
    {
        _Particles = new ParticleSystem.Particle[_EmitterParticleSystem.main.maxParticles];
        _TempParticle = new ParticleSystem.Particle[1];
    }


    void Start()
    {
    }

    void Update()
    {
    }


    //---------------------------------------------------------------------
    public void Initialise(Granulator granulator)
    {
        _Granulator = granulator;
    }


    //---------------------------------------------------------------------
    public void SetMass(float mass)
    {
        ParticleSystem.MainModule main = _EmitterParticleSystem.main;

        main.gravityModifier = mass;
    }

    public void SetCollisions (bool collisions)
    {
        ParticleSystem.CollisionModule collisionModule = _EmitterParticleSystem.collision;

        collisionModule.enabled = collisions;
    }



    //---------------------------------------------------------------------
    // Main collide passthrough function and grain particle spawner
    //---------------------------------------------------------------------
    public void Collide(GameObject other, List<ParticleCollisionEvent> collisions)
    {
        //Construct a collision grain burst in the Granulator
        _Granulator.TriggerCollision(collisions, other);

        foreach (ParticleCollisionEvent collision in collisions)
        {
            SpawnCollisionParticle(collision, _Granulator._GrainDuration);
        }
    }


    //---------------------------------------------------------------------
    // Particle spawning for collision system
    //---------------------------------------------------------------------
    public void SpawnCollisionParticle(ParticleCollisionEvent collision, float life)
    {
        // Set transform of collision particle game object, ready for spawning
        GameObject gameObject = _CollisionParticleSystem.gameObject;
        gameObject.transform.position = collision.intersection;
        gameObject.transform.rotation = Quaternion.LookRotation(collision.normal);

        // Generate new emit params based on dummy particle and life passed into the function
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();

        emitParams.startLifetime = life * 2;
        emitParams.applyShapeToPosition = true;

        // Emit particle in main particle system based on those values
        _CollisionParticleSystem.Emit(emitParams, 1);
    }


    //---------------------------------------------------------------------
    // Particle spawning for emitter system. Utilises randomness from the trigger particle
    // system module settings
    //---------------------------------------------------------------------
    public ParticleSystem.Particle SpawnEmitterParticle(Vector3 inheritVelocity, float startSpeed, float life)
    {
        ParticleSystem.MainModule main = _TriggerParticleSystem.main;
        // Emit particle in the dummy system, get its values, then kill the particle
        main.startSpeed = startSpeed;
        _TriggerParticleSystem.Emit(1);
        _TriggerParticleSystem.GetParticles(_TempParticle);
        _TriggerParticleSystem.Clear();

        // Generate new emit params based on dummy particle and life passed into the function
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        emitParams.position = _TempParticle[0].position;
        emitParams.velocity = _TempParticle[0].velocity + inheritVelocity;
        emitParams.startLifetime = life;

        // Emit particle in main particle system based on those values
        _EmitterParticleSystem.Emit(emitParams, 1);

        // Return the particle to granulator function
        return _TempParticle[0];
    }


    //---------------------------------------------------------------------
    // NOT IMPLEMENTED
    // Returns the list of particles active in the specified particle system
    //---------------------------------------------------------------------
    public ParticleSystem.Particle[] GetParticles(ParticleGroup particleGroup)
    {
        ParticleSystem.Particle[] returnParticleSystem = null;

        if (particleGroup == ParticleGroup.Collision)
        {
            _CollisionParticleSystem.GetParticles(returnParticleSystem);
        }
        else if (particleGroup == ParticleGroup.Emitter)
        {
            _EmitterParticleSystem.GetParticles(returnParticleSystem);
        }
        else if (particleGroup == ParticleGroup.Trigger)
        {
            _TriggerParticleSystem.GetParticles(returnParticleSystem);
        }
        else if (particleGroup == ParticleGroup.Constant)
        {
            _ConstantParticleSystem.GetParticles(returnParticleSystem);
        }

        return returnParticleSystem;
    }


    //---------------------------------------------------------------------
    public ParticleSystem.Particle GetRandomMovementParticle()
    {
        // Get all particles, then return a random one based on the current count
        _EmitterParticleSystem.GetParticles(_Particles);
        ParticleSystem.Particle particle = _Particles[(int)(Random.value * _EmitterParticleSystem.particleCount)];

        return particle;
    }


    //---------------------------------------------------------------------
    public int GetMaxParticles()
    {
        return _EmitterParticleSystem.main.maxParticles;
    }


    //---------------------------------------------------------------------
    public int GetParticleCount()
    {
        return _EmitterParticleSystem.particleCount;
    }
}
