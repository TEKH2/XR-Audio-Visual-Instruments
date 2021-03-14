using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SendOSC : MonoBehaviour
{
    public OSC _OscManager;

    private void Awake()
    {
        //_OscManager = GetComponent<OSC>();
    }

    public void Output(string name, float[] output)
    {
        if (_OscManager == null)
            Debug.LogWarning(name + "::SendOSC::_OscManager object is NULL. NO OSC OUTPUT!");
        else
        {
            OscMessage message = new OscMessage();
            message.address = "/" + name;

            foreach (float value in output)
                message.values.Add(value);
            _OscManager.Send(message);
        }
    }

    public void Output(string name, float output)
    {
        OscMessage message = new OscMessage();
        message.address = "/" + name;
        message.values.Add(output);
        _OscManager.Send(message);
    }
}