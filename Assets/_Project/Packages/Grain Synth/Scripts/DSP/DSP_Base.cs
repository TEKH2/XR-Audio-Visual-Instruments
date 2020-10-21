using UnityEngine;
using Unity.Entities;

public class DSP_Base : MonoBehaviour
{
    [Range(0,1)]
    public float _Mix = 1;

    public virtual DSPParametersElement GetDSPBufferElement()
    {
        return new DSPParametersElement();
    }
}