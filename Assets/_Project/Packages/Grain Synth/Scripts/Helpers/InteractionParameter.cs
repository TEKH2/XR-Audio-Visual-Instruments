using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InteractionParameter : InteractionBase
{
    public enum InteractionParameterType
    {
        Speed,
        AccelerationAbsolute,
        Acceleration,
        Deacceleration,
        Scale,
        Aux
    }

    public InteractionParameterType _SourceParameter;

    [Range(0f, 1f)]
    public float _Smoothing = 0.2f;

    private float _PreviousInputValue = 0;

    private void FixedUpdate()
    {
        float currentValue;
        switch (_SourceParameter)
        {
            case InteractionParameterType.Speed:
                UpdateOutputValue(_RigidBody.velocity.magnitude);
                break;
            case InteractionParameterType.AccelerationAbsolute:
                currentValue = Mathf.Abs((_RigidBody.velocity.magnitude - _PreviousInputValue) / Time.deltaTime);
                UpdateOutputValue(currentValue);
                _PreviousInputValue = _RigidBody.velocity.magnitude;
                break;
            case InteractionParameterType.Acceleration:
                currentValue = Mathf.Max((_RigidBody.velocity.magnitude - _PreviousInputValue) / Time.deltaTime, 0f);
                UpdateOutputValue(currentValue);
                _PreviousInputValue = _RigidBody.velocity.magnitude;
                break;
            case InteractionParameterType.Deacceleration:
                currentValue = Mathf.Abs(Mathf.Min((_RigidBody.velocity.magnitude - _PreviousInputValue) / Time.deltaTime, 0f));
                UpdateOutputValue(currentValue);
                _PreviousInputValue = _RigidBody.velocity.magnitude;
                break;
            case InteractionParameterType.Scale:
                UpdateOutputValue(_SourceObject.transform.localScale.magnitude);
                break;
            default:
                break;
        }
    }

    public void SetAuxValue(float val)
    {
        UpdateOutputValue(val);
    }

    private void UpdateOutputValue(float inputValue)
    {
        float newValue = Map(inputValue, _InputMin, _InputMax, 0, 1);
        float actualSmoothing = (1 - _Smoothing) * 10f;
        _OutputValue = Mathf.Lerp(_OutputValue, newValue, actualSmoothing * Time.deltaTime);
    }
}
