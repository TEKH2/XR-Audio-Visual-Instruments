using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Interaction;

public class HandForceManager : MonoBehaviour
{
    private GameObject _GravityLeft;
    private GameObject _GravityRight;

    private OVRInput.RawButton _LeftButton = OVRInput.RawButton.X;
    private OVRInput.RawButton _RightButton = OVRInput.RawButton.A;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        OVRInput.Update();

        Vector3 controllerPositionL = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
        Vector3 controllerPositionR = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);


        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            Debug.Log("Y PRESSED");
        }

        if (OVRInput.GetDown(OVRInput.RawButton.Y))
        {
            Debug.Log("RAW Y PRESSED");
        }

        if (_GravityLeft == null)
        {
            Debug.Log("LEFT: NULL");

            if (OVRInput.GetDown(_LeftButton))
            {
                Debug.Log("BUTTON ONE");
                _GravityLeft = new GameObject();
                _GravityLeft.name = "Gravity Left";
            }
        }
        
        if (_GravityLeft != null)
        {
            Debug.Log("LEFT: EXISTS");

            _GravityLeft.transform.position = controllerPositionL;

            if (OVRInput.GetUp(_LeftButton))
                Destroy(_GravityLeft);
        }



        if (_GravityRight == null)
        {
            Debug.Log("RIGHT: NULL");

            if (OVRInput.GetDown(_RightButton))
            {
                Debug.Log("BUTTON THREE");
                _GravityRight = new GameObject();
                _GravityRight.name = "Gravity Right";
            }
        }

        if (_GravityRight != null)
        {
            Debug.Log("RIGHT: EXISTS");

            _GravityRight.transform.position = controllerPositionR;

            if (OVRInput.GetUp(_RightButton))
                Destroy(_GravityRight);
        }


    }
}
