using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Events;

namespace EXP.XR
{
    /// XR Controllers is a generalized class that provides events for all bool, float, Vector2
    /// and Vector 3 propeties of all XR controller inputs. Should work across all platforms

    #region XR Enums

    public enum XRControllerType
    {
        HMD,
        RightController,
        LeftController,
        Eyes,
    }

    public enum XRBools
    {
        IsTracked,
        PrimaryButton,
        PrimaryTouch,
        SecondaryButton,
        SecondaryTouch,
        GripButton,
        TriggerButton,
        MenuButton,
        Primary2DAxisClick,
        Primary2DAxisTouch,
        Thumbrest
    }

    public enum XRFloats
    {
        BatteryLevel,
        Trigger,
    }

    public enum XRVector2s
    {
        PrimaryAxis,
        SecondaryAxis,
    }

    public enum XRVector3s
    {
        Velocity,
        Acceleration,
        AngularVelocity,
        AngularAcceleration,
        DevicePosition,
    }

    public enum XRQuaternions
    {
        DeviceRotation,
    }

    #endregion

    #region TRACKED INPUT FEATURES

    public class TrackedInputFeatureBool
    {
        public InputFeatureUsage<bool> _InputFeature = CommonUsages.triggerButton;
        bool _Value = false;
        public bool Value
        {
            set
            {
                if (value != _Value)
                {
                    _Value = value;
                    if (_Value)
                        OnDownEvent.Invoke();
                    else
                        OnUpEvent.Invoke();

                    if (XRControllers.Instance._DebugControllerFeatureValues)
                        Debug.Log(_InputFeature.name.ToString() + "   " + _Value);
                }
            }

            get { return _Value; }
        }

        public UnityEvent OnDownEvent = new UnityEvent();
        public UnityEvent OnUpEvent = new UnityEvent();

        public TrackedInputFeatureBool(InputFeatureUsage<bool> inputFeature, bool initState = false)
        {
            _InputFeature = inputFeature;
            _Value = initState;
        }
    }

    public class TrackedInputFeatureFloat
    {
        public InputFeatureUsage<float> _InputFeature = CommonUsages.trigger;
        float _Value = 0;

        float _HighCutoff = .99f;
        float _Lowcutoff = .01f;

        public float Value
        {
            set
            {
                if (value != _Value)
                {
                    if (value >= _HighCutoff && _Value < _HighCutoff)
                    {
                        OnValueOne.Invoke();
                        if (XRControllers.Instance._DebugControllerEvents)
                            Debug.Log(_InputFeature.name.ToString() + "  OnValueOne");
                    }
                    else if (value < _HighCutoff && _Value >= _HighCutoff)
                    {
                        OnValueExitOne.Invoke();
                        if (XRControllers.Instance._DebugControllerEvents)
                            Debug.Log(_InputFeature.name.ToString() + "  OnValue ExitOne");
                    }

                    if (value <= _Lowcutoff && _Value > _Lowcutoff)
                    {
                        OnValueZero.Invoke();
                        if (XRControllers.Instance._DebugControllerEvents)
                            Debug.Log(_InputFeature.name.ToString() + "  OnValue Zero");
                    }

                    _Value = value;
                    OnValueUpdate.Invoke(_Value);

                    if (XRControllers.Instance._DebugControllerFeatureValues)
                        Debug.Log(_InputFeature.name.ToString() + "   " + _Value);
                }
            }

            get { return _Value; }
        }

        public FloatEvent OnValueUpdate = new FloatEvent();
        public UnityEvent OnValueOne = new UnityEvent();
        public UnityEvent OnValueExitOne = new UnityEvent();
        public UnityEvent OnValueZero = new UnityEvent();

        public TrackedInputFeatureFloat(InputFeatureUsage<float> inputFeature)
        {
            _InputFeature = inputFeature;
        }
    }

   
    public class TrackedInputFeatureVector2
    {
        float _AxisCutoffTrigger = .96f;
        public InputFeatureUsage<Vector2> _InputFeature = CommonUsages.primary2DAxis;
        Vector2 _Value;
        public Vector2 Value
        {
            set
            {
                if (value != _Value)
                {
                    if (_Value.x < _AxisCutoffTrigger && value.x > _AxisCutoffTrigger)
                        OnXOne.Invoke();

                    if (_Value.x > -_AxisCutoffTrigger && value.x < -_AxisCutoffTrigger)
                        OnXNegOne.Invoke();

                    if (_Value.y < _AxisCutoffTrigger && value.y > _AxisCutoffTrigger)
                        OnYOne.Invoke();

                    if (_Value.y > -_AxisCutoffTrigger && value.y < -_AxisCutoffTrigger)
                        OnYNegOne.Invoke();

                    _Value = value;

                    OnValueUpdate.Invoke(_Value);                   

                    if (XRControllers.Instance._DebugControllerFeatureValues)
                        Debug.Log(_InputFeature.name.ToString() + "   " + _Value);
                }
            }

            get { return _Value; }
        }

