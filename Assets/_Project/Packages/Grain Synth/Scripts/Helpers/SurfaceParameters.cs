using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurfaceParameters : MonoBehaviour
{
    public enum SurfaceParameter
    {
        Rigidity
    }

    [Range(0, 1)]
    public float _Rigidity = 1;
}
