using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UIElements;

[System.Serializable]
public class BehaviourParameter
{    public void SetGameObject(GameObject gameObject)
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
    public float _ModulationInput = 0;
    public Vector2 _InputScale = new Vector2(0.0f, 1.0f);

    [Range(0,1)]
    public float _TestInput = 0;

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



public class EmitBehaviour : MonoBehaviour
{
    public enum BehaviourType
    {
        Move,
        Collide
    }

    public string _BehaviourName;
    public BehaviourType _Behaviour;
    public GrainEmitterAuthoring _Emitter;

    [Header("Global Values")]
    public float _BehaviourDuration = 4000;
    private float _Timer;
    private float _TimerPrevious;
    [Range(0,1)]
    public float _Norm;
    public bool _Active = true;
    public bool _Play = true;
    private bool _PlayPreviously = true;
    public bool _Loop = true;
    public bool _SilenceWhenDone = true;

    [Header("Grain Modulations")]
    public BehaviourParameter _Playhead;
    public BehaviourParameter _Cadence;
    public BehaviourParameter _Duration;
    public BehaviourParameter _Transpose;

    void Start()
    {
        _Playhead.SetGameObject(gameObject);
        _Cadence.SetGameObject(gameObject);
        _Duration.SetGameObject(gameObject);
        _Transpose.SetGameObject(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        // If not looping, but play has turned to active
        if (!_Loop && _Play && !_PlayPreviously)
        {
            ResetTimer();
        }

        if (_Active && _Play)
        {
            if (!_PlayPreviously)
                _Emitter._EmissionProps._Playing = true;

            _Timer += Time.deltaTime * 1000;
            _Timer %= _BehaviourDuration;

            // End of non-looped trigger
            if (!_Loop && _Timer < _TimerPrevious)
            {
                _Play = false;
                ResetTimer();

                if (_SilenceWhenDone)
                    _Emitter._EmissionProps._Playing = false;
            }
            else
            {
                _Norm = _Timer / _BehaviourDuration;
                _Emitter._EmissionProps._Playhead = _Playhead.GetValue(_Norm);
                _Emitter._EmissionProps._Cadence = _Cadence.GetValue(_Norm);
                _Emitter._EmissionProps._Duration = _Duration.GetValue(_Norm);
                _Emitter._EmissionProps._Transpose = _Transpose.GetValue(_Norm);
            }
        }

        _TimerPrevious = _Timer;
        _PlayPreviously = _Play;
    }

    public void ResetTimer()
    {
        _Timer = 0;
    }
}