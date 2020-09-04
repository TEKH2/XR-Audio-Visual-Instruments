using UnityEngine;
using System.Collections;

public class Input_MouseToWorld : MonoBehaviour
{
    public Camera m_Camera;
    public float m_DistanceFromCamera = 10;
    public float m_Smoothing = 8;

    Rigidbody m_RB;

    public int _MouseButton = 0;

    Vector3 _TargetPos;

    void Start()
    {
        if (m_Camera == null)
            m_Camera = Camera.main;
    }

    void Update()
    {
        if (!Input.GetMouseButton(_MouseButton))
            return;

        _TargetPos = m_Camera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, m_DistanceFromCamera));

        if (m_RB != null)
        {
            Vector3 vecTo = transform.position - _TargetPos;
            m_RB.AddForce(vecTo * m_Smoothing, ForceMode.Force);
        }
        else
        {
            if (m_Smoothing != 0)
                transform.position = Vector3.Lerp(transform.position, _TargetPos, Time.deltaTime * m_Smoothing);
            else
                transform.position = _TargetPos;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(_TargetPos, .03f);
    }
}

