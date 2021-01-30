using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionCollision : InteractionBase
{
    public enum InteractionCollisionType
    {
        CollisionForce,
        CollisionForceTimesMass,
        CollisionPoint,
        CollisionNormal,
    }

    public InteractionCollisionType _SourceParameter;

    public bool _UseMassOfCollidingBody = false;

    public override void UpdateTempEmitterInteractionSource(GameObject gameObject, Collision collision)
    {
        _SourceObject = gameObject;
        _RigidBody = _SourceObject.GetComponent<Rigidbody>();
        _Colliding = true;

        SetCollisionData(collision);
    }


    public override void SetCollisionData(Collision collision)
    {
        switch (_SourceParameter)
        {
            case InteractionCollisionType.CollisionForce:
                _OutputValue = collision.relativeVelocity.magnitude;
                break;
            case InteractionCollisionType.CollisionForceTimesMass:
                if (_RigidBody != null)
                {

                    if (_UseMassOfCollidingBody)
                    {
                        Rigidbody remoteRB = collision.collider.GetComponent<Rigidbody>();
                        if (remoteRB != null)
                            _OutputValue = collision.relativeVelocity.magnitude * (1 - remoteRB.mass / 2);
                    }
                    else
                    {
                        _OutputValue = collision.relativeVelocity.magnitude * _RigidBody.mass;
                    }

                }

                break;
            case InteractionCollisionType.CollisionPoint:
                break;
            case InteractionCollisionType.CollisionNormal:
                _OutputValue = collision.GetContact(0).normal.magnitude;
                break;
            default:
                break;
        }
        _OutputValue = Map(_OutputValue, _InputMin, _InputMax, 0, 1);
        
    }
}
