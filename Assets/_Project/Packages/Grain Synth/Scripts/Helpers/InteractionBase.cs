using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionBase : MonoBehaviour
{
    public GameObject _SourceObject;
    protected Rigidbody _RigidBody;

    public float _InputMin = 0f;
    public float _InputMax = 1f;
    public float _OutputValue = 0;

    void Start()
    {
        if (_SourceObject == null)
            _SourceObject = gameObject;

        if (_SourceObject.GetComponent<Rigidbody>() == null)
            _SourceObject = this.transform.parent.gameObject;

        _RigidBody = _SourceObject.GetComponent<Rigidbody>();
    }

    public float GetValue()
    {
        return Mathf.Clamp(_OutputValue, 0f, 1f);
    }

    public virtual void CollisionData(Collision collision) {}

    public static float Map(float val, float inMin, float inMax, float outMin, float outMax)
    {
        return outMin + ((outMax - outMin) / (inMax - inMin)) * (val - inMin);
    }
}
