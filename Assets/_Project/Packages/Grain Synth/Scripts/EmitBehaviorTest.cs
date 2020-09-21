using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;


/// <summary>
///  Usability in edotir
///  Multi curve editing
/// </summary>
public class EmitBehaviorTest : MonoBehaviour
{
    #region ----------------- ENUMS
    public enum State
    {
        Playing,
        Stopped,
        Paused
    }

    public enum EmitPropertyType
    {
        Cadence,
        Playhead,
        Duration,
        Volume,
        Transpose
    }

    public enum PlaybackType
    {
        OneShot,
        Looping,
        HoldOnFinish
    }
    #endregion

    public GrainEmitterAuthoring _Emitter;

    public PlaybackType _Type = PlaybackType.OneShot;


    public float _Duration = 3;
    float _Timer;
    float _Norm;
    State _State = State.Stopped;

    public List<ScaledMinMaxCurve> _PropertieCurves;

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.P))
        {
            SetState(State.Playing);
        }

        if (_State == State.Playing)
        {
            // Update timer
            _Timer += Time.deltaTime;
            _Norm = Mathf.Clamp01(_Timer / _Duration);

            UpdateValues();

            if (_Timer >= _Duration)
            {
                _State = State.Stopped;
                _Timer = 0;
            }
        }
    }

    void UpdateValues()
    {
        for (int i = 0; i < _PropertieCurves.Count; i++)
        {
            switch(_PropertieCurves[i]._PropertyType)
            {
                case EmitPropertyType.Cadence:
                    _Emitter._EmissionProps.Cadence = _PropertieCurves[i].GetMinMaxValue(_Norm);
                    break;
                case EmitPropertyType.Duration:
                    _Emitter._EmissionProps.Duration = _PropertieCurves[i].GetMinMaxValue(_Norm);
                    break;
                case EmitPropertyType.Playhead:
                    _Emitter._EmissionProps._Playhead = _PropertieCurves[i].GetMinMaxValue(_Norm);
                    break;
                case EmitPropertyType.Transpose:
                    _Emitter._EmissionProps._Transpose = _PropertieCurves[i].GetMinMaxValue(_Norm);
                    break;
                case EmitPropertyType.Volume:
                    _Emitter._EmissionProps.Volume = _PropertieCurves[i].GetMinMaxValue(_Norm);
                    break;
            }
        }
    }

    public void SetState(State state)
    {
        switch(state)
        {
            case State.Playing:
                _Timer = 0;
                _Norm = 0;
                break;
            case State.Paused:                
                break;
            case State.Stopped:
                _Timer = 0;
                _Norm = 0;
                UpdateValues();
                break;
        }

        _State = state;
    }       
}

[System.Serializable]
public class ScaledMinMaxCurve
{
    public EmitBehaviorTest.EmitPropertyType _PropertyType = EmitBehaviorTest.EmitPropertyType.Volume;
    public ParticleSystem.MinMaxCurve _VolumeCurve;
    public Vector2 _OutputRange = new Vector2(0, 1);

    public float GetMinMaxValue(float norm)
    {
        float curveVal;
        switch (_VolumeCurve.mode)
        {
            case ParticleSystemCurveMode.Constant:
                curveVal = _VolumeCurve.constant;
                break;
            case ParticleSystemCurveMode.Curve:
                curveVal = _VolumeCurve.curve.Evaluate(norm);
                break;
            case ParticleSystemCurveMode.TwoConstants:
                curveVal = Mathf.Lerp(_VolumeCurve.constantMin, _VolumeCurve.constantMax, UnityEngine.Random.value);
                break;
            case ParticleSystemCurveMode.TwoCurves:
                curveVal = Mathf.Lerp(_VolumeCurve.curveMin.Evaluate(norm), _VolumeCurve.curveMax.Evaluate(norm), UnityEngine.Random.value);
                break;
            default:
                curveVal = 0;
                break;
        }

        return Mathf.Lerp(_OutputRange.x, _OutputRange.y, curveVal);
    }
}
