using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialManager : MonoBehaviour
{
    private static MaterialManager thisOne;

    public Material _Container;
    public Material _Rail;
    public Material _Slider;
    public Material _Axis;

    void Awake()
    {
        thisOne = this;
    }
}
