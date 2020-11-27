using Unity.Entities;
using UnityEngine;

// Modulated delay mono chorus 
public class DSP_Flange : DSPBase
{
    [Range(0f, 1f)]
    [SerializeField]
    public float _Mix = 1;

    [Range(0f, 1000f)]
    [SerializeField]
    float _Delay = 50f;

    [Range(0.1f, 1000)]
    [SerializeField]
    float _Mod = 0.1f;

    [Range(0.1f, 300f)]
    [SerializeField]
    float _Frequency = 0.1f;

    [Range(0.1f, 0.99f)]
    [SerializeField]
    float _Feedback = 0.3f;

    int _SampleRate;

    public void Start()
    {
        _SampleRate = AudioSettings.outputSampleRate;
    }

    public override DSPParametersElement GetDSPBufferElement()
    {
        DSPParametersElement dspParams = new DSPParametersElement();
        dspParams._DSPType = DSPTypes.Flange;
        dspParams._SampleRate = _SampleRate;
        dspParams._Mix = _Mix;
        dspParams._Value0 = _Delay;
        dspParams._Value1 = _Mod;
        dspParams._Value2 = _Frequency;
        dspParams._Value3 = _Feedback;

        return dspParams;
    }

    public static void ProcessDSP(DSPParametersElement dspParams, DynamicBuffer<GrainSampleBufferElement> sampleBuffer, DynamicBuffer<DSPSampleBufferElement> dspBuffer)
    {
        float delayOutput = 0;
        int writeIndex = 0;
        float readIndex = 0;
        float phase = 0;
        float outputSample = 0;

        for (int i = 0; i < sampleBuffer.Length; i++)
        {
            readIndex = dspParams._Value0 + (DSP_Utils_DOTS.SineOcillator(ref phase, dspParams._Value2, dspParams._SampleRate) + 1.01f) * dspParams._Value1;
            delayOutput = DSP_Utils_DOTS.BufferGetSample(dspBuffer, writeIndex, readIndex);

            float combined = sampleBuffer[i].Value + delayOutput * dspParams._Value3;
            DSP_Utils_DOTS.BufferAddSample(dspBuffer, ref writeIndex, combined);

            dspBuffer[i] = new DSPSampleBufferElement { Value = Mathf.Lerp(dspBuffer[i].Value, outputSample, dspParams._Mix) };
        }

        for (int i = 0; i < sampleBuffer.Length; i++)
        {
            sampleBuffer[i] = new GrainSampleBufferElement { Value = dspBuffer[i].Value };
        }
    }
}
