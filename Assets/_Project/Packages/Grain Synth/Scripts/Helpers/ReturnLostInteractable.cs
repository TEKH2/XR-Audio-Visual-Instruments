using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReturnLostInteractable : MonoBehaviour
{
    public Vector3 _InitialPosition;
    public Quaternion _InitialRotation;
    public GameObject _BoundingObject;
    private Rigidbody _RigidBody;
    public float _Radius = 30f;
    public float _HeightLimit = 20f;

    void Start()
    {
        _InitialPosition = this.transform.position;
        _InitialRotation = this.transform.rotation;

        if (_BoundingObject != null)
            _Radius = (_BoundingObject.transform.localScale.x + _BoundingObject.transform.localScale.z) / 2;

        _RigidBody = this.GetComponent<Rigidbody>();
    }

    void Update()
    {
        Vector3 objectPlanePosition = new Vector3(this.transform.position.x, 0, this.transform.position.z);
        if (objectPlanePosition.magnitude > _Radius || this.transform.position.y > _HeightLimit || this.transform.position.y < -_HeightLimit)
        {
            if (_RigidBody != null)
            {
                _RigidBody.MovePosition(_InitialPosition);
                _RigidBody.velocity = Vector3.zero;
                _RigidBody.rotation = _InitialRotation;
            }
        }
    }
}
