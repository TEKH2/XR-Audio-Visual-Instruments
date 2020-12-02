using Unity.Entities;
using UnityEngine;


// TODO: For this effect to be... effective... we'll have to add samples to the end of each grain so the tail of
// the chorus/flange effect (which is essentially a very short delay) can play out. Otherwise, it cuts off, and
// makes the effect far less.... effective.


// Modulated delay mono chorus 
public class DSP_Flange : DSPBase
{
    [Range(0f, 1f)]
    [SerializeField]
    public float _Mix = 1;

    [Range(0f, 100f)]
    [SerializeField]
    float _Delay = 40f;

    [Range(0.1f, 1f)]
    [SerializeField]
    float _Depth = 0.1f;

    [Range(0.01f, 20f)]
    [SerializeField]
    float _Frequency = 0.8f;

    [Range(0.1f, 0.999f)]
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
        dspParams._SampleTail = (int) (_Delay * _SampleRate * 2);
        dspParams._Mix = _Mix;
        dspParams._Value0 = _Delay;
        dspParams._Value1 = _Depth;
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

        for (int i = 0; i < sampleBuffer.Length; i++)
        {
            // 
            // delayTime = delayTime * 200 + (delay = (ocillator next sample + 1.01) * modparams * 100) * 200 ) + 0.002
            readIndex = Mathf.Clamp(dspParams._Value0 + (DSP_Utils_DOTS.SineOcillator(ref phase, dspParams._Value2, dspParams._SampleRate) + 1.01f) * dspParams._Value1, 0, sampleBuffer.Length);
            
            delayOutput = DSP_Utils_DOTS.BufferGetSample(dspBuffer, writeIndex, readIndex);

            float combined = sampleBuffer[i].Value + delayOutput * dspParams._Value3;
            DSP_Utils_DOTS.BufferAddSample(dspBuffer, ref writeIndex, combined);

            dspBuffer[i] = new DSPSampleBufferElement { Value = Mathf.Lerp(sampleBuffer[i].Value, combined, dspParams._Mix) };
        }

        for (int i = 0; i < sampleBuffer.Length; i++)
        {
            sampleBuffer[i] = new GrainSampleBufferElement { Value = dspBuffer[i].Value };
        }
    }
}


 /* Some default chorus min/max ranges
 * 
 * rate 0 - 100
 * depth 0 - 1
 * centre_delay (offset, delay?) 1 - 100
 * feedback -1 - 1
 * mix 0 - 1
 */