using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Supershape : MonoBehaviour
{
    public int _PointCount;
    Vector3[] _Points;

    public float _M;
    public float _N1;
    public float _N2;
    public float _N3;

    public float _Scalar = 1;
    public float _PointSize = .01f;

    // Update is called once per frame
    void Update()
    {
        if (_Points.Length != _PointCount || _Points == null)
            _Points = new Vector3[_PointCount];

        for (int i = 0; i < _Points.Length; i++)
        {            
            float phi = i * Mathf.PI * 2 / _PointCount;
            _Points[i] = Eval(_M, _N1, _N2, _N3, phi);
        }
    }

    Vector3 Eval(float m, float n1, float n2, float n3, float phi)
    {
        Vector3 point = Vector3.zero;

        float r;
        float t1, t2;
        float a = 1, b = 1;

        t1 = Mathf.Cos(m * phi / 4) / a;
        t1 = Mathf.Abs(t1);
        t1 = Mathf.Pow(t1, n2);

        t2 = Mathf.Sin(m * phi / 4) / b;
        t2 = Mathf.Abs(t2);
        t2 = Mathf.Pow(t2, n3);

        r = Mathf.Pow(t1 + t2, 1 / n1);
        if (Mathf.Abs(r) == 0)
        {
            point.x = 0;
            point.y = 0;
        }
        else
        {
            r = 1 / r;
            point.x = r * Mathf.Cos(phi) * _Scalar;
            point.y = r * Mathf.Sin(phi) * _Scalar;
        }

        return point;
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            for (int i = 0; i < _Points.Length; i++)
            {
                Gizmos.DrawCube(_Points[i] + transform.position, Vector3.one * _PointSize);
            }
        }
    }
}
