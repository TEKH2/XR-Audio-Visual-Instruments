using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Interaction;

[System.Serializable]
public class HandForce
{
    private GameObject _ForceObject;
    private OVRInput.Controller _Controller;
    private Transform _Parent;
    private Transform _LeapRig;

    public HandForce(OVRInput.Controller controller, Transform parent, Transform leapRig)
    {
        _Controller = controller;
        _ForceObject = new GameObject();
        _ForceObject.transform.parent = parent;
        _Parent = parent;
        _LeapRig = leapRig;
        _ForceObject.transform.localPosition = Vector3.zero;

        if (controller == OVRInput.Controller.LTouch)
            _ForceObject.name = "Force Left";
        else if (controller == OVRInput.Controller.RTouch)
            _ForceObject.name = "Force Right";
    }


    public void Update(List <Slider> sliders, float amount)
    {
        if (_ForceObject != null)
        {

            _ForceObject.transform.position = OVRInput.GetLocalControllerPosition(_Controller) + _LeapRig.transform.position;

            foreach (Slider slider in sliders)
            {
                //float force = Mathf.Clamp(0.5f - (_ForceObject.transform.position - slider.GetTransform().position).magnitude, 0, 1);
                float force = (1 - Mathf.Clamp((_ForceObject.transform.position - slider.GetTransform().position).sqrMagnitude, 0, 1)) * 20;
                slider.ApplyForce(_ForceObject.transform, force * amount);
            }
        }
    }

    public void Delete()
    {
        if (_ForceObject != null)
            Object.Destroy(_ForceObject);
    }
}
