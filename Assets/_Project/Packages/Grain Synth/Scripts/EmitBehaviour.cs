using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public ParticleSystem.MinMaxCurve _Playhead;
    public ParticleSystem.MinMaxCurve _Cadence;
    public ParticleSystem.MinMaxCurve _Duration;
    public ParticleSystem.MinMaxCurve _Transpose;

    public float _LoopDuration = 4;
    private float _Timer;
    private float _TimerPrevious;
    public float _Norm;
    public bool _Active = true;
    public bool _Play = true;
    private bool _PlayPreviously = true;
    public bool _Loop = true;

    public bool _SilenceWhenDone = true;

    private bool _ModEnd = false;

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

            _Timer += Time.deltaTime;
            _Timer %= _LoopDuration;

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
                _Norm = _Timer / _LoopDuration;
                _Emitter._EmissionProps._Playhead = GetMinMaxValue(_Playhead, _Norm);
                _Emitter._EmissionProps._Cadence = GetMinMaxValue(_Cadence, _Norm);
                _Emitter._EmissionProps._Duration = GetMinMaxValue(_Duration, _Norm);
                _Emitter._EmissionProps._Transpose = GetMinMaxValue(_Transpose, _Norm);
            }
        }

        _TimerPrevious = _Timer;
        _PlayPreviously = _Play;
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
                return Mathf.Lerp(minMaxCurve.constantMin, minMaxCurve.constantMax, Random.value); 
            case ParticleSystemCurveMode.TwoCurves:
                return Mathf.Lerp(minMaxCurve.curveMin.Evaluate(norm), minMaxCurve.curveMax.Evaluate(norm), Random.value);
        }

        return 0;
    }


    public void ResetTimer()
    {
        _Timer = 0;
    }
}