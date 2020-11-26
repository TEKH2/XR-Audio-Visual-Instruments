using UnityEngine;
using Unity.Entities;

public class DSPBase : MonoBehaviour
{
    public virtual DSPParametersElement GetDSPBufferElement()
    {
        return new DSPParametersElement();
    }
}