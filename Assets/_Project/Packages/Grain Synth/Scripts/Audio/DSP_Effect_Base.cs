using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DSP_Effect_Base
{
    public bool newEffect = true;
    protected int sampleRate = AudioSettings.outputSampleRate;

    public virtual float Process(float inputSample)
    {
        float outputSample = 0;

        return outputSample;
    }
}
