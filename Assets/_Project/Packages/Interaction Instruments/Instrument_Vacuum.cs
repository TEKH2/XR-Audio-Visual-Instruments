using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EXP.XR;
using UnityEngine.InputSystem;
using Unity.Entities.UniversalDelegates;

public class Instrument_Vacuum : MonoBehaviour
{
    public float _MaxDist = 20;

    public float _ForceStrength = 10;

    public AnimationCurve _FallOff;

    // Test for later to get a laggy line
    Vector3[] _ForwardDirections;
    int _SegementCount = 20;

    public float _ThumbScalar = 0;
    float _DestroyRadius = .4f;

    public float forceTowardLine = .5f;
    public float forceTowardSource = .5f;

    public float _TotalVacuumedMass = 0;

    public List<GameObject> _ObjectsCurrentBeingVacuumed;

    public bool _UseKB = false;
    public bool _UseMouse = false;

    public MeshRenderer _MatMesh;
    Material _VacuumMat;

    [SerializeField]
    InputActionProperty _RightThumbstickAction;
   

    private void Start()
    {
        _VacuumMat = _MatMesh.material;

        _RightThumbstickAction.action.started += ctx => _ThumbScalar = ctx.ReadValue<Vector2>().y; print("Started... " + _ThumbScalar);
        _RightThumbstickAction.action.performed += ctx => _ThumbScalar = ctx.ReadValue<Vector2>().y; print("performed... " + _ThumbScalar);
        _RightThumbstickAction.action.canceled += ctx => _ThumbScalar = ctx.ReadValue<Vector2>().y; print("cancelled... " + _ThumbScalar);
    }

    private void Update()
    {
        //if (_UseKB)
        //{
        //    if (Input.GetKey(KeyCode.DownArrow))
        //        _ThumbScalar = -1;
        //    else if (Input.GetKey(KeyCode.UpArrow))
        //        _ThumbScalar = 1;
        //    else
        //        _ThumbScalar = 0;
        //}

        //if(Input.GetKeyDown(KeyCode.D))
        //{
        //    GameObject go = FindObjectOfType<InteractionForce>().gameObject;
        //    if(go != null)
        //        DestroyEmitter(go);
        //}

        _VacuumMat.SetFloat("_Speed", -_ThumbScalar);
        _VacuumMat.SetFloat("_Alpha", Mathf.Abs(_ThumbScalar));
    }

    private void FixedUpdate()
    {
        _TotalVacuumedMass = 0;

        foreach (GameObject item in _ObjectsCurrentBeingVacuumed)
        {
            _TotalVacuumedMass += item.GetComponent<Rigidbody>().mass * 
                Mathf.Clamp(1 - 10 * Vector3.Distance(item.transform.position, transform.parent.position) / _MaxDist, 0f, 0.7f) *
                Mathf.Abs(Mathf.Min(_ThumbScalar, 0));
        }
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
        Vacuum(other, true);
    }

