using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EmitBehavior", menuName = "Grain Synth/Emitter Behavior", order = 1)]
public class EmitterBehavior : ScriptableObject
{
    public BehaviourParameter _Playhead;
    public BehaviourParameter _Cadence;
    public BehaviourParameter _Duration;
    public BehaviourParameter _Transpose;

    public void Init(GameObject go)
    {
        _Playhead.SetGameObject(go);
        _Cadence.SetGameObject(go);
        _Duration.SetGameObject(go);
        _Transpose.SetGameObject(go);
    }
}

[System.Serializable]
public class BehaviourParameter
{ 
    public void SetGameObject(GameObject gameObject)
    {
        _AttachedGameObject = gameObject;
    }

    public enum ModulationType
    {
        Time,
        Position,
        Velocity,
        Rotation,
        Scale,
        TEST
    }

    public enum AxisSelection
    {
        Magnitude,
        X,
        Y,
        Z
    }


    private GameObject _AttachedGameObject;

    public ParticleSystem.MinMaxCurve _Curve;

    public ModulationType _ModulationType;
    public AxisSelection _Axis;

    public Vector2 _InputScale = new Vector2(0.0f, 1.0f);

    [Header("DEBUG")]
    [Range(0, 1)]
    public float _TestInput = 0;
    public float _ModulationInput = 0;

    public float GetValue(float time)
    {
        if (_ModulationType == ModulationType.Time)
            _ModulationInput = time;
        else
        {
            switch (_ModulationType)
            {
                case ModulationType.Time:
                    _ModulationInput = time;
                    break;
                case ModulationType.Position:
                    _ModulationInput = GetValue(_AttachedGameObject.transform.position, _Axis);
                    break;
                case ModulationType.Velocity:
                    if (_AttachedGameObject.GetComponent<Rigidbody>() != null) // TODO Get component is very slow. Best to check the rigidbody exists one on start up rather than each value get
                        _ModulationInput = GetValue(_AttachedGameObject.GetComponent<Rigidbody>().velocity, _Axis);
                    break;
                case ModulationType.Rotation:
                    _ModulationInput = GetValue(_AttachedGameObject.transform.rotation.eulerAngles, _Axis);
                    break;
                case ModulationType.Scale:
                    _ModulationInput = GetValue(_AttachedGameObject.transform.localScale, _Axis);
                    break;
                case ModulationType.TEST:
                    _ModulationInput = _TestInput;
                    break;
                default:
                    break;
            }

            _ModulationInput = GrainSynthSystem.Map(_ModulationInput, _InputScale.x, _InputScale.y, 0, 1);
        }

        return GetMinMaxValue(_Curve, _ModulationInput);
    }

    public float GetValue(Vector3 input, AxisSelection axis)
    {
        switch (axis)
        {
            case AxisSelection.X:
                return input.x;
            case AxisSelection.Y:
                return input.y;
            case AxisSelection.Z:
                return input.z;
            case AxisSelection.Magnitude:
                return Vector3.SqrMagnitude(input); //TODO did you mean to get sqr magnitude instead of staright magnitude?
            default:
                return 0;
        }
    }

    public float GetMinMaxValue(ParticleSystem.MinMaxCurve minMaxCurve, float norm)
    {
        switch (minMaxCurve.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return minMaxCurve.constant;
            case ParticleSystemCurveMode.Curve:
                return minMaxCurve.curve.Evaluate(norm);
            case ParticleSystemCurveMode.TwoConstants:
                return Mathf.Lerp(minMaxCurve.constantMin, minMaxCurve.constantMax, UnityEngine.Random.value);
            case ParticleSystemCurveMode.TwoCurves:
                return Mathf.Lerp(minMaxCurve.curveMin.Evaluate(norm), minMaxCurve.curveMax.Evaluate(norm), UnityEngine.Random.value);
            default:
                return 0;
        }
    }
}
