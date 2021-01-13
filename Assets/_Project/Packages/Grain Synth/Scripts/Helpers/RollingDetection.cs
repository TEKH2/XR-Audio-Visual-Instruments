using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RollingDetection : MonoBehaviour
{
    public bool _Colliding = false;
    public PhysicMaterial _CollidedPhysicsMat;
    public float _RollSpeed;

    Rigidbody _RB;

    private void Start()
    {
        _RB = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        _Colliding = true;
        _CollidedPhysicsMat = collision.collider.material;
    }

    private void OnCollisionStay(Collision collision)
    {
        _Colliding = true;
        _RollSpeed = _RB.angularVelocity.magnitude;
    }

    private void OnCollisionExit(Collision collision)
    {
        _Colliding = false;
        _CollidedPhysicsMat = null;
        _RollSpeed = 0;
    }
}
