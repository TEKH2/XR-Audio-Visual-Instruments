using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class ParameterSphere : ParameterController
{
    private GameObject _RadiusAxisLine;
    private GameObject _PolarAxisA;
    private GameObject _PolarAxisB;
    private GameObject _ElevationAxisA;
    private GameObject _ElevationAxisB;

    public Vector3 _RotatePolar = new Vector3(90, 0, 90);
    public Vector3 _RotateElevation = new Vector3(180, 0, 90);

    public bool _ShiftingAxis = true;
    public int _ArcResolution = 180;

    SphericalCoordinates _SphericalCoordinates = new SphericalCoordinates();
    SphericalCoordinates _PreviousSphericalCoordinates = new SphericalCoordinates();
    private float _Speed;
    private float _MaxSpeed = 10.0f;

    void Start()
    {
        // Get Interaction Size
        _InteractionSize = _InteractionBoundsObject.transform.localScale / 2.0f;

        //Game objects to graphically display parameter values
        _RadiusAxisLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _RadiusAxisLine.name = "Radius Axis";
        CleanUpAxisObject(_RadiusAxisLine, "");

        _PolarAxisA = CreateSphericalAxisObject("Polar Axis A");
        _PolarAxisB = CreateSphericalAxisObject("Polar Axis B");
        _ElevationAxisA = CreateSphericalAxisObject("Elevation Axis A");
        _ElevationAxisB = CreateSphericalAxisObject("Elevation Axis B");
    }

    void Update()
    {
        // Limit the sliders position and speed
        ApplyConstraints();
        // Calculate the spherical coordinates from cartesian position vector
        CalculateSphericalValues();   
        // Display axis bars
        DisplayAxis();
        // Output OSC
        Output();
        // Store current value for next update
        _PreviousSphericalCoordinates = _SphericalCoordinates;
    }

    void DisplayAxis()
    {
        if (_ShowAxis != _AxisParent.activeSelf)
            _AxisParent.SetActive(_ShowAxis);

        // Draws line and arcs to display axis information
        if (_ShowAxis)
        {
            AxisDrawRadius();
            AxisDrawCircle(_SphericalCoordinates.radius, _SphericalCoordinates.polar, _PolarAxisA, _RotatePolar); // Draw Polar
            AxisDrawCircle(_SphericalCoordinates.radius, _SphericalCoordinates.elevation, _ElevationAxisA,
                new Vector3(_RotateElevation.x, 360 - _SphericalCoordinates.polar + 90, _RotateElevation.z)); // Draw Elevation
        }
    }

    void ApplyConstraints()
    {
        // Limit Velocity
        Vector3 velocity = _SliderObject.GetComponent<Rigidbody>().velocity;
        velocity = Vector3.ClampMagnitude(velocity, _MaxSpeed);
        _Speed = _SliderObject.GetComponent<Rigidbody>().velocity.sqrMagnitude;
        // Limit Position
        Vector3 newLocalPos = _SliderObject.transform.localPosition;
        newLocalPos = Vector3.ClampMagnitude(newLocalPos, 1.0f);
    }

    void CalculateSphericalValues()
    {
        Vector3 newLocalPos = _SliderObject.transform.localPosition;
        _SphericalCoordinates = SphericalCoordinates.CartesianToSpherical(newLocalPos);
        _SphericalCoordinates.HasChanged(_PreviousSphericalCoordinates);
    }

    private void Output()
    {
        if (_SphericalCoordinates._HasChanged)
        {
            SendOSC(_Name + "/radius", _SphericalCoordinates.radius);
            SendOSC(_Name + "/polar", _SphericalCoordinates.polar / 360.0f);
            SendOSC(_Name + "/elevation", _SphericalCoordinates.elevation / 180.0f);
            SendOSC(_Name + "/speed", _Speed);
        }
    }

    private void AxisDrawRadius()
    {
        Vector3 newLocalPos = _SliderObject.transform.localPosition;
        Vector3 between = _AxisParent.transform.localPosition - newLocalPos;
        Transform line = _RadiusAxisLine.transform;

        float distance = between.magnitude;
        line.localScale = new Vector3(line.localScale.x, line.localScale.y, distance);
        line.localPosition = newLocalPos + (between / 2.0f);
        line.LookAt(_AxisParent.transform.position);
    }


    private void AxisDrawCircle(float radius, float angle, GameObject axisObject, Vector3 axisDrawRotation)
    {
        axisObject.GetComponent<MeshFilter>().mesh =
            axisObject.GetComponent<circleGenerator>().drawCircle(radius, (int)angle, 0f);

        axisObject.transform.eulerAngles = axisDrawRotation;
    }


    private GameObject CreateSphericalAxisObject(string objectName)
    {
        GameObject newObject = new GameObject { name = objectName };
        newObject.transform.parent = _AxisParent.transform;
        newObject.transform.localPosition = Vector3.zero;
        newObject.transform.localScale = new Vector3(1f, 1f, 1f);
        newObject.AddComponent<MeshFilter>();
        newObject.AddComponent<MeshRenderer>();
        newObject.GetComponent<Renderer>().material = _AxisMaterial;

        newObject.AddComponent<circleGenerator>();

        return newObject;
    }

  }