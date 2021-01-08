using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EXP.XR;
using Unity.Entities.UniversalDelegates;

public class Instrument_Vaccuum : MonoBehaviour
{
    public float _MaxDist = 20;

    public float _ForceStrength = 10;

    public AnimationCurve _FallOff;

    // Test for later to get a laggy line
    Vector3[] _ForwardDirections;
    int _SegementCount = 20;

    float _ThumbScalar = 0;
    float _DestroyRadius = .2f;

    public float forceTowardLine = .5f;
    public float forceTowardSource = .5f;

    private void Start()
    {
        XRControllers.Instance._RightControllerFeatures._XRVector2Dict[XRVector2s.PrimaryAxis].OnValueUpdate.AddListener((Vector2 v) => _ThumbScalar = v.y );
    }

    private void Update()
    {
        _ThumbScalar = Input.GetMouseButton(0) ? 1 : 0;

        if (Input.GetKey(KeyCode.DownArrow))
            _ThumbScalar = -1;
        else
            _ThumbScalar = 0;
    }


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
        Vacuum(other, false);
    }

    void OnTriggerEnter(Collider other)
    {
        Vacuum(other, true);
    }

    void Vacuum(Collider other, bool inTrigger)
    {
        if (other.attachedRigidbody)
        {
            float dist = Vector3.Distance(transform.position, other.transform.position);
            Vector3 directionTowardLine = Vector3.zero;
            Vector3 directionTowardSource = Vector3.zero;
            Vector3 direction = Vector3.zero;
            Vector3 force = Vector3.zero;

            if (dist < _DestroyRadius && _ThumbScalar < 0)
            {
                // Destroy
                Destroy(other.gameObject);
            }
            else
            {
                float normDistToSource = dist / _MaxDist;

                Vector3 linePoint = NearestPointOnLine(transform.position, transform.forward, other.transform.position);

                directionTowardLine = (other.transform.position - linePoint).normalized;
                directionTowardSource = (other.transform.position - transform.position).normalized;

                float falloffStrength = _FallOff.Evaluate(normDistToSource) * _ForceStrength * _ThumbScalar;

                force = (forceTowardLine * directionTowardLine * falloffStrength) + (forceTowardSource * directionTowardSource * falloffStrength);

                other.attachedRigidbody.AddForce(force);
            }

            //---   INTERACTION FORCE
            InteractionForce interactionForce = other.gameObject.GetComponent<InteractionForce>();
            if(interactionForce != null)
            {
                interactionForce.UpdateInteractionForce(dist, force, inTrigger);
            }
        }
    }

    //linePnt - point the line passes through
    //lineDir - unit vector in direction of line, either direction works
    //pnt - the point to find nearest on line for
    public static Vector3 NearestPointOnLine(Vector3 linePnt, Vector3 lineDir, Vector3 pnt)
    {
        lineDir.Normalize();//this needs to be a unit vector
        var v = pnt - linePnt;
        var d = Vector3.Dot(v, lineDir);
        return linePnt + lineDir * d;
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
