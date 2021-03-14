using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceObject
{
    private GameObject _ForceObject;
    private ParticleSystemForceField _ForceField;
    private GameObject _ForcePrefab;
    private Transform _LeapRigTransform;
    private AnimationCurve _GravityCurve;
    private ParticleSystem.MinMaxCurve _MinMaxCurve;
    private bool _SpinClockwise = false;


    public ForceObject(GameObject forcePrefab, string name, Transform parentTransform, Transform leapRig)
    {
        _ForceObject = Object.Instantiate(forcePrefab, Vector3.zero, Quaternion.identity);
        _ForceObject.name = name;
        _ForceObject.transform.parent = parentTransform.transform;
        _ForceObject.transform.localPosition = Vector3.zero;
        _LeapRigTransform = leapRig;

        _GravityCurve = new AnimationCurve();
        _GravityCurve.AddKey(0.0f, 1.0f);
        _GravityCurve.AddKey(1.0f, 0.0f);

        _ForceField = _ForceObject.GetComponent<ParticleSystemForceField>();
    }

    public void Update(float grip, Vector3 handPosition, Quaternion rotation, Vector3 rotationVelocity, Vector2 thumb)
    {
        if (grip > 0)
        {
            _ForceObject.SetActive(true);
            _ForceObject.transform.transform.position = handPosition + _LeapRigTransform.position;

            _ForceField.gravity = new ParticleSystem.MinMaxCurve(grip / 5, _GravityCurve);
            _ForceField.transform.rotation = rotation;
            _ForceField.directionY = thumb[1] * 100;

            if (thumb[0] < 0)
            {
                _SpinClockwise = true;
                _ForceField.rotationSpeed = -thumb[0] * 3;
            }
            else if (thumb[0] > 0)
            {
                _SpinClockwise = false;
                _ForceField.rotationSpeed = -thumb[0] * 3;
            }
            else
            {
                if (_SpinClockwise)
                    _ForceField.rotationSpeed = 1;
                else
                    _ForceField.rotationSpeed = -1;
            }
        }
        else
        {
            _ForceObject.SetActive(false);
        }
    }

    public void setActive(bool active)
    {
        _ForceObject.SetActive(active);
    }
}
