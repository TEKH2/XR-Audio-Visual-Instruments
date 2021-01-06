using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Instrument_Vaccuum : MonoBehaviour
{
    public float _MaxDist = 20;

    public LayerMask _LayerMask;
    public float _ForceStrength = 10;

    public AnimationCurve _FallOff;

    // Test for later to get a laggy line
    Vector3[] _ForwardDirections;
    int _SegementCount = 20;


    void UpdateForwardDirections()
    {
        for (int i = 1; i < _ForwardDirections.Length; i++)
        {
            _ForwardDirections[i] = _ForwardDirections[i-1];
        }

        _ForwardDirections[0] = transform.forward;


        for (int i = 0; i < _SegementCount; i++)
        {
            float norm = i / (_SegementCount - 1f);
            Vector3 forward = _ForwardDirections[i] * norm * _MaxDist;
            //_Line.SetPosition(i, transform.position + forward);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.attachedRigidbody)
        {
            float dist = Vector3.Distance(transform.position, other.transform.position);
            float normDist = dist / _MaxDist;
            float strength = _FallOff.Evaluate(normDist) * _ForceStrength;
            Vector3 direction = (other.transform.position - transform.position).normalized;

            other.attachedRigidbody.AddForce(direction * strength);
        }
    }

    private void OnDrawGizmos()
    {
        //if (_SpherecastTransform != null)
        //{
        //    for (int i = 0; i < 5; i++)
        //    {
        //        float norm = i / 4f;
        //        Gizmos.DrawWireSphere(_SpherecastTransform.position + (_SpherecastTransform.forward * norm * _MaxDist), _Radius);
        //    }
        //}
    }
}
