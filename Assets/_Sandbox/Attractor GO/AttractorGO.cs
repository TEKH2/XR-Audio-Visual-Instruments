using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttractorGO : MonoBehaviour
{
    public List<Rigidbody> _RBs;
    public float _MaxDistanceSqrd = 10;
    public float _Strength = 2;

    // Update is called once per frame
    void Update()
    {
        foreach (var rb in _RBs)
        {           
            Vector3 vectorToAttractor = transform.position - rb.transform.position;
            float distSqrd = vectorToAttractor.sqrMagnitude;

            if (distSqrd < _MaxDistanceSqrd)
            {
                // Alter linear velocity
                rb.velocity += _Strength * (vectorToAttractor / Mathf.Sqrt(distSqrd));
            }
        }
    }
}
