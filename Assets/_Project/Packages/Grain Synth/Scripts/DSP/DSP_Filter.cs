using Unity.Entities;
using UnityEngine;

// A classic bi-quad filter design
public class DSP_Filter : DSPBase
{
    [Range(0f, 1f)]
    [SerializeField]
    public float _Mix = 1;

    [SerializeField]
    public FilterConstruction.FilterType _FilterType;

    [Range(0f, 1f)]
    [SerializeField]
    float _FilterCutoffNorm = 0.5f;

    [Range(0.05f, 1)]
    [SerializeField]
    float _FilterGain = 1;

    [Range(0.1f, 5)]
    [SerializeField]
    float _FilterQ = 1;

    int _SampleRate;

    public void Start()
    {
        _SampleRate = AudioSettings.outputSampleRate;
    }

    public override DSPParametersElement GetDSPBufferElement()
    {
        DSPParametersElement dspParams = new DSPParametersElement();
        dspParams._DSPType = DSPTypes.Filter;

        float cutoffFreq = AudioUtils.NormToFreq(Mathf.Clamp(_FilterCutoffNorm, 0f, 1f));
        float gain = Mathf.Clamp(_FilterGain, 0.5f, 1f);
        float q = Mathf.Clamp(_FilterQ, 0.1f, 5f);


        //--  Construct bi-quad filter coefficents
        FilterCoefficients newCoefficients;

        if (_FilterType == FilterConstruction.FilterType.LowPass)
            newCoefficients = LowPass(cutoffFreq, gain, q, _SampleRate);
        else if (_FilterType == FilterConstruction.FilterType.HiPass)
            newCoefficients = HiPass(cutoffFreq, gain, q, _SampleRate);
        else if (_FilterType == FilterConstruction.FilterType.BandPass)
            newCoefficients = BandPass(cutoffFreq, gain, q, _SampleRate);
        else if (_FilterType == FilterConstruction.FilterType.PeakNotch)
            newCoefficients = PeakNotch(cutoffFreq, gain, q, _SampleRate);
        else
            newCoefficients = AllPass(cutoffFreq, gain, q, _SampleRate);

        dspParams._SampleRate = _SampleRate;
        dspParams._Mix = _Mix;
        dspParams._Value0 = newCoefficients.a0;
        dspParams._Value1 = newCoefficients.a1;
        dspParams._Value2 = newCoefficients.a2;
        dspParams._Value3 = newCoefficients.b1;
        dspParams._Value4 = newCoefficients.b2;

        return dspParams;
    }

    public static void ProcessDSP(DSPParametersElement dspParams, DynamicBuffer<GrainSampleBufferElement> sampleBuffer, DynamicBuffer<DSPSampleBufferElement> dspBuffer)
    {
        float outputSample = 0;

        float previousX1 = 0;
        float previousX2 = 0;
        float previousY1 = 0;
        float previousY2 = 0;

        for (int i = 0; i < sampleBuffer.Length; i++)
        {
            // Apply coefficients to input singal and history data
            outputSample = (sampleBuffer[i].Value * dspParams._Value0 +
                             previousX1 * dspParams._Value1 +
                             previousX2 * dspParams._Value2) -
                             (previousY1 * dspParams._Value3 +
                             previousY2 * dspParams._Value4);

            // Set history states for signal data
            previousX2 = previousX1;
            previousX1 = sampleBuffer[i].Value;
            previousY2 = previousY1;
            previousY1 = outputSample;

            sampleBuffer[i] = new GrainSampleBufferElement { Value = Mathf.Lerp(sampleBuffer[i].Value, outputSample, dspParams._Mix) };
        }
    }

