using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointCloudInstrument : MonoBehaviour
{
    public Granulator_MultiData _Granulator;

    GrainDataObject[] _AllGrainDataObjects;

    public float _Radius = .3f;

    // Start is called before the first frame update
    void Start()
    {
        _AllGrainDataObjects = FindObjectsOfType<GrainDataObject>();
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < _AllGrainDataObjects.Length; i++)
        {

        }
    }
}
