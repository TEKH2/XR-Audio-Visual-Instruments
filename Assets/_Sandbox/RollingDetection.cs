using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RollingDetection : MonoBehaviour
{
    public enum State
    {
        NotColliding,
        Colliding,
    }

    public State _State = State.NotColliding;
    public PhysicMaterial _CollidedPhysicsMat;
    public float _RollSpeed;

    Rigidbody _RB;


    private void Start()
    {
        _RB = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        _State = State.Colliding;
        _CollidedPhysicsMat = collision.collider.material;
    }

    private void OnCollisionStay(Collision collision)
    {
        _State = State.Colliding;
        _RollSpeed = _RB.angularVelocity.magnitude;
    }

    private void OnCollisionExit(Collision collision)
    {
        _State = State.NotColliding;
        _CollidedPhysicsMat = null;
        _RollSpeed = 0;
    }
}
