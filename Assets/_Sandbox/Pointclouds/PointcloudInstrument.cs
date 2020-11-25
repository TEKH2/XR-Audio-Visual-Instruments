using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[ExecuteInEditMode]
public class PointcloudInstrument : MonoBehaviour
{
    public Transform _InputTform;
    public VisualEffect _VFX;

    // Update is called once per frame
    void Update()
    {
        _VFX.SetVector3("InputPos-Hand", _InputTform.position);
        _VFX.SetFloat("InputRadius-Hand", _InputTform.localScale.x*.5f);
    }
}