        public UnityEvent OnXOne = new UnityEvent();
        public UnityEvent OnXNegOne = new UnityEvent();

        public UnityEvent OnYOne = new UnityEvent();
        public UnityEvent OnYNegOne = new UnityEvent();
        public Vector2Event OnValueUpdate = new Vector2Event();

        public TrackedInputFeatureVector2(InputFeatureUsage<Vector2> inputFeature)
        {
            _InputFeature = inputFeature;
        }
    }

    public class TrackedInputFeatureVector3
    {
        public InputFeatureUsage<Vector3> _InputFeature = CommonUsages.deviceAngularVelocity;
        Vector3 _Value;
        public Vector3 Value
        {
            set
            {
                if (value != _Value)
                {
                    _Value = value;
                    OnValueUpdate.Invoke(_Value);

                    if (XRControllers.Instance._DebugControllerFeatureValues)
                        Debug.Log(_InputFeature.name.ToString() + "   " + _Value);
                }
            }

            get { return _Value; }
        }

        public Vector3Event OnValueUpdate = new Vector3Event();

        public TrackedInputFeatureVector3(InputFeatureUsage<Vector3> inputFeature)
        {
            _InputFeature = inputFeature;
        }
    }

    public class TrackedInputFeatureQuaternion
    {
        public InputFeatureUsage<Quaternion> _InputFeature = CommonUsages.deviceRotation;
        Quaternion _Value;
        public Quaternion Value
        {
            set
            {
                if (value != _Value)
                {
                    _Value = value;
                    OnValueUpdate.Invoke(_Value);

                    if (XRControllers.Instance._DebugControllerFeatureValues)
                        Debug.Log(_InputFeature.name.ToString() + "   " + _Value);
                }
            }

            get { return _Value; }
        }

        public QuaternionEvent OnValueUpdate = new QuaternionEvent();

        public TrackedInputFeatureQuaternion(InputFeatureUsage<Quaternion> inputFeature)
        {
            _InputFeature = inputFeature;
        }
    }
    #endregion

    public class XRControllerFeatureSet
    {
        // Dictionaries to hold all the tracked inputs against an enum
        public Dictionary<XRBools, TrackedInputFeatureBool> _XRBoolDict = new Dictionary<XRBools, TrackedInputFeatureBool>();
        public Dictionary<XRFloats, TrackedInputFeatureFloat> _XRFloatDict = new Dictionary<XRFloats, TrackedInputFeatureFloat>();
        public Dictionary<XRVector2s, TrackedInputFeatureVector2> _XRVector2Dict = new Dictionary<XRVector2s, TrackedInputFeatureVector2>();
        public Dictionary<XRVector3s, TrackedInputFeatureVector3> _XRVector3Dict = new Dictionary<XRVector3s, TrackedInputFeatureVector3>();
        public Dictionary<XRQuaternions, TrackedInputFeatureQuaternion> _XRQuaternionDict = new Dictionary<XRQuaternions, TrackedInputFeatureQuaternion>();

