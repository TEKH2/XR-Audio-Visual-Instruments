using UnityEngine;
using System.Collections;

public class Input_MouseToWorld : MonoBehaviour
{
    public Camera m_Camera;
    public float m_DistanceFromCamera = 10;
    public float m_Smoothing = 8;

    Rigidbody m_RB;

    public bool _MouseLeft = false;
    public bool _MouseRight = false;

    void Start()
    {
        if (m_Camera == null)
            m_Camera = Camera.main;
    }

    void Update()
    {
        if (_MouseLeft && !Input.GetMouseButton(0))
            return;

        if (_MouseRight && !Input.GetMouseButton(1))
            return;

        Vector3 newPos = m_Camera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, m_DistanceFromCamera));

        if (m_RB != null)
        {
            Vector3 vecTo = transform.position - newPos;
            m_RB.AddForce(vecTo * m_Smoothing, ForceMode.Force);
        }
        else
        {
            if (m_Smoothing != 0)
                transform.position = Vector3.Lerp(transform.position, newPos, Time.deltaTime * m_Smoothing);
            else
                transform.position = newPos;
        }
    }
}

