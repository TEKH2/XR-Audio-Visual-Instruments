using Unity.Entities;
using UnityEngine;

public class DSP_Flange : DSP_Base
{
    public float _FlangeSoft;
    public float _FlangeWide;

    public override DSPParametersElement GetDSPBufferElement()
    {
        DSPParametersElement dspBuffer = new DSPParametersElement();
        dspBuffer._DSPType = DSPTypes.Flange;
        dspBuffer._Mix = _Mix;
        dspBuffer._Value0 = _FlangeSoft;
        dspBuffer._Value1 = _FlangeWide;

        return dspBuffer;
    }

    public static void ProcessDSP(DSPParametersElement dspParams, DynamicBuffer<GrainSampleBufferElement> sampleBuffer)
    {
        float outputSample = 0;

        for (int i = 0; i < sampleBuffer.Length; i++)
        {
            outputSample = Mathf.Lerp(sampleBuffer[i].Value, outputSample, dspParams._Mix);
            sampleBuffer[i] = new GrainSampleBufferElement { Value = outputSample };
        }
    }
}