    void OnTriggerEnter(Collider other)
    {
        _ObjectsCurrentBeingVacuumed.Add(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        Vacuum(other, false);
        _ObjectsCurrentBeingVacuumed.Remove(other.gameObject);
    }

    void Vacuum(Collider other, bool inTrigger)
    {
        if (other.attachedRigidbody)
        {
            float dist = Vector3.Distance(transform.parent.position, other.transform.position);
            Vector3 force = Vector3.zero;

            if (dist < _DestroyRadius && _ThumbScalar < 0)
                DestroyEmitter(other.gameObject);
            else
            {
                force = VacuumRB(other.attachedRigidbody);
            }

            //---   INTERACTION FORCE
            InteractionForce interactionForce = other.gameObject.GetComponent<InteractionForce>();
            if(interactionForce != null)
            {
                interactionForce.UpdateInteractionForce(dist, force, inTrigger);
            }
        }
    }

    public float _ForceTowardLine = .5f;
    public float _ForceTowardSource = .5f;
    public float _ForceAlongTangent = .5f;

    public float _GravitationalRadius = 4;

    public float _MinVel = 0;
    public float _MaxVel = 2;
    public float _MaxForce = 20;
    public float _Gain = 5f;

    Vector3 _LastPointOnLine;
    Vector3 _LastRBPos;

    public float _MasterScalar = .5f;

    Vector3 VacuumRB(Rigidbody rb)
    {
        float dist = Vector3.Distance(transform.position, rb.gameObject.transform.position);
        Vector3 directionTowardLine = Vector3.zero;
        Vector3 directionTowardSource = Vector3.zero;
        Vector3 direction = Vector3.zero;
        Vector3 force = Vector3.zero;

        float normDistToSource = dist / _MaxDist;



        // Line force
        Vector3 linePoint = NearestPointOnLine(transform.position, transform.forward, rb.transform.position, _MaxDist);
        directionTowardLine = (rb.transform.position - linePoint).normalized;
        Vector3 vecToProjectedPoint = rb.gameObject.transform.position - linePoint;
        float lineGravitationalFactor = 1 - (vecToProjectedPoint.magnitude / _GravitationalRadius);
        lineGravitationalFactor = Mathf.Clamp(lineGravitationalFactor, .1f, 1f);
        //directionTowardLine *= lineGravitationalFactor * _ForceTowardLine;
        directionTowardLine *= _ForceTowardLine;

        // Tan force      
        Vector3 dirToProjectedPoint = vecToProjectedPoint.normalized;
        Vector3 tangentialForce = Vector3.Cross(transform.forward, dirToProjectedPoint);

        tangentialForce *= _ForceAlongTangent;

        // Source force
        directionTowardSource = (rb.transform.position - transform.position).normalized;
        directionTowardSource *= _ForceTowardSource;



        // Move to target
        Vector3 distToTarget = linePoint - rb.transform.position;
        // calc a target vel proportional to distance (clamped to maxVel)
        Vector3 tgtVel = Vector3.ClampMagnitude(_MinVel * distToTarget, _MaxVel);
        // calculate the velocity error
        Vector3 error = tgtVel - rb.velocity;
        // calc a force proportional to the error (clamped to maxForce)
        Vector3 toTargetForce = Vector3.ClampMagnitude(_Gain * error, _MaxForce);

        rb.AddForce(toTargetForce * Mathf.Abs(_ThumbScalar) * _MasterScalar);


        force = directionTowardLine + directionTowardSource + tangentialForce;
        rb.AddForce(force * -(_ThumbScalar * _MasterScalar));

        // DEBUG
        _LastPointOnLine = linePoint;
        _LastRBPos = rb.transform.position;

        return force;
    }

    public static Vector3 NearestPointOnLine(Vector3 lineOrigin, Vector3 lineDir, Vector3 worldPosToProject, float maxLineLength = float.MaxValue)
    {
        lineDir.Normalize();//this needs to be a unit vector
        Vector3 vectorToOrigin = worldPosToProject - lineOrigin;
        float dotOriginAndDir = Vector3.Dot(vectorToOrigin, lineDir);
        Vector3 pointOnLine = lineOrigin + lineDir * dotOriginAndDir;

        float distOriginToPoint = Vector3.Distance(pointOnLine, lineOrigin);

        if (maxLineLength != float.MaxValue && distOriginToPoint > maxLineLength)
            pointOnLine = lineOrigin + (lineDir * maxLineLength); // pointOnLine.normalized * maxLineLength;

        if (Vector3.Dot(lineDir, lineOrigin - pointOnLine) > 0)
            pointOnLine = lineOrigin;

        return pointOnLine;
    }


    void DestroyEmitter(GameObject go)
    {
        _ObjectsCurrentBeingVacuumed.Remove(go);
        Destroy(go);
    }  

    private void OnDrawGizmos()
    {
        if(Application.isPlaying)
        {
            Debug.DrawLine(transform.position, transform.position + transform.forward * _MaxDist);
            Debug.DrawLine(_LastPointOnLine, _LastRBPos);
        }

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
