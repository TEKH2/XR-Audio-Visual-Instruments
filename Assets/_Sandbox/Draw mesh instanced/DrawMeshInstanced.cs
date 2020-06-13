using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawMeshInstanced : MonoBehaviour
{
    public Mesh _Mesh;
    public Material _Mat;
    public float _BoundSize = 100;
    Bounds _Bounds;

    public List<Matrix4x4> _MatrixList = new List<Matrix4x4>();

    // Start is called before the first frame update
    void Start()
    {
        _Bounds = new Bounds(transform.position, Vector3.one * _BoundSize);
    }

    // Update is called once per frame
    void Update()
    {
        // Render
        Graphics.DrawMeshInstanced(_Mesh, 0, _Mat, _MatrixList);
    }
}
