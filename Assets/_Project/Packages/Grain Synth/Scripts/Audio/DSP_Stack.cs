using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DSP_Stack : MonoBehaviour
{
    public enum DSP_Effect_Type
    {
        Filter,
        BitCrush,
        Chorus,
    }

    public DSP_Effect_Type[] DSP_Type = new DSP_Effect_Type[2];
    private List<DSP_Effect_Base> _DspEffectBase;

    void Start()
    {
        DSP_Type[0] = DSP_Effect_Type.Filter;
        DSP_Type[1] = DSP_Effect_Type.BitCrush;
    }

    void Update()
    {
        foreach (var type in DSP_Type)
        {
            switch (type)
            {
                case DSP_Effect_Type.Filter:
                    break;
            }
        }
    }
}
