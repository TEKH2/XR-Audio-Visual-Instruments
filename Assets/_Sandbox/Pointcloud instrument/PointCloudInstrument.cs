using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Reference: https://twitter.com/dmvrg/status/1259662191097024512
public class PointCloudInstrument : MonoBehaviour
{
    public Granulator_MultiData _Granulator;

    GrainDataObject[] _AllGrainDataObjects;

    public float _Radius = .3f;

    public int _Cadence = 50;
    int _LastEmitSample = 0;

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
            // if in range
            if(Vector3.Distance(_AllGrainDataObjects[i].transform.position, transform.position) < _Radius )
            {
               // emite grains
            }
        }
    }
}
