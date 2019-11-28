using UnityEngine;
using Klak.Math;

namespace Klak.Wiring
{
    [AddComponentMenu("Klak/Wiring/Input/XR Vector")]
    public class XRVector : NodeBase
    {
        #region Editable properties

        [SerializeField]
        XRControllerType _ControllerType = XRControllerType.RightController;

        [SerializeField]
        XRVector3s _VectorType = XRVector3s.Velocity;
               
        [SerializeField]
        FloatInterpolator.Config _MagnitudeInterpolator = null;

        #endregion

        #region Node I/O

        [SerializeField, Outlet]
        FloatEvent _MagnitudeEvent = new FloatEvent();

        [SerializeField, Outlet]
        Vector3Event _VectorEvent = new Vector3Event();

        #endregion

        #region MonoBehaviour functions

        FloatInterpolator _Magnitude;

        TrackedInputFeatureVector3 _TrackedInput;

        void Start()
        {
            _Magnitude = new FloatInterpolator(0, _MagnitudeInterpolator);

            if (_ControllerType == XRControllerType.RightController)
                _TrackedInput = XRControllers.Instance._RightControllerFeatures._XRVector3Dict[_VectorType];
            else
                _TrackedInput = XRControllers.Instance._LeftControllerFeatures._XRVector3Dict[_VectorType];
        }

        void Update()
        {
            _Magnitude.targetValue = _TrackedInput.Value.magnitude;
            _MagnitudeEvent.Invoke(_Magnitude.Step());
            _VectorEvent.Invoke(_TrackedInput.Value);
        }

        #endregion
    }
}
