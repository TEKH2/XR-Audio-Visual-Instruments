using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Granulator_TestInput : MonoBehaviour
{
    public Granulator _Granulator;

    public Vector2 _PosXRange = new Vector2(-3, 3);
    public Vector2 _PosYRange = new Vector2(0, 3);

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _Granulator._EmitGrainProps.Position = Mathf.InverseLerp(_PosXRange.x, _PosXRange.y, transform.position.x);
        _Granulator._EmitGrainProps.Pitch = Mathf.InverseLerp(_PosYRange.x, _PosYRange.y, transform.position.y);
    }
}
