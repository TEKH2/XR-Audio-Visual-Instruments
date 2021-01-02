using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectParameter : MonoBehaviour
{
    public GameObject _SourceObject;
    private Rigidbody _RigidBody;

    public enum SourceParameter
    {
        Speed,
        Acceleration,
        Scale,
        CollisionForce,
        CollisionPoint,
        CollisionNormal,
    }

    public SourceParameter _SourceParameter;

    public float _InputMin = 0f;
    public float _InputMax = 1f;

    [Range(0f, 1f)]
    public float _Smoothing = 0.2f;
    private float _ActualSmoothing = 0;

    public float _OutputValue;

    private float _PreviousInputValue = 0;


    void Start()
    {
        if (_SourceObject == null)
            _SourceObject = gameObject;

        _RigidBody = _SourceObject.GetComponent<Rigidbody>();
    }

    public float GetValue()
    {
        return _OutputValue;
    }

    void Update()
    {
        _ActualSmoothing = (1 - _Smoothing) * 10f;
    }

    private void FixedUpdate()
    {
        switch (_SourceParameter)
        {
            case SourceParameter.Speed:
                UpdateOutputValue(_RigidBody.velocity.magnitude);
                break;
            case SourceParameter.Acceleration:
                UpdateOutputValue((_RigidBody.velocity.magnitude - _PreviousInputValue) / Time.deltaTime);
                _PreviousInputValue = _RigidBody.velocity.magnitude;
                break;
            case SourceParameter.Scale:
                UpdateOutputValue(_SourceObject.transform.localScale.magnitude);
                break;
            default:
                break;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        switch (_SourceParameter)
        {
            case SourceParameter.CollisionForce:
                _OutputValue = collision.relativeVelocity.magnitude;
                break;
            case SourceParameter.CollisionPoint:
                break;
            case SourceParameter.CollisionNormal:
                _OutputValue = collision.GetContact(0).normal.magnitude;
                break;
            default:
                break;
        }
    }

    void UpdateOutputValue(float inputValue)
    {
        float newValue = Map(inputValue, _InputMin, _InputMax, 0, 1);
        _OutputValue = Mathf.Lerp(_OutputValue, newValue, _ActualSmoothing * Time.deltaTime);
    }

    public static float Map(float val, float inMin, float inMax, float outMin, float outMax)
    {
        return outMin + ((outMax - outMin) / (inMax - inMin)) * (val - inMin);
    }
}
