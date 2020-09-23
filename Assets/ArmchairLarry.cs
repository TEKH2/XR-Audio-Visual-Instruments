using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ArmchairLarry : MonoBehaviour
{
    public float _GrabSpeed = 1f;
    public GameObject _Target;
    private Vector3 _Direction;
    private Rigidbody _RigidBody;
    

    // Start is called before the first frame update
    void Start()
    {
        _RigidBody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        
        if (Input.GetKey("e"))
        {
            _Direction = Vector3.Normalize(_Target.transform.position - transform.position);
            _RigidBody.AddForce(_Direction * _GrabSpeed);
        }
        else if (Input.GetKey("r"))
        {

        }

    }
}
