using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class HybridController : MonoBehaviour
{
    public struct InteractionState
    {
        /// <summary>This field is true if it is is currently on.</summary>
        public bool active;
        /// <summary>This field is true if the interaction state was activated this frame.</summary>
        public bool activatedThisFrame;
        /// <summary>This field is true if the interaction state was de-activated this frame.</summary>
        public bool deActivatedThisFrame;

        public float _AxisValue;
    }

    [SerializeField]
    [Tooltip("Gets or sets the XRNode for this controller.")]
    XRNode _ControllerNode;
    /// <summary>Gets or sets the XRNode for this controller.</summary>
    public XRNode ControllerNode { get { return _ControllerNode; } set { _ControllerNode = value; } }

    /// <summary>Gets the InputDevice being used to read data from.</summary>
    public InputDevice InputDevice
    {
        get
        {
            return _InputDevice.isValid ? _InputDevice : (_InputDevice = InputDevices.GetDeviceAtXRNode(ControllerNode));
        }
    }
    private InputDevice _InputDevice;

    public InteractionState _GripInteraction;
    public InteractionState _TriggerInteraction;

    private void Start()
    {
        _GripInteraction = new InteractionState();
        _TriggerInteraction = new InteractionState();
    }

    private void Update()
    {
        _GripInteraction.activatedThisFrame = _GripInteraction.deActivatedThisFrame = false;
        _TriggerInteraction.activatedThisFrame = _TriggerInteraction.deActivatedThisFrame = false;

        HandleInteractionAction(_ControllerNode, InputHelpers.Button.Grip, ref _GripInteraction);
        HandleInteractionAction(_ControllerNode, InputHelpers.Button.Trigger, ref _TriggerInteraction);
    }

    void HandleInteractionAction(XRNode node, InputHelpers.Button button, ref InteractionState interactionState)
    {
        bool pressed = false;
        InputDevice.IsPressed(button, out pressed, .1f);

        InputHelpers.Button b = InputHelpers.Button.Grip;
        //InputDevice.TryGetFeatureValue()

        if (pressed)
        {
            if (!interactionState.active)
            {
                interactionState.activatedThisFrame = true;
                interactionState.active = true;
            }
        }
        else
        {
            if (interactionState.active)
            {
                interactionState.deActivatedThisFrame = true;
                interactionState.active = false;
            }
        }
    }
}
