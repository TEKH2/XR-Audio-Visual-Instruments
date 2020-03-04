using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowOffset : MonoBehaviour
{
    public enum OffsetType
    {
        DistanceFromParent, // Uses magnitude of offset vector
        LocalToParent,
    }

    public OffsetType _OffsetType = OffsetType.LocalToParent;
    public Transform m_FollowTform;
    public Vector3 m_Offset = Vector3.up;
    float _Distance = 0;
    public float m_Smoothing = 4;

    bool _IsLeadTransform = false;

    private void Start()
    {
        _Distance = m_Offset.magnitude;

        _IsLeadTransform = !m_FollowTform.GetComponent<FollowOffset>();
    }

    void Update ()
    {
       

        Vector3 targetPos;
        if (_OffsetType == OffsetType.LocalToParent)
        {
            // Target position to follow
            targetPos = m_FollowTform.TransformPoint(m_Offset);
        }
        else
        {
            Vector3 directionToTarget = transform.position - m_FollowTform.position;         
            targetPos = m_FollowTform.position + (directionToTarget.normalized * _Distance);
        }

        //targetPos = m_FollowTform.position;

        // Draw a line to the target
        Debug.DrawLine(transform.position, targetPos);


        //if ((transform.position - targetPos).magnitude > .01f)
        {
            // Lerp towards the position
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * m_Smoothing);

            // if lead transform then update the rotation of the tip to ensure correct rotations
            if (_IsLeadTransform)
            {
                // direction to the follow tform is the tangent
                Vector3 tanget = (transform.position - m_FollowTform.position).normalized;
                Vector3 normal = Vector3.Cross(tanget, new Vector3(.5f, 1.1f, -.7f).normalized).normalized;
                Vector3 binormal = Vector3.Cross(tanget, normal).normalized;
                transform.rotation = Quaternion.LookRotation(tanget, normal);
            }
            else
            {
                // Update rotation using smooth rotation to stop gimble/axis flipping
                transform.rotation = TransformExtensions.SmoothRotation(transform.position, m_FollowTform.position, m_FollowTform.rotation);
            }
        }
    }
}
