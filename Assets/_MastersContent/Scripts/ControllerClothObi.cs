using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obi;

public class ControllerClothObi : MonoBehaviour
{
    public OSC _OscManager;

    public GameObject _ClothObject;
    public ObiSolver _ObiSolver;
    public ObiActor _ObiActor;
    private ObiCloth _ObiCloth;
    private ObiPinConstraints _ObiPinConstraints;

    private Vector3[] _VertsRest;
    private Vector3[] _VertsCurrent;

    private int _NumberOfVerts;

    // Grasping points
    public GameObject _GraspPointPrefab;
    private GameObject[] _GraspingObjects;

    private int _NumberOfGraspingPoints;
    private int[] _GraspPointVertexAllocation;


    // Start is called before the first frame update
    void Start()
    {
        _NumberOfVerts = _ObiSolver.renderablePositions.Length;
        _VertsRest = _ObiActor.positions;
        _VertsCurrent = new Vector3[_NumberOfVerts];

        _ObiCloth = _ClothObject.GetComponent<ObiCloth>();
        _ObiPinConstraints = _ClothObject.GetComponent<ObiPinConstraints>();

        CreateGraspingPoints();
    }

    // Update is called once per frame
    void Update()
    {
        GetVerts();
        //UpdateGraspPointPosition();

        SendOSC("/cloth/verts", _VertsCurrent);
    }



    private void CreateGraspingPoints()
    {
        if (_GraspingObjects != null)
            foreach (GameObject point in _GraspingObjects)
                Destroy(point);

        int xPos;
        int yPos;
        int vertsSquareRoot = Mathf.FloorToInt(Mathf.Sqrt(_NumberOfVerts));

        ObiCollider obiCollider;

        _GraspingObjects = new GameObject[_NumberOfVerts];

        for (int i = 0; i < _NumberOfVerts; i++)
        {
            xPos = i % vertsSquareRoot;
            yPos = Mathf.FloorToInt(i / vertsSquareRoot);

            _GraspingObjects[i] = Instantiate(_GraspPointPrefab, Vector3.zero, Quaternion.identity);
            _GraspingObjects[i].transform.parent = _ClothObject.transform;
            _GraspingObjects[i].transform.localPosition = Vector3.Scale(_VertsRest[i], new Vector3(10,10,10));
            _GraspingObjects[i].name = "Grasp Point " + i;

            obiCollider = _GraspingObjects[i].GetComponent<ObiCollider>();

            _ObiPinConstraints.RemoveFromSolver(null);
            ObiPinConstraintBatch batch = (ObiPinConstraintBatch)_ObiCloth.PinConstraints.GetFirstBatch();
            batch.AddConstraint(i, obiCollider, Vector3.zero, Quaternion.identity, 1);
            _ObiCloth.PinConstraints.AddToSolver(null);
        }
    }


    //private void UpdateGraspPointPosition()
    //{
    //    Rigidbody graspRigid;
    //    int currentGrasp = 0;

    //    foreach (GameObject graspPoint in _GraspingObjects)
    //    {
    //        graspRigid = graspPoint.GetComponent<Rigidbody>();
    //        graspRigid.MovePosition((_ClothObject.transform.position + _Verts[_GraspPointVertexAllocation[currentGrasp]]));

    //        currentGrasp++;
    //    }
    //}




    private void GetVerts()
    {
        for (int i = 0; i < _NumberOfVerts; i++)
        {
            //_VertsCurrent[i] = _ClothObject.transform.TransformDirection(_VertsLocal[i]);
            //_VertsCurrent[i] = _GraspingObjects[i].transform.localPosition / -10 + new Vector3( 0.5f, 0.5f, 0.5f);
            _VertsCurrent[i] = _ObiSolver.transform.InverseTransformPoint(_GraspingObjects[i].transform.position) + new Vector3(0.5f, 0.5f, 0.5f);
            //_VertsCurrent[i] = _ObiSolver.positions[i] - _ObiActor.positions[i];
        }
    }


    private void SendOSC(string name, float[] output)
    {
        OscMessage message = new OscMessage();
        message.address = name;

        foreach (float value in output)
            message.values.Add(value);
        _OscManager.Send(message);
    }

    private void SendOSC(string name, Vector3[] output)
    {
        OscMessage message = new OscMessage();
        message.address = name;

        for (int i = 0; i < output.Length; i++)
        {
            message.values.Add(i);
            message.values.Add(output[i].x);
            message.values.Add(output[i].y);
            message.values.Add(output[i].z);
            _OscManager.Send(message);
            message.values.Clear();
        }
    }
}