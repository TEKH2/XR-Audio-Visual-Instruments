using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UIElements;

// TODO One shot, hold, Loop
// Play pause stop
// Output scalar
// behaviors Idle, interaction, collision, moving

public class EmitBehaviour : MonoBehaviour
{
    public enum State
    {
        Idle,
        Moving,
        Collided,
        Interacting
    }

    public State _State = State.Idle;

    public EmitterBehavior _IdleBehavior;
    public EmitterBehavior _MovingBehavior;
    public EmitterBehavior _CollidingBehavior;
    public EmitterBehavior _InteractingBehavior;

    public EmitterBehavior _ActiveBehavior;


    public string _BehaviourName;
    public GrainEmitterAuthoring _Emitter;

    [Header("Global Values")]
    public float _BehaviourDuration = 4000;
    private float _Timer;
    private float _TimerPrevious;
    [Range(0,1)]
    public float _Norm;
   
    public bool _Play = true;
    private bool _PlayPreviously = true;
    public bool _Loop = true;
    public bool _SilenceWhenDone = true;


    [Header("DEBUG")]
    public bool _Active = true;

    void Start()
    {
        _IdleBehavior.Init(gameObject);
        _MovingBehavior.Init(gameObject);
        _CollidingBehavior.Init(gameObject);
        _InteractingBehavior.Init(gameObject);
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

            switch (_State)
            {
                case State.Idle:
                    _ActiveBehavior = _IdleBehavior;
                    break;
                case State.Moving:
                    _ActiveBehavior = _MovingBehavior;
                    break;
                case State.Interacting:
                    _ActiveBehavior = _InteractingBehavior;
                    break;
                case State.Collided:
                    _ActiveBehavior = _CollidingBehavior;
                    break;
            }


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
                _Emitter._EmissionProps._Playhead = _ActiveBehavior._Playhead.GetValue(_Norm);
                _Emitter._EmissionProps._Cadence = _ActiveBehavior._Cadence.GetValue(_Norm);
                _Emitter._EmissionProps._Duration = _ActiveBehavior._Duration.GetValue(_Norm);
                _Emitter._EmissionProps._Transpose = _ActiveBehavior._Transpose.GetValue(_Norm);
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