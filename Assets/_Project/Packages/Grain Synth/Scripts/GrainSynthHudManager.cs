using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrainSynthHudManager : MonoBehaviour
{
    public enum ControlMode
    {
        KeyboardMouse,
        XR
    }


    public ControlMode _ControlMode;

    public GameObject _XrRig;
    public GameObject _KmRig;

    private static float _Smoothing = 0.00001f;

    private GameObject _Camera;

    void Awake()
    {
        if (_ControlMode == ControlMode.XR)
        {
            _XrRig.SetActive(true);
            _KmRig.SetActive(false);
        }
        else
        {
            _XrRig.SetActive(false);
            _KmRig.SetActive(true);
        }
    }


    void Start()
    {
        _Camera = FindObjectOfType<Camera>().gameObject;
    }

    void Update()
    {
        float smooth = 1 - Mathf.Pow(_Smoothing, Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, _Camera.transform.position, smooth);
        transform.rotation = Quaternion.Lerp(transform.rotation, _Camera.transform.rotation, smooth);
    }
}