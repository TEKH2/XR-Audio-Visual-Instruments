using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadPosOutput : MonoBehaviour
{
    public string _Name = "head";
    public OSC _OscManager;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float[] position = new float[] {  this.transform.position.x, this.transform.position.y,
                                            this.transform.position.z};

        float[] rotation = new float[] { this.transform.rotation.x, this.transform.rotation.y,
                                            this.transform.rotation.z, this.transform.rotation.w };

        SendOSC(_Name + "/position", position);
        SendOSC(_Name + "/rotation", rotation);
    }

    public void SendOSC(string name, float[] output)
    {
        OscMessage message = new OscMessage();
        message.address = "/" + name;

        foreach (float value in output)
            message.values.Add(value);
        _OscManager.Send(message);
    }
}
