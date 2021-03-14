using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshManager : MonoBehaviour
{
    private static MeshManager thisOne;

    public Mesh _Sphere;
    public Mesh _Cube;
    public Mesh _CubeFrame;
    public Mesh _HalvedSphereA;
    public Mesh _HalvedSphereB;

    void Awake()
    {
        thisOne = this;
    }
}
