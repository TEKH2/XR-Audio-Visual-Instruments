using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EXP.XR;
using UnityEngine.InputSystem;
using Unity.Entities.UniversalDelegates;

public class Instrument_Vacuum : MonoBehaviour
{
    public float _MaxDist = 20;

    [Range(0,1)]
    public float _TractorBeamScalar = 0;
    [Range(-1, 1)]
    public float _PushPullScalar = 0;
    [Range(-1, 1)]
    public float _TanScalar = 0;

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

    [SerializeField]
    InputActionProperty _RightGripAction;

    [SerializeField]
    InputActionProperty _RightTriggerAction;

    bool _DestroyActive = false;


    private void Start()
    {
        _VacuumMat = _MatMesh.material;

        _RightGripAction.action.started += ctx => _TractorBeamScalar = 1; print("Grip... " + _TractorBeamScalar);
        _RightGripAction.action.performed += ctx => _TractorBeamScalar = 1; 
        _RightGripAction.action.canceled += ctx => _TractorBeamScalar = 0;

        _RightTriggerAction.action.started += ctx => _DestroyActive = true;
        _RightTriggerAction.action.performed += ctx => _DestroyActive = true;
        _RightTriggerAction.action.canceled += ctx => _DestroyActive = false;

        _RightThumbstickAction.action.started += ctx => _PushPullScalar = ctx.ReadValue<Vector2>().y;
        _RightThumbstickAction.action.performed += ctx => _PushPullScalar = ctx.ReadValue<Vector2>().y;
        _RightThumbstickAction.action.canceled += ctx => _PushPullScalar = ctx.ReadValue<Vector2>().y;

        _RightThumbstickAction.action.started += ctx => _TanScalar = ctx.ReadValue<Vector2>().x;
        _RightThumbstickAction.action.performed += ctx => _TanScalar = ctx.ReadValue<Vector2>().x;
        _RightThumbstickAction.action.canceled += ctx => _TanScalar = ctx.ReadValue<Vector2>().x;
    }

    private void Update()
    {      
        _VacuumMat.SetFloat("_Speed", -_PushPullScalar);
        _VacuumMat.SetFloat("_Alpha", Mathf.Abs(_PushPullScalar + _TractorBeamScalar));
    }

    private void FixedUpdate()
    {
        _TotalVacuumedMass = 0;

        foreach (GameObject item in _ObjectsCurrentBeingVacuumed)
        {
            _TotalVacuumedMass += item.GetComponent<Rigidbody>().mass * 
                Mathf.Clamp(1 - 10 * Vector3.Distance(item.transform.position, transform.parent.position) / _MaxDist, 0f, 0.7f) *
                Mathf.Abs(Mathf.Min(_PushPullScalar, 0));
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

            if (dist < _DestroyRadius && _PushPullScalar < 0 && _DestroyActive)
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

    public float _ForceTowardSource = .5f;
    public float _ForceAlongTangent = .5f;

    public float _GravitationalRadius = 4;

    [Header("Tractor Beam")]
    public float _MinTractorBeamVel = 0;
    public float _MaxTractorBeamVel = 2;
    public float _MaxTractorBeamForce = 20;
    public float _TractorBeamGain = 5f;

    Vector3 _LastPointOnLine;
    Vector3 _LastRBPos;

    Vector3 VacuumRB(Rigidbody rb)
    {
        // Line force
        Vector3 linePoint = NearestPointOnLine(transform.position, transform.forward, rb.transform.position, _MaxDist);       
        Vector3 vecToProjectedPoint = rb.gameObject.transform.position - linePoint;
       

        //---  TAN FORCE      
        Vector3 dirToProjectedPoint = vecToProjectedPoint.normalized;
        Vector3 tangentialForce = Vector3.Cross(transform.forward, dirToProjectedPoint);
        tangentialForce *= _ForceAlongTangent;
        rb.AddForce(tangentialForce * _TanScalar);


        //---   PUSH PULL FORCE
        Vector3 directionTowardSource = (rb.transform.position - transform.position).normalized;
        directionTowardSource *= _ForceTowardSource;
        rb.AddForce(directionTowardSource * -_PushPullScalar);


        //---   TRACTOR BEAM TO PROJECTED POINT ON LINE
        // MOVE TO TARGET
        Vector3 distToTarget = linePoint - rb.transform.position;
        // CALC A TARGET VEL PROPORTIONAL TO DISTANCE (CLAMPED TO MAXVEL)
        Vector3 tgtVel = Vector3.ClampMagnitude(_MinTractorBeamVel * distToTarget, _MaxTractorBeamVel);
        // CALCULATE THE VELOCITY ERROR
        Vector3 error = tgtVel - rb.velocity;
        // CALC A FORCE PROPORTIONAL TO THE ERROR (CLAMPED TO MAXFORCE)
        Vector3 tractorBeamForce = Vector3.ClampMagnitude(_TractorBeamGain * error, _MaxTractorBeamForce);
        // ADD FORCE
        rb.AddForce(tractorBeamForce * _TractorBeamScalar);


        // DEBUG
        _LastPointOnLine = linePoint;
        _LastRBPos = rb.transform.position;

        return (tangentialForce * _TanScalar) + (directionTowardSource * -_PushPullScalar) + (tractorBeamForce * _TractorBeamScalar);
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
