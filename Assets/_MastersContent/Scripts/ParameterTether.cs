using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParameterTether : ParameterController
{
    public GameObject _Anchor;
    private GameObject _RadiusAxisLine;
    private float _Speed;
    private float _Distance;
    private float _Angular;

    // Start is called before the first frame update
    void Start()
    {
        //Game objects to graphically display parameter values
        _RadiusAxisLine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        _RadiusAxisLine.name = "Radius Axis";
        CleanUpAxisObject(_RadiusAxisLine, "");
    }

    // Update is called once per frame
    void Update()
    {
        _Distance = (_Anchor.transform.localPosition - _SliderObject.transform.localPosition).magnitude;
        _Speed = _SliderObject.GetComponent<Rigidbody>().velocity.sqrMagnitude;
        _Angular = _SliderObject.GetComponent<Rigidbody>().angularVelocity.magnitude;
        Output();

        // Display axis bars
        AxisDrawRadius();
    }

    private void AxisDrawRadius()
    {
        Vector3 sliderPos = _SliderObject.transform.localPosition;
        Vector3 between = (sliderPos - _Anchor.transform.localPosition) / 2;

        Transform line = _RadiusAxisLine.transform;

        line.localPosition = between + _Anchor.transform.localPosition;
        line.localScale = new Vector3(_AxisThickness, _Distance / 2, _AxisThickness);

        Vector3 lookPos = _Anchor.transform.position - line.position;
        Quaternion rotation = Quaternion.LookRotation(lookPos);
        rotation *= Quaternion.Euler(90,0,0);
        line.rotation = rotation;
    }


    void Output()
    {
        SendOSC(_Name + "/dist", _Distance);
        SendOSC(_Name + "/speed", _Speed);
        SendOSC(_Name + "/angular", _Angular);
    }
}