    private static FilterCoefficients LowPass(float cutoff, float gain, float q, int sampleRate)
    {
        FilterCoefficients newFilterCoefficients = new FilterCoefficients();

        float omega = cutoff * 2 * Mathf.PI / sampleRate;
        float sn = Mathf.Sin(omega);
        float cs = Mathf.Cos(omega);

        float igain = 1.0f / gain;
        float one_over_Q = 1.0f / q;
        float alpha = sn * 0.5f * one_over_Q;

        float b0 = 1.0f / (1.0f + alpha);

        newFilterCoefficients.a2 = ((1.0f - cs) * 0.5f) * b0;
        newFilterCoefficients.a0 = newFilterCoefficients.a2;
        newFilterCoefficients.a1 = (1.0f - cs) * b0;
        newFilterCoefficients.b1 = (-2.0f * cs) * b0;
        newFilterCoefficients.b2 = (1.0f - alpha) * b0;

        return newFilterCoefficients;
    }

    private static FilterCoefficients HiPass(float cutoff, float gain, float q, float sampleRate)
    {
        FilterCoefficients newFilterCoefficients = new FilterCoefficients();

        float omega = cutoff * 2 * Mathf.PI / sampleRate;
        float sn = Mathf.Sin(omega);
        float cs = Mathf.Cos(omega);

        float alpha = sn * 0.5f / q;

        float b0 = 1.0f / (1.0f + alpha);
        newFilterCoefficients.a2 = ((1.0f + cs) * 0.5f) * b0;
        newFilterCoefficients.a0 = newFilterCoefficients.a2;
        newFilterCoefficients.a1 = -(1.0f + cs) * b0;
        newFilterCoefficients.b1 = (-2.0f * cs) * b0;
        newFilterCoefficients.b2 = (1.0f - alpha) * b0;

        return newFilterCoefficients;
    }

    private static FilterCoefficients BandPass(float cutoff, float gain, float q, float sampleRate)
    {
        FilterCoefficients newFilterCoefficients = new FilterCoefficients();

        float omega = cutoff * 2 * Mathf.PI / sampleRate;
        float sn = Mathf.Sin(omega);
        float cs = Mathf.Cos(omega);

        float alpha = sn * 0.5f / q;

        float b0 = 1.0f / (1.0f + alpha);
        newFilterCoefficients.a0 = alpha * b0;
        newFilterCoefficients.a1 = 0.0f;
        newFilterCoefficients.a2 = -alpha * b0;
        newFilterCoefficients.b1 = -2.0f * cs * b0;
        newFilterCoefficients.b2 = (1.0f - alpha) * b0;

        return newFilterCoefficients;
    }

    private static FilterCoefficients PeakNotch(float cutoff, float gain, float q, float sampleRate)
    {
        FilterCoefficients newFilterCoefficients = new FilterCoefficients();

        float omega = cutoff * 2 * Mathf.PI / sampleRate;
        float sn = Mathf.Sin(omega);
        float cs = Mathf.Cos(omega);

        float alpha = sn * 0.5f / q;

        float A = Mathf.Sqrt(gain);
        float one_over_A = 1.0f / A;
        float b0 = 1.0f / (1.0f + alpha * one_over_A);

        newFilterCoefficients.a0 = (1.0f + alpha * A) * b0;
        newFilterCoefficients.b1 = (-2.0f * cs) * b0;
        newFilterCoefficients.a1 = newFilterCoefficients.b1;
        newFilterCoefficients.a2 = (1.0f - alpha * A) * b0;
        newFilterCoefficients.b2 = (1.0f - alpha * one_over_A) * b0;

        return newFilterCoefficients;
    }

    private static FilterCoefficients AllPass(float cutoff, float gain, float q, float sampleRate)
    {
        FilterCoefficients newFilterCoefficients = new FilterCoefficients();

        float omega = cutoff * 2 * Mathf.PI / sampleRate;
        float sn = Mathf.Sin(omega);
        float cs = Mathf.Cos(omega);

        float alpha = sn * 0.5f / q;

        float b0 = 1.0f / (1.0f + alpha);
        newFilterCoefficients.b1 = (-2.0f * cs) * b0;
        newFilterCoefficients.a1 = newFilterCoefficients.b1;
        newFilterCoefficients.a0 = (1.0f - alpha) * b0;
        newFilterCoefficients.b2 = newFilterCoefficients.a0;
        newFilterCoefficients.a2 = 1.0f;

        return newFilterCoefficients;
    }
}