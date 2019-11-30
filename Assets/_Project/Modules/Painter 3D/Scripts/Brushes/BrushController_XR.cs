using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using EXP.XR;

namespace EXP.Painter
{
    // A script that takes a brushtip transform and passes it into the brush
    // Enables the user to change brush properties and move the canvas using XR controllers
    public class BrushController_XR : MonoBehaviour
    {
        public Brush _Brush;
        public Transform _BrushTip;
        public Transform _BrushtipFollowTransform;      
        public float m_Smoothing = 2;
        public bool m_UpdateFacing = false;

        public Vector2 _BrushSizeRange = new Vector2(.02f, .15f);
        public XRUI_ColourPad _ColourPicker;

        XRUI_Pointer _UIPointer;

        XRUI_Pointer _XRPointer;
                
        private void Start()
        {
            // Trigger draws stroke
            XRControllers.Instance._RightControllerFeatures._XRFloatDict[XRFloats.Trigger].OnValueUpdate.AddListener((float f) => UdpateStroke(f));

            // Left grip moves canvas
            XRControllers.Instance._LeftControllerFeatures._XRBoolDict[XRBools.GripButton].OnDownEvent.AddListener(() => MoveCanvasBegin());
            XRControllers.Instance._LeftControllerFeatures._XRBoolDict[XRBools.GripButton].OnUpEvent.AddListener(() => MoveCanvasEnd());

            // Left thumb LR changes brush
            XRControllers.Instance._LeftControllerFeatures._XRVector2Dict[XRVector2s.PrimaryAxis].OnXNegOne.AddListener(() => BrushPresetSelector.Instance.Prev());
            XRControllers.Instance._LeftControllerFeatures._XRVector2Dict[XRVector2s.PrimaryAxis].OnXOne.AddListener(() => BrushPresetSelector.Instance.Prev());

            // Right thumb LR chanegs size
            XRControllers.Instance._RightControllerFeatures._XRVector2Dict[XRVector2s.PrimaryAxis].OnValueUpdate.AddListener((Vector2 v) => IncrementBrushSize(v.x));

            // Right primary button is undo
            XRControllers.Instance._RightControllerFeatures._XRBoolDict[XRBools.PrimaryButton].OnDownEvent.AddListener(() => PainterManager.Instance.UndoLastStroke());

            // Right secondary button is col
            _ColourPicker._PadEvent.AddListener((Color c) => _Brush.SetCol(c));

            _XRPointer = FindObjectOfType<XRUI_Pointer>();

            // Set Brush tip
            _Brush.SetBrushTip(_BrushTip);
        }

        void MoveCanvasBegin()
        {
            PainterManager.Instance.ActiveCanvas.BeginMoveCanvas(XRControllers.Instance._LeftController);
        }

        void MoveCanvasEnd()
        {
            PainterManager.Instance.ActiveCanvas.EndCanvasMove();
        }

        void IncrementBrushSize(float amount)
        {
            if (Mathf.Abs(amount) > 0.2f)
            {
                print(_Brush.BrushSize);
                _Brush.BrushSize += amount * Time.deltaTime * .2f;
            }
        }

        void UdpateStroke(float trigger)
        {
            print(trigger);

            // if the pointer is interacting then return
            if (_XRPointer.State != InteractableState.Normal)            
                return;
                       
            if (!_Brush.Painting && trigger > 0)
            {
                _Brush.BeginStroke(_BrushTip);
            }
            else if(_Brush.Painting && trigger == 0)
            {
                _Brush.EndStroke();
            }
        }

        // Update is called once per frame
        void Update()
        {
          

            Vector3 targetPos = _BrushtipFollowTransform.position;

            // Smooth toward targetpos of smoothing is larger than 0
            if (m_Smoothing != 0)
                targetPos = Vector3.Lerp(_BrushTip.position, targetPos, Time.deltaTime * m_Smoothing);

            // Update the rotation of the brush tip
            if (m_UpdateFacing && Vector3.Distance(targetPos, _BrushTip.position) > .01f)
            {
                var newRotation = Quaternion.LookRotation(_BrushTip.position - targetPos, Vector3.forward);
                newRotation.x = 0.0f;
                newRotation.y = 0.0f;
                _BrushTip.rotation = Quaternion.Slerp(_BrushTip.rotation, newRotation, 1);
            }

            // Set brushtime position
            _BrushTip.position = targetPos;

            if (_Brush.Painting)
            {
                _Brush.UpdateStroke();
            }

            // Turn on teh brush tip if the UI pointer isn't interacting
            _BrushTip.GetComponent<MeshRenderer>().enabled = _XRPointer.State == InteractableState.Normal;   
        }
    }
}
