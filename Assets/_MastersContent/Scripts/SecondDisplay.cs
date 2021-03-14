using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecondDisplay : MonoBehaviour
{
    Camera _Camera;

    // Start is called before the first frame update
    void Start()
    {
        _Camera = GetComponent<Camera>();
        if (Display.displays.Length > 1)
        {
            _Camera.targetDisplay = 1;
            Display.displays[1].Activate();
        }
            


    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
