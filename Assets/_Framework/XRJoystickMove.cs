using EXP.XR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRJoystickMove : MonoBehaviour
{
    XRRig _XRRig;

    public float _Speed = .4f;

    // Start is called before the first frame update
    void Start()
    {
        _XRRig = FindObjectOfType<XRRig>();
    }

    // Update is called once per frame
    void Update()
    {
      
        Vector2 movement = XRControllers.Instance._LeftControllerFeatures._XRVector2Dict[XRVector2s.PrimaryAxis].Value;
        Vector3 move3d = new Vector3(movement.x, 0, movement.y);
        _XRRig.rig.transform.Translate(_XRRig.cameraGameObject.transform.TransformDirection(move3d) * _Speed);
    }
}
