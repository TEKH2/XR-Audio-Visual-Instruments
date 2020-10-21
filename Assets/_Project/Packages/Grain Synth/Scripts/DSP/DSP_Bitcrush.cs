using Unity.Entities;
using UnityEngine;

public class DSP_Bitcrush : DSP_Base
{
    [Range(0, 50)]
    public float _DownsampleFactor;

    public override DSPParametersElement GetDSPBufferElement()
    {
        DSPParametersElement dspBuffer = new DSPParametersElement();
        dspBuffer._DSPType = DSPTypes.Bitcrush;
        dspBuffer._Value0 = _DownsampleFactor;

        return dspBuffer;
    }

    public static void ProcessDSP(DSPParametersElement dspParams, DynamicBuffer<GrainSampleBufferElement> sampleBuffer)
    {
        int count = 0;
        float previousSample = 0;
        float outputSample = 0;

        for (int i = 0; i < sampleBuffer.Length; i++)
        {
            if (count >= dspParams._Value0)
            {
                outputSample = sampleBuffer[i].Value;
                previousSample = outputSample;
                count = 0;
            }
            else
                outputSample = previousSample;

            count++;

            outputSample = Mathf.Lerp(sampleBuffer[i].Value, outputSample, dspParams._Mix);
            sampleBuffer[i] = new GrainSampleBufferElement { Value = outputSample };
        }
    }
}