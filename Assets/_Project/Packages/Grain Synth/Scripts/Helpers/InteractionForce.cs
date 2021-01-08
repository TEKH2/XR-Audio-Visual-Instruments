using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionForce : MonoBehaviour
{
    public float _DistanceFromForceSource;
    public float _DotVelocityToForceDirection;
    public Vector3 _ForceDirection;
    bool _InForceVolume = false;

    public InteractionParameter _InteractionParam_DistFromSource;
    public InteractionParameter _InteractionParam_InVolume;

    Rigidbody _RB;

    // Start is called before the first frame update
    void Start()
    {
        _RB = GetComponent<Rigidbody>();

        _DistanceFromForceSource = float.MaxValue;
    }

    // Update is called once per frame
    public void UpdateInteractionForce(float dist, Vector3 forceDir, bool inForceVolume = true)
    {
        _ForceDirection = forceDir;
        _DistanceFromForceSource = dist;
        _InForceVolume = inForceVolume;

        _DotVelocityToForceDirection = Vector3.Dot(_RB.velocity.normalized, forceDir.normalized);

        _InteractionParam_DistFromSource.SetAuxValue(_DistanceFromForceSource);
        _InteractionParam_InVolume.SetAuxValue(_InForceVolume ? 1 : 0);

    }
}
