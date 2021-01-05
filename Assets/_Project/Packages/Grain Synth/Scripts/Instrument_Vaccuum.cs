using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Instrument_Vaccuum : MonoBehaviour
{
    public Transform _SpherecastTransform;
    public float _Radius = .1f;
    public float _MaxDist = 20;

    public float _FalloffPower = 1;

    public LayerMask _LayerMask;

    public float _ForceStrength = 10;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;
        float normDist = 0;

        if (Physics.SphereCast(_SpherecastTransform.transform.position, _Radius, _SpherecastTransform.forward,out hit, _MaxDist, _LayerMask))
        {
            normDist = hit.distance / _MaxDist;
            float strength = Mathf.Pow(normDist, _FalloffPower) * _ForceStrength;

            Vector3 direction = (hit.point - _SpherecastTransform.position).normalized;

            hit.rigidbody.AddForce(direction * strength);
        }
    }

    private void OnDrawGizmos()
    {
        if (_SpherecastTransform != null)
        {
            for (int i = 0; i < 5; i++)
            {
                float norm = i / 4f;
                Gizmos.DrawWireSphere(_SpherecastTransform.position + (_SpherecastTransform.forward * norm * _MaxDist), _Radius);
            }
        }
    }
}
