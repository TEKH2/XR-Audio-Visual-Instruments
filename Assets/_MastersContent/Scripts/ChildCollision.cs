using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChildCollision : MonoBehaviour
{
    private Collision _LatestCollision;
    private Collision _StayCollision;
    private GameObject _ContainerObject;
    private bool _TouchingSpherecollider = false;

    private void OnCollisionEnter(Collision collision)
    {
        _LatestCollision = collision;
    }

    private void OnCollisionStay(Collision collision)
    {
        _StayCollision = collision;
        if (_ContainerObject != null)
            if (collision.gameObject == _ContainerObject)
                _TouchingSpherecollider = true;
    }

    public void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject == _ContainerObject)
            _TouchingSpherecollider = false;
    }

    public Collision GetCollision()
    {
        Collision tempCollision = _LatestCollision;
        _LatestCollision = null;
        return tempCollision;
    }

    public Collision GetCollisionStay()
    {
        Collision tempCollision = _StayCollision;
        _StayCollision = null;
        return tempCollision;
    }

    public void SetSphereCollider(GameObject colliderObject)
    {
        _ContainerObject = colliderObject;
    }

    public bool IsTouchingSphereCollider()
    {
        return _TouchingSpherecollider;
    }
}
