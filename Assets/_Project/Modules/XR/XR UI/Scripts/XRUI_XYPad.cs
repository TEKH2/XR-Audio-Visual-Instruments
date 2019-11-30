using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using EXP.XR;

namespace EXP.XR
{
    public class XRUI_XYPad : InteractableBase
    {
        #region VARIABLES

        [Header("XY Pad")]
        public Vector2 _HandleNorm;
        public Vector2 _ValueOutputRange = new Vector2(0, 1);
        public Vector2 Value
        {
            get
            {
                return new Vector2( Mathf.Lerp(_ValueOutputRange.x, _ValueOutputRange.y, _HandleNorm.x),
                                    Mathf.Lerp(_ValueOutputRange.x, _ValueOutputRange.y, _HandleNorm.y));
            }
            set
            {
                _HandleNorm.x = Mathf.InverseLerp(_ValueOutputRange.x, _ValueOutputRange.y, value.x);
                _HandleNorm.y = Mathf.InverseLerp(_ValueOutputRange.x, _ValueOutputRange.y, value.y);
            }
        }

        public Vector2 _HandleRange = new Vector2(-1.4f, 1.4f);
        public Vector2Event _PadEvent;

        Vector3 _HandleLocalPos;
        XRUI_Pointer _Pointer;

        #endregion

        #region UNITY METHODS

        private void Start()
        {
            _HandleLocalPos = _HoverHighlightMesh.transform.localPosition;
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
            _HoverHighlightMesh.transform.localPosition = _HandleLocalPos;
        }

        void SetHandleFromLocalX(Vector3 localPos, bool sendEvent = true)
        {
            _HandleLocalPos.x = Mathf.Clamp(localPos.x, _HandleRange.x, _HandleRange.y);
            _HandleLocalPos.y = Mathf.Clamp(localPos.y, _HandleRange.x, _HandleRange.y);
                       
            _HandleNorm.x = Mathf.InverseLerp(_HandleRange.x, _HandleRange.y, _HandleLocalPos.x);
            _HandleNorm.y = Mathf.InverseLerp(_HandleRange.x, _HandleRange.y, _HandleLocalPos.y);

            _HoverHighlightMesh.transform.localPosition = _HandleLocalPos;

            if (sendEvent && _PadEvent != null)
                _PadEvent.Invoke(Value);
        }
    }
}
