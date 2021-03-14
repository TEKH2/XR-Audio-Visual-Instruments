using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parameter3D : ParameterController
{
    GameObject _xAxis;
    GameObject _yAxis;
    GameObject _zAxis;
    private Vector3 _Value;
    private Vector3 _ValuePrevious;

    void Start()
    {
        // Get Interaction Size
        _InteractionSize = _InteractionBoundsObject.transform.localScale / 2.0f;

        //Game objects to graphically display parameter value
        _xAxis = GameObject.CreatePrimitive(PrimitiveType.Cube);
        CleanUpAxisObject(_xAxis, "x");
        _yAxis = GameObject.CreatePrimitive(PrimitiveType.Cube);
        CleanUpAxisObject(_yAxis, "y");
        _zAxis = GameObject.CreatePrimitive(PrimitiveType.Cube);
        CleanUpAxisObject(_zAxis, "z");
    }

    private void FixedUpdate()
    {
    }

    void Update()
    {
        // Limit the sliders position
        ApplyConstraints();
        // Display axis bars
        DisplayAxis();
        // Output OSC
        Output();
        // Store current value for next update
        _ValuePrevious = _Value;
    }



    void DisplayAxis()
    {
        if (_ShowAxis != _AxisParent.activeSelf)
            _AxisParent.SetActive(_ShowAxis);

        if (_ShowAxis)
        {
            Vector3 newLocalPos = _SliderObject.transform.localPosition;

            _xAxis.transform.localPosition =
                new Vector3(newLocalPos.x, 0, newLocalPos.z);
            _yAxis.transform.localPosition =
                new Vector3(0, newLocalPos.y, newLocalPos.z);
            _zAxis.transform.localPosition =
                new Vector3(newLocalPos.x, newLocalPos.y, 0);
        }
    }

    void ApplyConstraints()
    {
        Vector3 newLocalPos = _SliderObject.transform.localPosition;

        newLocalPos = new Vector3(
            LimitAxis(_InteractionSize.x, newLocalPos.x),
            LimitAxis(_InteractionSize.y, newLocalPos.y),
            LimitAxis(_InteractionSize.z, newLocalPos.z));

        _SliderObject.transform.localPosition = newLocalPos;

        _Value = new Vector3(
            Map(newLocalPos.x, -_InteractionSize.x, _InteractionSize.x, 0f, 1f, true),
            Map(newLocalPos.y, -_InteractionSize.y, _InteractionSize.y, 0f, 1f, true),
            Map(newLocalPos.z, -_InteractionSize.z, _InteractionSize.z, 0f, 1f, true));
    }

    void Output()
    {
        if (_Value != _ValuePrevious)
        {
            SendOSC(_Name + "/x", _Value.x);
            SendOSC(_Name + "/y", _Value.y);
            SendOSC(_Name + "/z", _Value.z);
        }
    }
}