        public XRControllerFeatureSet()
        {
            //BOOLS
            _XRBoolDict.Add(XRBools.IsTracked, new TrackedInputFeatureBool(CommonUsages.isTracked));
            _XRBoolDict.Add(XRBools.PrimaryButton, new TrackedInputFeatureBool(CommonUsages.primaryButton));
            _XRBoolDict.Add(XRBools.PrimaryTouch, new TrackedInputFeatureBool(CommonUsages.primaryTouch));
            _XRBoolDict.Add(XRBools.SecondaryButton, new TrackedInputFeatureBool(CommonUsages.secondaryButton));
            _XRBoolDict.Add(XRBools.SecondaryTouch, new TrackedInputFeatureBool(CommonUsages.secondaryTouch));
            _XRBoolDict.Add(XRBools.GripButton, new TrackedInputFeatureBool(CommonUsages.gripButton));
            _XRBoolDict.Add(XRBools.TriggerButton, new TrackedInputFeatureBool(CommonUsages.triggerButton));
            _XRBoolDict.Add(XRBools.MenuButton, new TrackedInputFeatureBool(CommonUsages.menuButton));
            _XRBoolDict.Add(XRBools.Primary2DAxisClick, new TrackedInputFeatureBool(CommonUsages.primary2DAxisClick));
            _XRBoolDict.Add(XRBools.Primary2DAxisTouch, new TrackedInputFeatureBool(CommonUsages.primary2DAxisTouch));
            _XRBoolDict.Add(XRBools.Thumbrest, new TrackedInputFeatureBool(CommonUsages.thumbrest));
            //FLOATS
            _XRFloatDict.Add(XRFloats.BatteryLevel, new TrackedInputFeatureFloat(CommonUsages.batteryLevel));
            _XRFloatDict.Add(XRFloats.Trigger, new TrackedInputFeatureFloat(CommonUsages.trigger));
            // VECTOR 2
            _XRVector2Dict.Add(XRVector2s.PrimaryAxis, new TrackedInputFeatureVector2(CommonUsages.primary2DAxis));
            _XRVector2Dict.Add(XRVector2s.SecondaryAxis, new TrackedInputFeatureVector2(CommonUsages.secondary2DAxis));
            //VECTOR 3
            _XRVector3Dict.Add(XRVector3s.Velocity, new TrackedInputFeatureVector3(CommonUsages.deviceVelocity));
            _XRVector3Dict.Add(XRVector3s.Acceleration, new TrackedInputFeatureVector3(CommonUsages.deviceAcceleration));
            _XRVector3Dict.Add(XRVector3s.AngularVelocity, new TrackedInputFeatureVector3(CommonUsages.deviceAngularVelocity));
            _XRVector3Dict.Add(XRVector3s.AngularAcceleration, new TrackedInputFeatureVector3(CommonUsages.deviceAngularAcceleration));
            _XRVector3Dict.Add(XRVector3s.DevicePosition, new TrackedInputFeatureVector3(CommonUsages.devicePosition));
            //QUATERNIONS
            _XRQuaternionDict.Add(XRQuaternions.DeviceRotation, new TrackedInputFeatureQuaternion(CommonUsages.deviceRotation));
        }

        public void UpdateFeatureSet(InputDevice device)
        {
            // Update all bool features
            foreach (var trackedFeature in _XRBoolDict)
                UpdateBoolInputFeature(device, trackedFeature.Value);

            // Update all float features
            foreach (var trackedFeature in _XRFloatDict)
                UpdateFloatInputFeature(device, trackedFeature.Value);

            // Update all vector 2 features
            foreach (var trackedFeature in _XRVector2Dict)
                UpdateVector2InputFeature(device, trackedFeature.Value);

            // Update all vector 3 features
            foreach (var trackedFeature in _XRVector3Dict)
                UpdateVector3InputFeature(device, trackedFeature.Value);

            // Update all Quaternion features
            foreach (var trackedFeature in _XRQuaternionDict)
                UpdateQuaternionInputFeature(device, trackedFeature.Value);
        }

        #region FEATURE UPDATES

        void UpdateBoolInputFeature(InputDevice inputDevice, TrackedInputFeatureBool inputFeature)
        {
            bool newState = inputFeature.Value;

            if (inputDevice.TryGetFeatureValue(inputFeature._InputFeature, out newState))
                inputFeature.Value = newState;
        }

        void UpdateFloatInputFeature(InputDevice inputDevice, TrackedInputFeatureFloat inputFeature)
        {
            float newVal = inputFeature.Value;

            if (inputDevice.TryGetFeatureValue(inputFeature._InputFeature, out newVal))
                inputFeature.Value = newVal;
        }

        void UpdateVector2InputFeature(InputDevice inputDevice, TrackedInputFeatureVector2 inputFeature)
        {
            Vector2 newVal = inputFeature.Value;

            if (inputDevice.TryGetFeatureValue(inputFeature._InputFeature, out newVal))
                inputFeature.Value = newVal;
        }

        void UpdateVector3InputFeature(InputDevice inputDevice, TrackedInputFeatureVector3 inputFeature)
        {
            Vector3 newVal = inputFeature.Value;

            if (inputDevice.TryGetFeatureValue(inputFeature._InputFeature, out newVal))
                inputFeature.Value = newVal;
        }

        void UpdateQuaternionInputFeature(InputDevice inputDevice, TrackedInputFeatureQuaternion inputFeature)
        {
            Quaternion newVal = inputFeature.Value;

            if (inputDevice.TryGetFeatureValue(inputFeature._InputFeature, out newVal))
                inputFeature.Value = newVal;
        }

        #endregion
    }

    public class XRControllers : MonoBehaviour
    {
        public static XRControllers Instance;

        List<XRNodeState> _States = new List<XRNodeState>();

