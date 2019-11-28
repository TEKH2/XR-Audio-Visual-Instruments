using UnityEngine;
using System.Collections;

namespace EXPToolkit
{
    public class Input_MouseToWorld : MonoBehaviour
    {
        public Camera m_Camera;
        public float m_DistanceFromCamera = 10;
        public float m_Smoothing = 8;

        Rigidbody m_RB;

        void Start()
        {
            if (m_Camera == null)
                m_Camera = Camera.main;
        }

        void Update()
        {
            Vector3 newPos = m_Camera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, m_DistanceFromCamera));

            if (m_RB != null)
            {
                Vector3 vecTo = transform.position - newPos;
                m_RB.AddForce(vecTo * m_Smoothing, ForceMode.Force);
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, newPos, Time.deltaTime * m_Smoothing);
            }
        }
    }
}
