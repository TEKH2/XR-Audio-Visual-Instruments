using Unity.Entities;
using UnityEngine;

public class DSP_Chopper : DSPBase
{
    [Range(0f, 1f)]
    [SerializeField]
    public float _Mix = 1;
    [Range(-20f, 20f)]
    [SerializeField]
    public float _Rate = 1;

    int _SampleRate;

    public void Start()
    {
        _SampleRate = AudioSettings.outputSampleRate;
    }

    public override DSPParametersElement GetDSPBufferElement()
    {
        DSPParametersElement dspBuffer = new DSPParametersElement();
        dspBuffer._DSPType = DSPTypes.Bitcrush;
        dspBuffer._Mix = _Mix;
        dspBuffer._Value0 = _Rate;

        return dspBuffer;
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

            dspBuffer[i] = new DSPSampleBufferElement { Value = Mathf.Lerp(sampleBuffer[i].Value, outputSample, dspParams._Mix) };
        }

        for (int i = 0; i < sampleBuffer.Length; i++)
        {
            sampleBuffer[i] = new GrainSampleBufferElement { Value = dspBuffer[i].Value };
        }
    }
}