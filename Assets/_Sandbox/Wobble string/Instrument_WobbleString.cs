using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectNode
{
    public float _EffectStrength;
    public Transform transform { get { return _RB.transform; } }
    public Rigidbody _RB;
    public Vector3 _OriginalPos;

}

public class Instrument_WobbleString : MonoBehaviour
{
    public int _NodeCount = 100;
    public float _Radius = 1;
    public float _Degrees = 100;
    public float _YPos = 1.3f;
    float _BaseScale = .05f;    

    public Transform _RTform;
    public Transform _LTform;

    public float _SpringForce = 1;
    public float _PushForce = 1;

    EffectNode[] _Nodes;

    public AnimationCurve _TFormEffectCurve;
    public float _TformEffectRadius = .3f;

    public Vector2 ScaleRange = new Vector2(.05f, .3f);


    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    private void Init()
    {
        _Nodes = new EffectNode[_NodeCount];

        float norm = 0;
        for (int i = 0; i < _NodeCount; i++)
        {
            norm = i / (float)(_NodeCount - 1f);
            float angleRads =  ((norm * _Degrees) - (_Degrees / 2f)) * Mathf.Deg2Rad;
            Rigidbody rb = GameObject.CreatePrimitive(PrimitiveType.Cube).AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;           

            Vector3 pos = new Vector3();
            pos.x = Mathf.Sin(angleRads) * _Radius;
            pos.y = _YPos;
            pos.z = Mathf.Cos(angleRads) * _Radius;

            _Nodes[i] = new EffectNode() { _RB = rb, _OriginalPos = pos };

            // Set node pos scale rotation
            _Nodes[i].transform.localPosition = pos;
            _Nodes[i].transform.localScale = Vector3.one * _BaseScale;
            _Nodes[i].transform.localRotation = Quaternion.LookRotation(new Vector3(pos.x, 0, pos.z), Vector3.up);

            _Nodes[i].transform.SetParent(transform);
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < _NodeCount; i++)
        {
            float dist = Vector3.Distance(_RTform.position, _Nodes[i].transform.position);
            float normDist = 1 - Mathf.Clamp01(dist / _TformEffectRadius);
            float effectStrength = _TFormEffectCurve.Evaluate(normDist);

            _Nodes[i]._EffectStrength = effectStrength;


            // Add force away from the effector
            Vector3 direction = Vector3.Normalize(_RTform.position - _Nodes[i].transform.position);
            Vector3 force = direction * effectStrength * _PushForce;
            _Nodes[i]._RB.AddForceAtPosition(force, _RTform.position);

            // Add spring force to orig pos
            _Nodes[i]._RB.AddForce((_Nodes[i].transform.position - _Nodes[i]._OriginalPos).normalized * _SpringForce);

            _Nodes[i].transform.localScale = Vector3.one * Mathf.Lerp(ScaleRange.x, ScaleRange.y, _TFormEffectCurve.Evaluate(effectStrength));

            //Vector3 direction = Vector3.Normalize(_RTform.position - _Nodes[i].position);
            //Vector3 force = direction * _TFormEffectCurve.Evaluate(effectStrength);
            //_Nodes[i].AddForceAtPosition(force, _RTform.position);
        }
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < _NodeCount; i++)
        {
            Gizmos.DrawWireSphere(_RTform.position, _TformEffectRadius);
        }
    }
}
