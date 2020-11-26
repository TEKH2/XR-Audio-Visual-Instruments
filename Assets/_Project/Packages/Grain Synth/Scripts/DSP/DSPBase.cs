using UnityEngine;
using Unity.Entities;

public class DSPBase : MonoBehaviour
{
    //---   ASSIGN DSP PARAMETER VALUES IN HERE PER INHERITED CLASS
    public virtual DSPParametersElement GetDSPBufferElement()
    {
        return new DSPParametersElement();
    }
}