using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionForce : MonoBehaviour
{
    private const float _SmoothToZeroRate = 8f;
    public float _DistanceFromForceSource;
    public float _DotVelocityToForceDirection;
    public Vector3 _ForceDirection;
    bool _InForceVolume = false;

    public InteractionParameter _InteractionParam_DistFromSource;
    public InteractionParameter _InteractionParam_InVolume;

    Rigidbody _RigidBody;

    void Start()
    {
        _RigidBody = GetComponent<Rigidbody>();
        _DistanceFromForceSource = float.MaxValue;
    }

    private void Update()
    {
        // Added this bit to roll the force values to zero if they've stopped interacting with the vacuum
        // otherwise they'll be stuck(?)

        if (_DistanceFromForceSource < 0.01f)
            _DistanceFromForceSource = 0;
        else
            _DistanceFromForceSource = Mathf.Lerp(_DistanceFromForceSource, 0f, _SmoothToZeroRate * Time.deltaTime);

        if (_DotVelocityToForceDirection < 0.01f)
            _DotVelocityToForceDirection = 0;
        else
            _DotVelocityToForceDirection = Mathf.Lerp(_DotVelocityToForceDirection, 0f, _SmoothToZeroRate * Time.deltaTime);
    }

    public void UpdateInteractionForce(float dist, Vector3 forceDir, bool inForceVolume = true)
    {
        _ForceDirection = forceDir;
        _DistanceFromForceSource = dist;
        _InForceVolume = inForceVolume;

        _DotVelocityToForceDirection = Vector3.Dot(_RigidBody.velocity.normalized, forceDir.normalized);

        if (_InteractionParam_DistFromSource != null && _InteractionParam_InVolume != null)
        {
            _InteractionParam_DistFromSource.SetAuxValue(_DistanceFromForceSource);
            _InteractionParam_InVolume.SetAuxValue(_InForceVolume ? 1 : 0);
        }

    }
}
