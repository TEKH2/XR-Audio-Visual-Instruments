using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Events;

public class TrackedFloatUpdate : UnityEvent<float> { }
public class TrackedVector2Update : UnityEvent<Vector2> { }
public class TrackedVector3Update : UnityEvent<Vector3> { }
public class TrackedQuaternionUpdate : UnityEvent<Quaternion> { }

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
            if(value != _Value)
            {
                _Value = value;
                if (_Value)
                    OnDownEvent.Invoke();
                else
                    OnUpEvent.Invoke();

                if(XRControllers.Instance._DebugControllers)
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
    public float Value
    {
        set
        {
            if (value != _Value)
            {
                _Value = value;
                OnValueUpdate.Invoke(_Value);

                if (XRControllers.Instance._DebugControllers)
                    Debug.Log(_InputFeature.name.ToString() + "   " + _Value);
            }
        }

        get { return _Value; }
    }

    public TrackedFloatUpdate OnValueUpdate = new TrackedFloatUpdate();

    public TrackedInputFeatureFloat(InputFeatureUsage<float> inputFeature)
    {
        _InputFeature = inputFeature;
    }
}

public class TrackedInputFeatureVector2
{
    public InputFeatureUsage<Vector2> _InputFeature = CommonUsages.primary2DAxis;
    Vector2 _Value;
    public Vector2 Value
    {
        set
        {
            if (value != _Value)
            {
                _Value = value;
                OnValueUpdate.Invoke(_Value);

                if (XRControllers.Instance._DebugControllers)
                    Debug.Log(_InputFeature.name.ToString() + "   " + _Value);
            }
        }

        get { return _Value; }
    }

    public TrackedVector2Update OnValueUpdate = new TrackedVector2Update();

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

                if (XRControllers.Instance._DebugControllers)
                    Debug.Log(_InputFeature.name.ToString() + "   " + _Value);
            }
        }

        get { return _Value; }
    }

    public TrackedVector3Update OnValueUpdate = new TrackedVector3Update();

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

                if (XRControllers.Instance._DebugControllers)
                    Debug.Log(_InputFeature.name.ToString() + "   " + _Value);
            }
        }

        get { return _Value; }
    }

    public TrackedQuaternionUpdate OnValueUpdate = new TrackedQuaternionUpdate();

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

    public TrackingSpaceType _SpaceType;

    List<XRNodeState> _States = new List<XRNodeState>();
    public Transform _RightController;
    public Transform _LeftController;

    private List<UnityEngine.XR.InputDevice> _AllDevices;
    private List<UnityEngine.XR.InputDevice> _DevicesWithPrimaryButton;

    public XRControllerFeatureSet _RightControllerFeatures;
    public XRControllerFeatureSet _LeftControllerFeatures;

    public bool _DebugControllers = false;

    private void Awake()
    {
        Instance = this;

        _RightControllerFeatures = new XRControllerFeatureSet();
        _LeftControllerFeatures = new XRControllerFeatureSet();

        _AllDevices = new List<UnityEngine.XR.InputDevice>();
        _DevicesWithPrimaryButton = new List<UnityEngine.XR.InputDevice>();
       
        InputTracking.nodeAdded += UpdateInputDevices;

        XRDevice.SetTrackingSpaceType(_SpaceType);
    }

    private void Update()
    {
        InputTracking.GetNodeStates(_States);

        // UPDATE CONTROLLER TRANSFORMS
        for (int i = 0; i < _States.Count; i++)
        {
            if(_States[i].nodeType == XRNode.LeftHand)
            {
                UpdateControllerTransform(_LeftController, _States[i]);
            }
            else if (_States[i].nodeType == XRNode.RightHand)
            {
                UpdateControllerTransform(_RightController, _States[i]);
            }
        }

        // UPDATE CONTROLLER BUTTONS
        foreach (var device in _DevicesWithPrimaryButton)
        {
            // For each device
            if(device.characteristics == InputDeviceCharacteristics.Right)            
               _RightControllerFeatures.UpdateFeatureSet(device);   
            else if (device.characteristics == InputDeviceCharacteristics.Left)
                _LeftControllerFeatures.UpdateFeatureSet(device);
        }

        if (Input.GetKeyDown(KeyCode.R))
            InputTracking.Recenter();

       
    }

    // Updates the controller positions and rotations
    void UpdateControllerTransform(Transform t, XRNodeState state)
    {
        Vector3 pos = t.localPosition;
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
            }
        }
    }
}
