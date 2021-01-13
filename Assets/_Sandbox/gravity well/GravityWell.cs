using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityWell : MonoBehaviour
{
    public float _ForceTowardLine = .5f;
    public float _ForceTowardSource = .5f;
    public float _ForceAlongTangent = .5f;
    public float _WellLength = 8;

    public Rigidbody[] _AllRigidBodies;

    Vector3 _LinePoint;
    Vector3 _DirectionToLine;
    Vector3 _DirectionToSource;
    Vector3 _TanForce;

    public float _GravitationalRadius = 4;


    public float minVel = 0;
    public float maxVel = 2;
    public float maxForce = 20;
    public float gain = 5f;

    // Start is called before the first frame update
    void Start()
    {
        _AllRigidBodies = FindObjectsOfType<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < _AllRigidBodies.Length; i++)
        {
            Vacuum(_AllRigidBodies[i], i==0);
        }
    }

    void Vacuum(Rigidbody rb, bool storeDebug)
    {
        float dist = Vector3.Distance(transform.position, rb.gameObject.transform.position);
        Vector3 directionTowardLine = Vector3.zero;
        Vector3 directionTowardSource = Vector3.zero;
        Vector3 direction = Vector3.zero;
        Vector3 force = Vector3.zero;

        float normDistToSource = dist / _WellLength;

        


        // Line force
        Vector3 linePoint = NearestPointOnLine(transform.position, transform.forward, rb.transform.position, _WellLength);
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

        if (storeDebug)
        {
            _DirectionToLine = directionTowardLine;
            _DirectionToSource = directionTowardSource;
            _LinePoint = linePoint;
            _TanForce = tangentialForce;
        }


       
        // Move to target
        Vector3 distToTarget = linePoint - rb.transform.position;       
        // calc a target vel proportional to distance (clamped to maxVel)
        Vector3 tgtVel = Vector3.ClampMagnitude(minVel * distToTarget, maxVel);
        // calculate the velocity error
        Vector3 error = tgtVel - rb.velocity;
        // calc a force proportional to the error (clamped to maxForce)
        Vector3 toTargetForce = Vector3.ClampMagnitude(gain * error, maxForce);

        rb.AddForce(toTargetForce);


        force = directionTowardLine + directionTowardSource + tangentialForce;
        rb.AddForce(force);
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


    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * _WellLength);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(_AllRigidBodies[0].transform.position, _AllRigidBodies[0].transform.position + _DirectionToLine);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_LinePoint, .2f);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(_AllRigidBodies[0].transform.position, _AllRigidBodies[0].transform.position + _DirectionToSource);
    }
}
