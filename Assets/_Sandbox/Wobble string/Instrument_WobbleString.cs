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

[RequireComponent(typeof(LineRenderer))]
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

    public bool _AddPush = true;
    public bool _AddScale = true;
    public bool _AddSpringToPosition = false;

    public float _Drag = .5f;
    public float _AngularDrag = .5f;

    LineRenderer _LineRend;

    public bool _AddJointSprings = false;

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    private void Init()
    {
        _Nodes = new EffectNode[_NodeCount];

        _LineRend = GetComponent<LineRenderer>();
        _LineRend.positionCount = _NodeCount;

        float norm = 0;
        for (int i = 0; i < _NodeCount; i++)
        {
            norm = i / (float)(_NodeCount - 1f);
            float angleRads =  ((norm * _Degrees) - (_Degrees / 2f)) * Mathf.Deg2Rad;
            Rigidbody rb = GameObject.CreatePrimitive(PrimitiveType.Cube).AddComponent<Rigidbody>();
            rb.gameObject.name = "Node " + i;
            rb.useGravity = false;
            //rb.isKinematic = true;           

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

        if (_AddJointSprings)
        {
            for (int i = 1; i < _NodeCount-1; i++)
            {
                Rigidbody leftAnchor = _Nodes[i - 1]._RB;
                Rigidbody rightAnchor = _Nodes[i + 1]._RB;

                SpringJoint springLeft = _Nodes[i]._RB.gameObject.AddComponent<SpringJoint>();
                springLeft.connectedBody = leftAnchor;
                springLeft.spring = 50;

                SpringJoint springRight = _Nodes[i]._RB.gameObject.AddComponent<SpringJoint>();
                springRight.connectedBody = rightAnchor;
                springRight.spring = 50;

                SpringJoint springSource = _Nodes[i]._RB.gameObject.AddComponent<SpringJoint>();
                Rigidbody rb = new GameObject("Anchor " + i).AddComponent<Rigidbody>();// _Nodes[i].transform.position);
                rb.isKinematic = true;
                rb.useGravity = false;
                springSource.connectedBody = rb;
                springSource.spring = 50;
            }

            _Nodes[0]._RB.isKinematic = true;
            _Nodes[_NodeCount-1]._RB.isKinematic = true;
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
            if (_AddPush)
                _Nodes[i]._RB.AddForceAtPosition(force, _RTform.position);

            // Add spring force to orig pos
            if (_AddSpringToPosition)                
                _Nodes[i]._RB.AddForce((_Nodes[i]._OriginalPos - _Nodes[i].transform.position).normalized * _SpringForce);

            _Nodes[i]._RB.drag = _Drag;
            _Nodes[i]._RB.angularDrag = _AngularDrag;

            if (_AddScale)
                _Nodes[i].transform.localScale = Vector3.one * Mathf.Lerp(ScaleRange.x, ScaleRange.y, _TFormEffectCurve.Evaluate(effectStrength));

            _LineRend.SetPosition(i, _Nodes[i].transform.position);

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
