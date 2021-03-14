using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerCloth : MonoBehaviour
{
    public OSC _OscManager;
    public GameObject _ClothObject;
    private SkinnedMeshRenderer _ClothSkin;
    private Cloth _Cloth;

    private Vector3[] _Verts;
    private Vector3[] _VertsLocal;
    private Vector3[] _NewVerts;

    private int _NumberOfVerts;

    // Grasping points
    public GameObject _GraspPointPrefab;
    private GameObject[] _GraspingPoints;
    private int _NumberOfGraspingPoints;
    private int[] _GraspPointVertexAllocation;


    // Start is called before the first frame update
    void Start()
    {
        _ClothSkin = _ClothObject.GetComponent<SkinnedMeshRenderer>();
        _Cloth = _ClothObject.GetComponent<Cloth>();

        _NumberOfVerts = _ClothSkin.sharedMesh.vertexCount;
        _Verts = new Vector3[_NumberOfVerts];
        _NewVerts = new Vector3[_NumberOfVerts];


        //GetVerts();
        CreateGraspingPoints();
    }

    // Update is called once per frame
    void Update()
    {
        GetVerts();
        UpdateGraspPointPosition();
        //UpdateClothVerts();

        SendOSC("/cloth/verts", _VertsLocal);
    }



    private void CreateGraspingPoints()
    {
        if (_GraspingPoints != null)
            foreach (GameObject point in _GraspingPoints)
            {
                Destroy(point);
            }

        int xPos;
        int yPos;
        int vertsSquareRoot = Mathf.FloorToInt(Mathf.Sqrt(_NumberOfVerts));
        _NumberOfGraspingPoints = (vertsSquareRoot - 2) * (vertsSquareRoot - 2);
        _GraspPointVertexAllocation = new int[_NumberOfGraspingPoints];
        _GraspingPoints = new GameObject[_NumberOfGraspingPoints];
        int currentGraspPoint = 0;

        Debug.Log("ABOUT TO LOOP: " + _NumberOfVerts);

        for (int i = 0; i < _NumberOfVerts; i++)
        {
            xPos = i % vertsSquareRoot;
            yPos = Mathf.FloorToInt(i / vertsSquareRoot);

            if (xPos > 0 && xPos < vertsSquareRoot - 1 && yPos > 0 && yPos < vertsSquareRoot - 1)
            {
                _GraspingPoints[currentGraspPoint] = Instantiate(_GraspPointPrefab, Vector3.zero, Quaternion.identity);
                _GraspingPoints[currentGraspPoint].transform.parent = _ClothObject.transform;

                //_GraspingPoints[currentGraspPoint].transform.localPosition = _Verts[i];
                _GraspingPoints[currentGraspPoint].name = "Grasp Point " + currentGraspPoint;

                _GraspPointVertexAllocation[currentGraspPoint] = i;


                currentGraspPoint++;
            }
        }
    }


    private void UpdateGraspPointPosition()
    {
        Rigidbody graspRigid;
        int currentGrasp = 0;

        foreach (GameObject graspPoint in _GraspingPoints)
        {
            graspRigid = graspPoint.GetComponent<Rigidbody>();
            graspRigid.MovePosition((_ClothObject.transform.position + _Verts[_GraspPointVertexAllocation[currentGrasp]]));

            currentGrasp++;
        }

        //_ClothSkin.
    }



    private void UpdateClothVerts()
    {
        _NewVerts = _Verts;

        int currentGrasp = 0;

        foreach (GameObject graspPoint in _GraspingPoints)
        {
            _NewVerts[_GraspPointVertexAllocation[currentGrasp]] = graspPoint.transform.localPosition;

            currentGrasp++;
        }

        _ClothSkin.sharedMesh.vertices = _NewVerts;
    }


    private void GetVerts()
    {
        _VertsLocal = _Cloth.vertices;

        for (int i = 0; i < _Verts.Length; i++)
        {
            //_Verts[i] = _VertsLocal[i];// _ClothObject.transform.TransformDirection(_VertsLocal[i]);
            _Verts[i] = _ClothObject.transform.TransformDirection(_VertsLocal[i]);
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