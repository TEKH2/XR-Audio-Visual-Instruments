using Ludiq;
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
        Interacting,
        Off
    }

    public string _BehaviourName;
    public GrainEmitterAuthoring _Emitter;

    [Header("Behaviour States")]
    public State _State = State.Idle;
    public EmitterBehaviour _ActiveBehaviour;

    public EmitterBehaviour _IdleBehaviour;
    public EmitterBehaviour _MovingBehaviour;
    public EmitterBehaviour _CollisionBehaviour;
    public EmitterBehaviour _InteractionBehaviour;

    [Header("State Configuration")]

    public bool _IdleEnabled = true;
    public bool _MovingEnabled = true;
    public bool _CollisionEnabled = true;
    public bool _InteractionEnabled = true;

    public float _MovingSpeedThreshold = 0.1f;
    public bool _RotateTriggersMoving = false;
    public float _RotationSpeedThreshold = 0.1f;

    private Rigidbody _RigidBody;

    [Header("DEBUG")]
    public bool _Active = true;

    void Start()
    {
        if (GetComponent<Rigidbody>() != null)
            _RigidBody = GetComponent<Rigidbody>();
        else
        {
            _RigidBody = gameObject.AddComponent<Rigidbody>();
            _RigidBody.isKinematic = true;
        }

        _IdleBehaviour.Init(gameObject, _Emitter);
        _MovingBehaviour.Init(gameObject, _Emitter);
        _CollisionBehaviour.Init(gameObject, _Emitter);
        _InteractionBehaviour.Init(gameObject, _Emitter);

        if (_IdleEnabled)
        {
            _State = State.Idle;
            _ActiveBehaviour = _IdleBehaviour;
            _IdleBehaviour._Active = true;
        }
        else
        {
            _State = State.Off;
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (_CollisionEnabled)
        {
            _State = State.Collided;
            _CollisionBehaviour.SetCollisionData(collision);
            _CollisionBehaviour.Reset();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Clear finished behaviour
        if (_ActiveBehaviour != null && !_ActiveBehaviour._Active)
            if (_IdleEnabled)
            {
                _State = State.Idle;
                _IdleBehaviour._Active = true;
            }
            else
                _State = State.Off;

        // If behaviour isn't an active collsion - check state in order of priority
        if (_State != State.Collided)
        {
            if (_InteractionEnabled)
            {
                // TODO define interaction triggers
            }
            else if (_MovingEnabled)
                if (_RigidBody.velocity.magnitude > _MovingSpeedThreshold || (_RotateTriggersMoving && _RigidBody.angularVelocity.magnitude > _RotationSpeedThreshold))
                {
                    _State = State.Moving;
                }
                else if (_IdleEnabled)
                    _State = State.Idle;
                else
                    _State = State.Off;
        }

        // Switch to requried behaviour
        switch (_State)
        {
            case State.Idle:
                _ActiveBehaviour = _IdleBehaviour;
                break;
            case State.Moving:
                _ActiveBehaviour = _MovingBehaviour;
                break;
            case State.Interacting:
                _ActiveBehaviour = _InteractionBehaviour;
                break;
            case State.Collided:
                _ActiveBehaviour = _CollisionBehaviour;
                break;
        }


        if (_State != State.Off || _ActiveBehaviour != null)
        {
            _ActiveBehaviour.OnUpdate();
        }
    }
}