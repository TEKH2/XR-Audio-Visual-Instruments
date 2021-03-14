using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Interaction;

[System.Serializable]
public class ParticleForce
{
    private GameObject _ForceObject;
    private OVRInput.Controller _Controller;
    private Transform _LeapRig;

    public ParticleForce(OVRInput.Controller controller, Transform parent, Transform leapRig)
    {
        _Controller = controller;
        _ForceObject = new GameObject();
        _ForceObject.transform.parent = parent;
        _LeapRig = leapRig;
        _ForceObject.transform.localPosition = Vector3.zero;

        if (controller == OVRInput.Controller.LTouch)
            _ForceObject.name = "Force Left";
        else if (controller == OVRInput.Controller.RTouch)
            _ForceObject.name = "Force Right";
    }


    public void Update(ActiveParticle[] particles, float amount)
    {
        if (_ForceObject != null)
        {

            _ForceObject.transform.position = OVRInput.GetLocalControllerPosition(_Controller) + _LeapRig.transform.position;

            foreach (ActiveParticle particle in particles)
            {
                float force = (1 - Mathf.Clamp((_ForceObject.transform.position - particle.GetPosition()).sqrMagnitude, 0, 1)) * 20;
                particle.ApplyForce(_ForceObject.transform, force * amount);
            }
        }
    }

    public void Delete()
    {
        if (_ForceObject != null)
            Object.Destroy(_ForceObject);
    }
}
