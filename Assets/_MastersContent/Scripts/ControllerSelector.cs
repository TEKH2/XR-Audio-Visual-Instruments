using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerSelector : MonoBehaviour
{

    public OSC _OscManager;
    public GameObject[] _Controllers;
    private int _NumberPressed;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.anyKeyDown)
        {
            foreach (GameObject controller in _Controllers)
            {
                if (controller != null)
                    controller.SetActive(false);
            }
        }


        for (int i = 0; i < 9; i++)
        {

            if (Input.GetKeyDown("" + i))
            {
                _Controllers[i].SetActive(true);
                SendOSC("/controller/active", _Controllers[i].name);
            }
        }
    }

    private void SendOSC(string name, string output)
    {
        OscMessage message = new OscMessage();
        message.address = name;
        message.values.Add(output);
        _OscManager.Send(message);
    }
}
