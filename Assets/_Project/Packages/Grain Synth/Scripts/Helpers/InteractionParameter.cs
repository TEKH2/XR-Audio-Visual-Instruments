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
        Roll,
        RollTimesMass,
        Slide,
        Aux
    }

    public InteractionParameterType _SourceParameter;

    [Range(0f, 1f)]
    public float _Smoothing = 0.2f;

    public override void UpdateTempEmitterInteractionSource(GameObject gameObject, Collision collision)
    {
        _SourceObject = gameObject;
        _RigidBody = _SourceObject.GetComponent<Rigidbody>();
        _Colliding = true;
    }


    private void Update()
    {
        float currentValue = _PreviousInputValue;

        if (_RigidBody != null)
        {
            switch (_SourceParameter)
            {
                case InteractionParameterType.Speed:
                    currentValue = _RigidBody.velocity.magnitude;
                    break;
                case InteractionParameterType.AccelerationAbsolute:
                    currentValue = Mathf.Abs((_RigidBody.velocity.magnitude - _PreviousInputValue) / Time.deltaTime);
                    _PreviousInputValue = _RigidBody.velocity.magnitude;
                    break;
                case InteractionParameterType.Acceleration:
                    currentValue = Mathf.Max((_RigidBody.velocity.magnitude - _PreviousInputValue) / Time.deltaTime, 0f);
                    _PreviousInputValue = _RigidBody.velocity.magnitude;
                    break;
                case InteractionParameterType.Deacceleration:
                    currentValue = Mathf.Abs(Mathf.Min((_RigidBody.velocity.magnitude - _PreviousInputValue) / Time.deltaTime, 0f));
                    _PreviousInputValue = _RigidBody.velocity.magnitude;
                    break;
                case InteractionParameterType.Scale:
                    currentValue = _SourceObject.transform.localScale.magnitude;
                    break;
                case InteractionParameterType.Roll:
                    if (_Colliding)
                        currentValue = _RigidBody.angularVelocity.magnitude;
                    else
                        currentValue = 0;
                    break;
                case InteractionParameterType.RollTimesMass:
                    if (_Colliding)
                        currentValue = _RigidBody.angularVelocity.magnitude * _RigidBody.mass;
                    else
                        currentValue = 0;
                    break;
                case InteractionParameterType.Slide:
                    if (_Colliding)
                        currentValue = _RigidBody.velocity.magnitude / _RigidBody.angularVelocity.magnitude;
                    else
                        currentValue = 0;
                    break;
                default:
                    break;
            }
        }
        else currentValue = 0;


        UpdateSmoothedOutputValue(currentValue, _Smoothing);
    }

    public void SetAuxValue(float val)
    {
        UpdateSmoothedOutputValue(val, _Smoothing);
    }
}
