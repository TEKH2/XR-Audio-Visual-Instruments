using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using EXP.XR;

namespace EXP.XR
{
    public class XRUI_Button : InteractableBase
    {
        [Header("Button")]
        public UnityEvent _ButtonEvent;

        protected override void SetInteractingState()
        {
            base.SetInteractingState();

            if (_ButtonEvent != null)
                _ButtonEvent.Invoke();
        }
    }
}
