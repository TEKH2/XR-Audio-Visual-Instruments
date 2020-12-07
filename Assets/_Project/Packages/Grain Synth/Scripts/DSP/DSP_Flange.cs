using Unity.Entities;
using UnityEngine;


// Modulated delay mono chorus/flange/garble effect
public class DSP_Flange : DSPBase
{
    [Range(0f, 1f)]
    [SerializeField]
    public float _Mix = 1;

    [Range(0f, 1f)]
    [SerializeField]
    public float _Original = 1;

    [Range(0.1f, 50f)]
    [SerializeField]
    float _Delay = 15f;

    [Range(0.01f, 1f)]
    [SerializeField]
    float _Depth = 0.1f;

    [Range(0.01f, 50f)]
    [SerializeField]
    float _Frequency = 5f;

    [Range(0f, 0.99f)]
    [SerializeField]
    float _Feedback = 0.3f;

    [Range(0f, 1f)]
    [SerializeField]
    float _PhaseSync = 1f;

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
        dspParams._SampleTail = (int) (_Delay * _Depth * _SampleRate / 1000) * 2;
        dspParams._Mix = _Mix;
        dspParams._Value0 = _Delay * _SampleRate / 1000;
        dspParams._Value1 = _Depth;
        dspParams._Value2 = _Frequency;
        dspParams._Value3 = _Feedback;
        dspParams._Value4 = _Original;
        dspParams._Value5 = _PhaseSync;

        return dspParams;
    }

    public static void ProcessDSP(DSPParametersElement dspParams, DynamicBuffer<GrainSampleBufferElement> sampleBuffer, DynamicBuffer<DSPSampleBufferElement> dspBuffer)
    {
        int writeIndex = 0;
        float delaySample = 0;
        float modIndex = 0;

        //-- Set initial phase based on DSP time to sync effect between grains
        float phase = dspParams._SampleStartTime * (dspParams._Value2 * 2 * Mathf.PI / dspParams._SampleRate) * dspParams._Value5;
        while (phase >= Mathf.PI * 2)
            phase -= Mathf.PI * 2;


        for (int i = 0; i < sampleBuffer.Length; i++)
        {
            // Modulation (delay offset) is a -1 to 1 sine wave, multiplied by the depth (0 to 1), scaled to the current sample delay offset parameter
            modIndex = (DSP_Utils_DOTS.SineOcillator(ref phase, dspParams._Value2, dspParams._SampleRate) * dspParams._Value1 * dspParams._Value0);

            // Set delay index to current index, offset by the centre delay parameter and modulation value
            writeIndex = (int)Mathf.Clamp(i + dspParams._Value0 + modIndex, 0, sampleBuffer.Length - 1);
            //Debug.Log("Current Sample: " + i + "     Flange Mod: " + modIndex + "     Write Index: " + writeIndex);

            // Combine sample in delayed buffer with current pos original sample and current pos delay sample multipled by "feedback" value
            delaySample = dspBuffer[writeIndex].Value + sampleBuffer[i].Value + dspBuffer[i].Value * dspParams._Value3;

            // Write the delayed sample
            dspBuffer[writeIndex] = new DSPSampleBufferElement { Value = delaySample };

            // Add the current input sample to the current DSP buffer for output
            dspBuffer[i] = new DSPSampleBufferElement { Value = sampleBuffer[i].Value * dspParams._Value4 + dspBuffer[i].Value };

            // Mix current sample with DSP buffer combowombo
            sampleBuffer[i] = new GrainSampleBufferElement { Value = Mathf.Lerp(sampleBuffer[i].Value, dspBuffer[i].Value, dspParams._Mix) };
        }
    }
}