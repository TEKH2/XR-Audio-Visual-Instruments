using UnityEngine;
using Klak.Math;

/*
using EXP.XR;

namespace Klak.Wiring
{
    [AddComponentMenu("Klak/Wiring/Input/XR Button")]
    public class XRButton : NodeBase
    {
        #region Editable properties

        [SerializeField]
        XRControllerType _ControllerType = XRControllerType.RightController;

        [SerializeField]
        XRBools _ButtonType = XRBools.TriggerButton;

        [SerializeField]
        float _offValue = 0.0f;

        [SerializeField]
        float _onValue = 1.0f;

        [SerializeField]
        FloatInterpolator.Config _interpolator = null;

        #endregion

        #region Node I/O

        [SerializeField, Outlet]
        VoidEvent _buttonDownEvent = new VoidEvent();

        [SerializeField, Outlet]
        VoidEvent _buttonUpEvent = new VoidEvent();

        [SerializeField, Outlet]
        FloatEvent _FloatValueEvent = new FloatEvent();

        #endregion

        #region MonoBehaviour functions

        FloatInterpolator _floatValue;

        void Start()
        {
            _floatValue = new FloatInterpolator(0, _interpolator);
            TrackedInputFeatureBool trackedInput;

            if (_ControllerType == XRControllerType.RightController)
                trackedInput = XRControllers.Instance._RightControllerFeatures._XRBoolDict[_ButtonType];
            else
                trackedInput = XRControllers.Instance._LeftControllerFeatures._XRBoolDict[_ButtonType];

            trackedInput.OnDownEvent.AddListener(() => ButtonDownEvent());
            trackedInput.OnUpEvent.AddListener(() => ButtonUpEvent());
        }

        void ButtonDownEvent()
        {
            _buttonDownEvent.Invoke();
            _floatValue.targetValue = _onValue;
        }

        void ButtonUpEvent()
        {
            _buttonUpEvent.Invoke();
            _floatValue.targetValue = _offValue;
        }

        void Update()
        {
            _FloatValueEvent.Invoke(_floatValue.Step());
        }

        #endregion
    }
}
*/
