using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EXP.Painter
{
    public class BrushController_Mouse : MonoBehaviour
    {
        public Camera m_Cam;        
        public Brush _Brush;
        Transform _BrushTip;
        public bool m_UpdateFacing = false;
        float m_Depth = 1.5f;
        public float m_Smoothing = 2;

        void Start()
        {
            _BrushTip = transform;

            // Set Brush tip
            _Brush.SetBrushTip(_BrushTip);
        }

        // Update is called once per frame
        void Update()
        {          
           // m_Brush.m_InputOverUI = EventSystem.current.IsPointerOverGameObject();

            UpdateBrushTipTransformFromMouse();

            if (Input.GetMouseButtonDown(0))
            {
                _Brush.BeginStroke(transform);
            }
            else if (Input.GetMouseButton(0) && _Brush.Painting)
            {
                _Brush.UpdateStroke();
            }
            else if (Input.GetMouseButtonUp(0) && _Brush.Painting)
            {
                _Brush.EndStroke();
            }


            // Move canvas
            if (Input.GetMouseButtonDown(1))
            {
                m_UpdateFacing = false;
                PainterManager.Instance.ActiveCanvas.BeginMoveCanvas(_BrushTip);
            }
            else if(Input.GetMouseButton(1))
            {
                

            }
            else if (Input.GetMouseButtonUp(1))
            {
                m_UpdateFacing = true;
                PainterManager.Instance.ActiveCanvas.EndCanvasMove();
            }

            if (Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0 )
            {
                PainterManager.Instance.ActiveCanvas.Scale(Input.GetAxis("Mouse ScrollWheel"));
            }
        }

        void UpdateBrushTipTransformFromMouse()
        {
            Vector3 targetPos = m_Cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, m_Depth));

            if (m_Smoothing != 0)
                targetPos = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * m_Smoothing);

            // Update the rotation of the brush tip
            if (m_UpdateFacing && Vector3.Distance(targetPos, _BrushTip.position) > .01f)
            {
                UpdateTipAngleOnXY(_BrushTip.transform.position, targetPos);
            }

            transform.position = targetPos;
        }
        
        void UpdateTipAngleOnXY(Vector3 currentPos, Vector3 targetPos)
        {
            var newRotation = Quaternion.LookRotation(currentPos - targetPos, Vector3.forward);
            newRotation.x = 0.0f;
            newRotation.y = 0.0f;
            _BrushTip.rotation = Quaternion.Slerp(_BrushTip.transform.rotation, newRotation, 1);
        }
    }
}
