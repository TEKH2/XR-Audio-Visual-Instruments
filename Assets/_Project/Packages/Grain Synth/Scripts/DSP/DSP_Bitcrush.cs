using Unity.Entities;
using UnityEngine;

// Sample and hold style bit reduction
public class DSP_Bitcrush : DSPBase
{
    [Range(0f,1f)]
    [SerializeField]
    public float _Mix = 1;
    [Range(0f, 50f)]
    [SerializeField]
    public float _CrushRatio;

    int _SampleRate;

    public void Start()
    {
        _SampleRate = AudioSettings.outputSampleRate;
    }

    public override DSPParametersElement GetDSPBufferElement()
    {
        DSPParametersElement dspParams = new DSPParametersElement();
        dspParams._DSPType = DSPTypes.Bitcrush;
        dspParams._Mix = _Mix;
        dspParams._Value0 = _CrushRatio;

        return dspParams;
    }

    public static void ProcessDSP(DSPParametersElement dspParams, DynamicBuffer<GrainSampleBufferElement> sampleBuffer, DynamicBuffer<DSPSampleBufferElement> dspBuffer)
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
            {
                outputSample = previousSample;
                count++;
            }

            sampleBuffer[i] = new GrainSampleBufferElement { Value = Mathf.Lerp(sampleBuffer[i].Value, outputSample, dspParams._Mix) };
        }
    }
}