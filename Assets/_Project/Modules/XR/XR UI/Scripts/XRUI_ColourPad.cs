using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using EXP.XR;

namespace EXP.XR
{
    public class XRUI_ColourPad : InteractableBase
    {
        #region VARIABLES

        [Header("Colour Pad")]
        public Color _Col;
        public Vector3 _HandleNorm;
        public Color Value
        {
            get
            {
                return Color.HSVToRGB(_HandleNorm.x, _HandleNorm.y, _HandleNorm.z);
            }
            set
            {
                Color.RGBToHSV(value, out _HandleNorm.x, out _HandleNorm.y, out _HandleNorm.z);
            }
        }

        public MeshRenderer _ColourSwatchMesh;
        public MeshRenderer _HSVMesh;
        public Transform _HSVParentCube;
        Vector2 _HandleRange = new Vector2(-.5f, .5f);
        public float _HandleSize = .1f;

        public ColourEvent _PadEvent;
      
        Vector3 _HandleLocalPos;
        XRUI_Pointer _Pointer;

        #endregion

        #region UNITY METHODS

        private void Start()
        {
            _HandleLocalPos = _HoverHighlightMesh.transform.localPosition;

            _HandleRange = new Vector2(-.5f + (_HandleSize / 2), .5f + (_HandleSize / 2));

            Value = _Col;
            SetHandleFromNorm();
            _Pointer = FindObjectOfType<XRUI_Pointer>();
        }

        protected override void Update()
        {
            base.Update();

            if (_State == InteractableState.Interacting)
            {
                Vector3 localPosOfPointer = transform.InverseTransformPoint(_Pointer.transform.position);
                SetHandleFromLocalX(localPosOfPointer, true);
            }
        }

        #endregion

        void SetHandleFromNorm()
        {
            _HandleLocalPos.x = Mathf.Lerp(_HandleRange.x, _HandleRange.y, _HandleNorm.x);
            _HandleLocalPos.y = Mathf.Lerp(_HandleRange.x, _HandleRange.y, _HandleNorm.y);
            _HandleLocalPos.z = Mathf.Lerp(_HandleRange.x, _HandleRange.y, _HandleNorm.z);
            _HoverHighlightMesh.transform.localPosition = _HandleLocalPos;
        }

        void SetHandleFromLocalX(Vector3 localPos, bool sendEvent = true)
        {           
            _HandleLocalPos.x = Mathf.Clamp(localPos.x, _HandleRange.x, _HandleRange.y);
            _HandleLocalPos.y = Mathf.Clamp(localPos.y, _HandleRange.x, _HandleRange.y);
            _HandleLocalPos.z = Mathf.Clamp(localPos.z, _HandleRange.x, _HandleRange.y);
                        
            _HandleNorm.x = Mathf.InverseLerp(_HandleRange.x, _HandleRange.y, _HandleLocalPos.x);
            _HandleNorm.y = Mathf.InverseLerp(_HandleRange.x, _HandleRange.y, _HandleLocalPos.y);
            _HandleNorm.z = Mathf.InverseLerp(_HandleRange.y, _HandleRange.x, _HandleLocalPos.z);

            //print(localPos + "   " + _HandleLocalPos + "   " + _HandleNorm);
            _HoverHighlightMesh.transform.localPosition = _HandleLocalPos;

            // Set the cube scale
            float zScale = 1 - (_HandleLocalPos.z + .5f);
            _HSVParentCube.localScale = new Vector3(1, 1, zScale);           

            // Set colour of the swatrch mat
            _ColourSwatchMesh.material.SetColor("_BaseColor", Value);

            // Set brightness of the HSV picker shader
            _HSVMesh.material.SetFloat("_Brightness", _HandleNorm.z);

            if (sendEvent && _PadEvent != null)
                _PadEvent.Invoke(Value);
        }
    }
}
