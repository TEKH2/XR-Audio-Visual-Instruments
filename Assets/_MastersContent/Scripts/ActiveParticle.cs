using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveParticle
{
    private int _ID;
    private bool _IsInside;
    private Vector3 _Position;
    private Vector3 _Origin;

    //private ParticleSystem.Particle _Particle;


    public void SetID(int id)
    {
        _ID = id;
    }

    public int GetID()
    {
        return _ID;
    }

    /*
    // set particle object within class
    public void SetParticle(ParticleSystem.Particle part)
    {
        _Particle = part;
    }

    public ParticleSystem.Particle[] GetParticle()
    {
        ParticleSystem.Particle[] particleArray = new ParticleSystem.Particle[1];
        particleArray[0] = _Particle;
        return particleArray;
    }
    */

    public void UpdatePosition(Vector3 pos)
    {
        _Position = pos;
    }

    // return current inside value
    public bool GetInside()
    {
        return _IsInside;
    }


    public bool HasParticleMovedBetweenPlayhead(Vector3 playheadPosition, float playheadSize, ref bool inside)
    {
        bool different = false;

        inside = (_Position - playheadPosition).sqrMagnitude < playheadSize;

        if (inside != _IsInside)
        {
            _IsInside = inside;
            different = true;
        }
           
        return different;
    }


    // return position of particle
    public Vector3 GetPosition()
    {
        return _Position;
    }

    public void SetInitialPosition(Vector3 position)
    {
        _Position = position;
        _Origin = position;
    }

    public void ApplyForce(Transform transform, float force)
    {
    }

    /*
    public void MoveTowardOrigin()
    {
        Vector3 newVelocity = (_Origin - _Position) / 5;
        _Particle.velocity = newVelocity;
        _Particle.position = Vector3.Lerp(_Position, _Origin, Time.deltaTime);

    }
    */
}
