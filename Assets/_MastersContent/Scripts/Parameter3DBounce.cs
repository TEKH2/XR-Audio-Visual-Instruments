using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parameter3DBounce : ParameterController
{
    GameObject _xAxis;
    GameObject _yAxis;
    GameObject _zAxis;
    GameObject[] _BoundaryPlane;
    Vector3[] _BoundaryPlanePositions;
    Vector3[] _BoundaryPlaneScales;

    public PhysicMaterial _SliderPhysics;
    public PhysicMaterial _WallPhysics;

    private Vector3 _Value;
    private Vector3 _ValuePrevious;
    private float _Speed;

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

        // Create cubes to block perimeter of cube
        CreateBoundaryPlanes();
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

        /*
        if (_OutOfBounds)
            _Speed = 0;
        else
            _Speed = _SliderObject.GetComponent<Rigidbody>().velocity.sqrMagnitude;
        */
    }

    void PrepBoundaryCoords()
    {
        // Create vector to set the boundary positions for each side
        Vector3 BoundaryVector = new Vector3(
            _InteractionSize.y + _SliderObject.transform.localScale.y / 2 + _AxisThickness / 2,
            _InteractionSize.z + _SliderObject.transform.localScale.x / 2 + _AxisThickness / 2,
            _InteractionSize.z + _SliderObject.transform.localScale.z / 2 + _AxisThickness / 2);

        // Populate the position array
        _BoundaryPlanePositions = new Vector3[6];
        _BoundaryPlanePositions[0] = new Vector3(0, -BoundaryVector.x, 0);
        _BoundaryPlanePositions[1] = new Vector3(0, BoundaryVector.x, 0);
        _BoundaryPlanePositions[2] = new Vector3(-BoundaryVector.y, 0, 0);
        _BoundaryPlanePositions[3] = new Vector3(BoundaryVector.y, 0, 0);
        _BoundaryPlanePositions[4] = new Vector3(0, 0, -BoundaryVector.z);
        _BoundaryPlanePositions[5] = new Vector3(0, 0, BoundaryVector.z);

        // Apply scaling to each boundary object
        _BoundaryPlaneScales = new Vector3[3];
        _BoundaryPlaneScales[0] = new Vector3(_InteractionSize.x * 2.1f, _AxisThickness, _InteractionSize.z * 2.1f);
        _BoundaryPlaneScales[1] = new Vector3(_AxisThickness, _InteractionSize.y * 2.1f, _InteractionSize.z * 2.1f);
        _BoundaryPlaneScales[2] = new Vector3(_InteractionSize.x * 2.1f, _InteractionSize.y * 2.1f, _AxisThickness);
    }

    void CreateBoundaryPlanes()
    {
        // Initialise boundary array, create objects and set transforms
        PrepBoundaryCoords();
        _BoundaryPlane = new GameObject[6];
        for (int i = 0; i < 6; i++)
        {
            _BoundaryPlane[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _BoundaryPlane[i].transform.parent = _AxisParent.transform;
            _BoundaryPlane[i].transform.localPosition = _BoundaryPlanePositions[i];
            _BoundaryPlane[i].transform.localScale = _BoundaryPlaneScales[(int)Mathf.Floor(i/2)];
            _BoundaryPlane[i].GetComponent<BoxCollider>().material = _WallPhysics;

            MeshRenderer tempMeshRenderer = _BoundaryPlane[i].GetComponent<MeshRenderer>();
            Destroy(tempMeshRenderer);
        }
    }

    void Output()
    {
        if (_Value != _ValuePrevious)
        {
            SendOSC(_Name + "/x", _Value.x);
            SendOSC(_Name + "/y", _Value.y);
            SendOSC(_Name + "/z", _Value.z);
            SendOSC(_Name + "/speed", _Speed);
        }
    }
}
