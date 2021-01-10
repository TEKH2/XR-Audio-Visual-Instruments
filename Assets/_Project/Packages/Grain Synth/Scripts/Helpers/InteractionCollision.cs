using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionCollision : InteractionBase
{
    public enum InteractionCollisionType
    {
        CollisionForce,
        CollisionPoint,
        CollisionNormal,
        Roll,
        Slide
    }

    public InteractionCollisionType _SourceParameter;


    public override void SetCollisionData(Collision collision, int currentCollisionCount)
    {
        switch (_SourceParameter)
        {
            case InteractionCollisionType.CollisionForce:
                _OutputValue = collision.relativeVelocity.magnitude;
                break;
            case InteractionCollisionType.CollisionPoint:
                break;
            case InteractionCollisionType.CollisionNormal:
                _OutputValue = collision.GetContact(0).normal.magnitude;
                break;
            case InteractionCollisionType.Roll:
                if (currentCollisionCount > 0)
                    _OutputValue = _RigidBody.angularVelocity.magnitude;
                else
                    _OutputValue = 0;
                break;
            case InteractionCollisionType.Slide:
                if (currentCollisionCount > 0)
                    _OutputValue = _RigidBody.velocity.magnitude;
                else
                    _OutputValue = 0;
                break;
            default:
                break;
        }

        _OutputValue = Map(_OutputValue, _InputMin, _InputMax, 0, 1);
    }
}
