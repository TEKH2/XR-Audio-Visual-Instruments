using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using EXP.XR;

namespace EXP.XR
{
    public class XRUI_Toggle : InteractableBase
    {
        #region VARIABLES

        [Header("Toggle")]
        public bool _ToggledOn = false;
        public Vector3 _ToggleScalar = Vector3.one * .5f;

        public BoolEvent _ToggleEvent;

        #endregion

        private void Start()
        {
            SetToggleMeshScale();
        }

        public void SetToggleMeshScale()
        {
            Vector3 scale = _ToggledOn ? Vector3.one : _ToggleScalar;
            print(_OriginalScale.ScaleReturn(scale));
            _HoverHighlightMesh.transform.localScale = _OriginalScale.ScaleReturn(scale);
        }

        #region SET STATE FUNCTIONS
        protected override void SetNormalState()
        {
            _State = InteractableState.Normal;
            SetMeshColour(_NormalCol);
            SetToggleMeshScale();
        }

        protected override void SetHoverState()
        {
            _State = InteractableState.Hover;
            SetMeshColour(_HoverCol);
            SetToggleMeshScale();
        }

        protected override void SetInteractingState()
        {
            _ToggledOn = !_ToggledOn;
            SetToggleMeshScale();

            if (_ToggleEvent != null)
                _ToggleEvent.Invoke(_ToggledOn);

            print(name + " Toggled: " + _ToggledOn);

            _State = InteractableState.Interacting;
            SetMeshColour(_InteractingCol);
        }
        #endregion
    }
}
