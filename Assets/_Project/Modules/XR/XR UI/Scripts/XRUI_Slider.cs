using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using EXP.XR;

namespace EXP.XR
{
    public class XRUI_Slider : InteractableBase
    {
        #region VARIABLES

        [Header("Slider")]
        public float _SliderNorm = 0;
        public Vector2 _ValueOutputRange = new Vector2(0, 1);
        public float Value
        {
            get
            {
                return Mathf.Lerp(_ValueOutputRange.x, _ValueOutputRange.y, _SliderNorm);
            }
            set
            {
                _SliderNorm = Mathf.InverseLerp(_ValueOutputRange.x, _ValueOutputRange.y, value);
            }
        }

        public Vector2 _HandleRange = new Vector2(-1.4f, 1.4f);
        public FloatEvent _SliderEvent;

        Vector3 _HandlePos;
        XRUI_Pointer _Pointer;

        #endregion

        #region UNITY METHODS

        private void Start()
        {
            _HandlePos = _HoverHighlightMesh.transform.localPosition;
            SetHandleFromNorm();
            _Pointer = FindObjectOfType<XRUI_Pointer>();
        }

        protected override void Update()
        {
            base.Update();

            if (_State == InteractableState.Interacting)
            {
                Vector3 localPosOfPointer = transform.InverseTransformPoint(_Pointer.transform.position);
                SetHandleFromLocalX(localPosOfPointer.x, true);
            }
        }

        #endregion

        void SetHandleFromNorm()
        {
            _HandlePos.x = Mathf.Lerp(_HandleRange.x, _HandleRange.y, _SliderNorm);
            _HoverHighlightMesh.transform.localPosition = _HandlePos;
        }

        void SetHandleFromLocalX(float localX, bool sendEvent = true)
        {
            _HandlePos.x = Mathf.Clamp(localX, _HandleRange.x, _HandleRange.y);
            _SliderNorm = Mathf.InverseLerp(_HandleRange.x, _HandleRange.y, localX);

            _HoverHighlightMesh.transform.localPosition = _HandlePos;

            if (sendEvent && _SliderEvent != null)
                _SliderEvent.Invoke(Value);
        }
    }
}
