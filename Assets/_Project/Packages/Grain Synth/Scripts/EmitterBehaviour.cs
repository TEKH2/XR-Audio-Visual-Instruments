using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "EmitBehavior", menuName = "Grain Synth/Emitter Behavior", order = 1)]
public class EmitterBehaviour : ScriptableObject
{
    public enum EventType
    {
        OneShot,
        Hold,
        Loop
    }

    [Header("Event")]
    public EventType _EventType;
    public int _BehaviourDuration = 1000;
    private float _Timer = 0;
    private float _TimerNorm = 0;
    public bool _Active = false;
    private bool _Started = false;
    private bool _Ended = false;

    [Header("Grain Parameters")]
    public BehaviourParameter _Playhead;
    public BehaviourParameter _Cadence;
    public BehaviourParameter _Duration;
    public BehaviourParameter _Transpose;

    private readonly Vector2 PlayheadRange = new Vector2(0, 1);
    private readonly Vector2 CadenceRange = new Vector2(3, 200);
    private readonly Vector2 DurationRange = new Vector2(5, 500);
    private readonly Vector2 TransposeRange = new Vector2(-3, 3);

    private GrainEmitterAuthoring _Emitter;
    private Collision _Collision;

    public void Init(GameObject go, GrainEmitterAuthoring emitter)
    {
        _Playhead.SetGameObject(go, PlayheadRange);
        _Cadence.SetGameObject(go, CadenceRange);
        _Duration.SetGameObject(go, DurationRange);
        _Transpose.SetGameObject(go, TransposeRange);

        _Emitter = emitter;
    }

    public void Reset()
    {
        _Active = true;
        _Started = false;
        _Ended = false;
    }


    public void OnUpdate()
    {
        if (_Active)
        {
            // When starting new playback, reset the timer unless its a loop
            if (!_Started)
            {
                _Started = true;
                _Ended = false;
                if (_EventType != EventType.Loop)
                    _Timer = 0;
            }

            // Update timer, or deactivate if OneShot and has ended
            if (!_Ended)
            {
                _Timer += Time.deltaTime * 1000;

                if (_EventType == EventType.OneShot)
                    if (_Timer > _BehaviourDuration)
                    {
                        _Ended = true;
                        _Active = false;
                        _Collision = null;
                    }
                else if (_EventType == EventType.Hold)
                    if (_Timer > _BehaviourDuration)
                    {
                        _Ended = true;
                        _Timer = _BehaviourDuration - 1;
                    }
                else if (_EventType == EventType.Loop)
                {
                    _Timer += Time.deltaTime * 1000;
                    _Timer %= _BehaviourDuration;
                }
            }

            if (_Active)
            {
                _TimerNorm = _Timer / _BehaviourDuration;
                _Emitter._EmissionProps._Playhead = _Playhead.GetValue(_TimerNorm, _Collision);
                _Emitter._EmissionProps._Cadence = _Cadence.GetValue(_TimerNorm, _Collision);
                _Emitter._EmissionProps._Duration = _Duration.GetValue(_TimerNorm, _Collision);
                _Emitter._EmissionProps._Transpose = _Transpose.GetValue(_TimerNorm, _Collision);
            }
        }
    }

    public void SetCollisionData(Collision collision)
    {
        _Collision = collision;
    }
}

[System.Serializable]
public class BehaviourParameter
{ 
    public void SetGameObject(GameObject gameObject, Vector2 range)
    {
        _AttachedGameObject = gameObject;
        _RigidBody = gameObject.GetComponent<Rigidbody>();
        _OutputLow = range.x;
        _OutputHigh = range.y;
    }

    public enum ModulationType
    {
        Time,
        Position,
        Velocity,
        Rotation,
        Scale,
        Collision,
        Off
    }

    public enum AxisSelection
    {
        Magnitude,
        X,
        Y,
        Z
    }

    private GameObject _AttachedGameObject;
    private Rigidbody _RigidBody;

    public ParticleSystem.MinMaxCurve _Curve;

    [Header("Input")]
    public ModulationType _ModulationType;
    public AxisSelection _Axis;

    public float _InputLow = 0;
    public float _InputHigh = 1;

    [Header("Output")]
    public float _OutputLow = 0;
    public float _OutputHigh = 1;

    [Header("DEBUG")]
    [Range(0, 1)]
    public float _TestInput = 0;
    public float _ModulationInput = 0;

    public float GetValue(float time, Collision collision)
    {
        if (_ModulationType == ModulationType.Time)
            _ModulationInput = time;
        else
        {
            switch (_ModulationType)
            {
                case ModulationType.Position:
                    _ModulationInput = GetValue(_AttachedGameObject.transform.position, _Axis);
                    break;
                case ModulationType.Velocity:
                    _ModulationInput = GetValue(_RigidBody.velocity, _Axis);
                    break;
                case ModulationType.Rotation:
                    _ModulationInput = GetValue(_AttachedGameObject.transform.rotation.eulerAngles, _Axis);
                    break;
                case ModulationType.Scale:
                    _ModulationInput = GetValue(_AttachedGameObject.transform.localScale, _Axis);
                    break;
                case ModulationType.Collision:
                    if (collision != null)
                        _ModulationInput = GetValue(collision.impulse, _Axis);
                    break;
                case ModulationType.Off:
                    _ModulationInput = _TestInput;
                    break;
                default:
                    break;
            }

            _ModulationInput = GrainSynthSystem.Map(_ModulationInput, _InputLow, _InputHigh, _OutputLow, _OutputHigh);
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
                return Vector3.Magnitude(input);
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
