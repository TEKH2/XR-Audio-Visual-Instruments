using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Leap.Unity.Interaction;
using System;

//[RequireComponent(typeof(InteractionBehaviour))]
public class ParameterController : MonoBehaviour
{
    // This GameObject's Variables
    public string _Name = "default";
    public int _NumberOfSliders;
    public bool _ShowAxis = true;
    public float _AxisThickness = 0.01f;
    public float _Smoothing = 100.0f;

    protected Vector3 _InteractionSize;

    /*
    protected Vector3 _NewLocalPosition;
    protected bool _NewHold = false;
    protected bool _Hover = false;
    protected bool _Held = false;
    protected bool _OutOfBounds = false;
    */

    // Child GameObjects
    public GameObject _SliderObject;
    public GameObject _InteractionBoundsObject;
    public GameObject _AxisParent;
    public Material _AxisMaterial;

    // Scripts
    protected InteractionBehaviour _IntObj;
    public OSC _OSC;


    void Start()
    {
        // Assign SliderObject's Interaction Behaviour to _IntObj
        _IntObj = _SliderObject.GetComponent<InteractionBehaviour>();
    }

    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("BANG");
    }


    protected static float LimitAxis(float limit, float value)
    {
        float newValue = value;
        
        if (value > limit)
            newValue = limit;
        else if (value < -limit)
            newValue = -limit;
            
        return newValue;
    }

    protected void CleanUpAxisObject(GameObject theObject, string axis)
    {
        if (theObject.GetComponent<Collider>().GetType() == typeof(BoxCollider))
        {
            BoxCollider tempCollider = theObject.GetComponent<BoxCollider>();
            Destroy(tempCollider);
        }
        else if (theObject.GetComponent<Collider>().GetType() == typeof(CapsuleCollider))
        {
            CapsuleCollider tempCollider = theObject.GetComponent<CapsuleCollider>();
            Destroy(tempCollider);
        }

        theObject.transform.parent = _AxisParent.transform;

        switch (axis)
        {
            case "x":
                theObject.transform.localScale =
                    new Vector3(_AxisThickness, _InteractionSize.y * 2, _AxisThickness);
                break;
            case "y":
                theObject.transform.localScale =
                    new Vector3(_InteractionSize.x * 2, _AxisThickness, _AxisThickness);
                break;
            case "z":
                theObject.transform.localScale =
                    new Vector3(_AxisThickness, _AxisThickness, _InteractionSize.z * 2);
                break;
            default:
                theObject.transform.localScale =
                    new Vector3(_AxisThickness, _AxisThickness, _AxisThickness);
                break;
        }
    }


    protected void SendOSC(string name, float[] output)
    {
        OscMessage message = new OscMessage();
        message.address = "/" + name;

        foreach (float value in output)
          message.values.Add(value);
        _OSC.Send(message);
    }

    protected void SendOSC(string name, float output)
    {
        OscMessage message = new OscMessage();
        message.address = "/" + name;
        message.values.Add(output);
        _OSC.Send(message);
    }

    public static float Map(float x, float in_min, float in_max, float out_min, float out_max, bool clamp = false)
    {
        if (clamp) x = Math.Max(in_min, Math.Min(x, in_max));
        return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
    }
}