        // Floor is normal room scale, Device means HMD is centered around the parent transform
        public TrackingOriginModeFlags _TrackingOriginMode = TrackingOriginModeFlags.Floor;

        List<Vector3> _BoundaryPoints = new List<Vector3>();

        XRInputSubsystem _InputSubsystem;

        public Handedness _Handedness = Handedness.Right;

        [Header("DEVICE TRANSFORMS")]
        public Transform _HMD;
        public Transform _RightController;
        public Transform _LeftController;       

        private List<UnityEngine.XR.InputDevice> _AllDevices;
        private List<UnityEngine.XR.InputDevice> _DevicesWithPrimaryButton;

        // FEATURE SETS (Buttons, axis, thum sticks, fingers)
        public XRControllerFeatureSet _RightControllerFeatures;
        public XRControllerFeatureSet _LeftControllerFeatures;

        [Header("DEBUG")]
        public bool _DebugLogging = false;
        public bool _DebugControllerFeatureValues = false;
        public bool _DebugControllerEvents = false;
        public bool _DebugDrawBoundary = false;

        private void Awake()
        {
            Instance = this;

            // Get devices
            _AllDevices = new List<UnityEngine.XR.InputDevice>();
            _DevicesWithPrimaryButton = new List<UnityEngine.XR.InputDevice>();

            // Get feature sets for right and left controllers
            _RightControllerFeatures = new XRControllerFeatureSet();
            _LeftControllerFeatures = new XRControllerFeatureSet();

            // Get input subsystem
            List<XRInputSubsystem> inputSubSystems = new List<XRInputSubsystem>();
            SubsystemManager.GetInstances<XRInputSubsystem>(inputSubSystems);
            _InputSubsystem = inputSubSystems[0];          

            // Set tracking origin mode    ref: https://docs.unity3d.com/ScriptReference/XR.TrackingOriginModeFlags.html
            _InputSubsystem.TrySetTrackingOriginMode(_TrackingOriginMode);
            _InputSubsystem.TryGetBoundaryPoints(_BoundaryPoints);

            InputTracking.nodeAdded += UpdateInputDevices;
        }

        private void Update()
        {
            // GET STATES
            InputTracking.GetNodeStates(_States);

            // UPDATE CONTROLLER TRANSFORMS
            for (int i = 0; i < _States.Count; i++)
            {
                if(_DebugLogging)
                    Debug.Log(i + "    " + _States[i].nodeType.ToString());

                if (_States[i].nodeType == XRNode.LeftHand && _LeftController != null)
                {
                    UpdateControllerTransform(_LeftController, _States[i]);
                }
                else if (_States[i].nodeType == XRNode.RightHand && _RightController != null)
                {
                    UpdateControllerTransform(_RightController, _States[i]);
                }
                else if (_States[i].nodeType == XRNode.CenterEye && _HMD != null)
                {
                    UpdateControllerTransform(_HMD, _States[i]);
                }
            }

            // UPDATE CONTROLLER BUTTONS
            foreach (var device in _DevicesWithPrimaryButton)
            {
                // For each device
                if (device.role == InputDeviceRole.RightHanded)
                    _RightControllerFeatures.UpdateFeatureSet(device);
                else if (device.role == InputDeviceRole.LeftHanded)
                    _LeftControllerFeatures.UpdateFeatureSet(device);
            }
        }

        // Updates the controller positions and rotations
        void UpdateControllerTransform(Transform t, XRNodeState state)
        {
            if (_DebugLogging)
                Debug.Log("Updating controller transform: " + t.name + "    node: " + state.nodeType.ToString());

            Vector3 pos = t.position;
            Quaternion rot = t.localRotation;

            if (state.TryGetPosition(out pos))
                t.localPosition = pos;

            if (state.TryGetRotation(out rot))
                t.localRotation = rot;
        }

        // find any devices supporting the desired feature usage
        void UpdateInputDevices(XRNodeState obj)
        {
            _DevicesWithPrimaryButton.Clear();
            UnityEngine.XR.InputDevices.GetDevices(_AllDevices);
            bool discardedValue;
            foreach (var device in _AllDevices)
            {
                if (device.TryGetFeatureValue(CommonUsages.primaryButton, out discardedValue))
                {
                    _DevicesWithPrimaryButton.Add(device); // Add any devices that have a primary button.
                    if (_DebugLogging)
                        Debug.Log("Adding device: " + device.name);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (_DebugDrawBoundary)
            {
                foreach (Vector3 point in _BoundaryPoints)
                {
                    Gizmos.DrawSphere(point, .1f);
                }
            }
        }
    }
}
