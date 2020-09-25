using MiscUtil.Collections.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FPS : MonoBehaviour
{
    public TMPro.TextMeshProUGUI _Text;
   
    void Update()
    {
        _Text.text = "FPS: " + (1f / Time.smoothDeltaTime).ToString("##");
    }
}
