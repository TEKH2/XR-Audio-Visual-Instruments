using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;



[System.Serializable]
public struct EmitterBehaviourData
{
    public EmitterStateBehavior.EventType _EventType;
    public int _BehaviourDuration;  

    [Header("Grain Parameters")]
    public BehaviourParameter _Playhead;
    public BehaviourParameter _Cadence;
    public BehaviourParameter _Duration;
    public BehaviourParameter _Volume;
    public BehaviourParameter _Transpose;
}

[System.Serializable]
public class EmitterStateBehavior
{
    public enum EventType
    {
        OneShot,
        Hold,
        Loop
    }

    public EmitterBehaviourData _Data;
    public EmitterStateBehaviourDataTemplate _Template;

    private float _Timer = 0;
    private float _TimerNorm = 0;
    public bool _Active = false;
    private bool _Started = false;
    private bool _TimerEnd = false;

    private readonly Vector2 PlayheadRange = new Vector2(0, 1);
    private readonly Vector2 CadenceRange = new Vector2(3, 200);
    private readonly Vector2 DurationRange = new Vector2(5, 500);
    private readonly Vector2 TransposeRange = new Vector2(-3, 3);

    private GrainEmitterAuthoring _Emitter;
    private Collision _Collision;

    public void Init(GameObject go, GrainEmitterAuthoring emitter)
    {
        _Data._Playhead.SetGameObject(go, PlayheadRange);
        _Data._Cadence.SetGameObject(go, CadenceRange);
        _Data._Duration.SetGameObject(go, DurationRange);
        _Data._Transpose.SetGameObject(go, TransposeRange);

        _Emitter = emitter;
    }

    public void Reset()
    {
        _Active = true;
        _Started = false;
        _TimerEnd = false;
    }

   

    public void OnUpdate()
    {
        if (_Active)
        {
            // When starting new playback, reset the timer unless its a loop
            if (!_Started)
            {
                _Started = true;
                _TimerEnd = false;
                if (_Data._EventType != EventType.Loop)
                    _Timer = 0;
            }

            // Update timer, or deactivate if OneShot and has ended
            if (!_TimerEnd)
            {
                _Timer += Time.deltaTime * 1000;

                if (_Timer > _Data._BehaviourDuration)
                    switch(_Data._EventType)
                    {
                        case EventType.OneShot:
                            _TimerEnd = true;
                            _Active = false;
                            _Collision = null;
                            break;
                        case EventType.Hold:
                            _TimerEnd = true;
                            _Timer = _Data._BehaviourDuration - 1;
                            break;
                        case EventType.Loop:
                            _Timer %= _Data._BehaviourDuration;
                            break;
                    }    
            }

            if (_Active)
            {
                _TimerNorm = _Timer / _Data._BehaviourDuration;
                _Emitter._EmissionProps._Playhead = _Data._Playhead.GetValue(_TimerNorm, _Collision);
                _Emitter._EmissionProps._Cadence = _Data._Cadence.GetValue(_TimerNorm, _Collision);
                _Emitter._EmissionProps._Duration = _Data._Duration.GetValue(_TimerNorm, _Collision);
                _Emitter._EmissionProps._Transpose = _Data._Transpose.GetValue(_TimerNorm, _Collision);
            }
        }
    }

    public void SetCollisionData(Collision collision)
    {
        _Collision = collision;
    }

    public void CopyTemplate()
    {
        if (_Template != null)
        {
            _Data._EventType = _Template._Data._EventType;
            _Data._BehaviourDuration = _Template._Data._BehaviourDuration;

            _Data._Playhead.CopyData(_Template._Data._Playhead);
            _Data._Cadence.CopyData(_Template._Data._Cadence);
            _Data._Transpose.CopyData(_Template._Data._Transpose);
            _Data._Volume.CopyData(_Template._Data._Volume);
            _Data._Duration.CopyData(_Template._Data._Duration);


        }
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

    public float _InputLow;
    public float _InputHigh;

    [Header("Output")]
    public float _OutputLow;
    public float _OutputHigh;

    [Header("DEBUG")]
    [Range(0, 1)]
    public float _TestInput = 0;
    public float _ModulationInput = 0;

    public void CopyData(BehaviourParameter behave)
    {
        _Curve.mode = behave._Curve.mode;
        _Curve.curve.keys = behave._Curve.curve.keys;

       _ModulationType = behave._ModulationType;

        _Axis = behave._Axis;

        _InputLow = behave._InputLow;
        _OutputHigh = behave._OutputHigh;

        _ModulationInput = behave._ModulationInput;
    }

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

            _ModulationInput = GrainSynthSystem.Map(_ModulationInput, _InputLow, _InputHigh, 0, 1);
        }

        return GrainSynthSystem.Map(GetMinMaxValue(_Curve, _ModulationInput), 0, 1, _OutputLow, _OutputHigh);
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
