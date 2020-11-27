using Unity.Entities;
using UnityEngine;

public class DSP_Bitcrush : DSPBase
{
    [Range(0f,1f)]
    [SerializeField]
    public float _Mix = 1;
    [Range(0f, 50f)]
    [SerializeField]
    public float _CrushRatio;

    public override DSPParametersElement GetDSPBufferElement()
    {
        DSPParametersElement dspBuffer = new DSPParametersElement();
        dspBuffer._DSPType = DSPTypes.Bitcrush;
        dspBuffer._Value0 = _Mix;
        dspBuffer._Value1 = _CrushRatio;

        return dspBuffer;
    }

    public static void ProcessDSP(DSPParametersElement dspParams, DynamicBuffer<GrainSampleBufferElement> sampleBuffer, DynamicBuffer<DSPSampleBufferElement> dspBuffer)
    {
        //float[] outputBuffer = new float[sampleBuffer.Length];
        int count = 0;
        float previousSample = 0;
        float outputSample = 0;

        for (int i = 0; i < sampleBuffer.Length; i++)
        {
            if (count >= dspParams._Value1)
            {
                outputSample = sampleBuffer[i].Value;
                previousSample = outputSample;
                count = 0;
            }
            else
            {
                outputSample = previousSample;
                count++;
            }

            dspBuffer[i] = new DSPSampleBufferElement { Value = Mathf.Lerp(sampleBuffer[i].Value, outputSample, dspParams._Value0) };
        }

        // Fill sample buffer element
        // Kept this as a sepearate loop for consistancy in case future DSP effects require separate for loops
        // to populate effect buffer vs populating the output buffer.. REVISE ONCE DONE
        for (int i = 0; i < sampleBuffer.Length; i++)
        {
            sampleBuffer[i] = new GrainSampleBufferElement { Value = dspBuffer[i].Value };
        }
    }
}