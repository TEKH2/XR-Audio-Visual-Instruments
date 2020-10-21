using System.Collections;
using Unity.Entities;
using UnityEngine;

public class DSP_Filter : DSP_Base
{
    //Filter Props
    public FilterConstruction.FilterType _Type;

    [Range(0,1)]
    public float _CutoffNorm = 1;
    [Range(0.5f,1)]
    public float _Gain = 1;
    [Range(0.1f,5)]
    public float _Q = 1;

    public override DSPParametersElement GetDSPBufferElement()
    {
        DSPParametersElement dspBuffer = new DSPParametersElement();
        dspBuffer._DSPType = DSPTypes.Filter;

        float[] coefficents;

        switch (_Type)
        {
            case FilterConstruction.FilterType.LowPass:
                {
                    break;
                }
            case FilterConstruction.FilterType.HiPass:
                {
                    break;
                }
            case FilterConstruction.FilterType.BandPass:
                {
                    break;
                }
            case FilterConstruction.FilterType.PeakNotch:
                {
                    break;
                }
            default:
                {
                    break;
                }
        }
            

        //dspBuffer._Value0 = FilterConstruction.FilterType;
        dspBuffer._Value1 = _CutoffNorm;
        dspBuffer._Value2 = _Gain;
        dspBuffer._Value3 = _Q;

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


    private static FilterCoefficients LowPass(DSP_Properties fp)
    {
        FilterCoefficients newFilterCoefficients = new FilterCoefficients();

        float omega = fp.FilterCutoff * 2 * Mathf.PI / AudioSettings.outputSampleRate;
        float sn = Mathf.Sin(omega);
        float cs = Mathf.Cos(omega);

        float igain = 1.0f / fp.FilterGain;
        float one_over_Q = 1.0f / fp.FilterQ;
        float alpha = sn * 0.5f * one_over_Q;

        float b0 = 1.0f / (1.0f + alpha);

        newFilterCoefficients.a2 = ((1.0f - cs) * 0.5f) * b0;
        newFilterCoefficients.a0 = newFilterCoefficients.a2;
        newFilterCoefficients.a1 = (1.0f - cs) * b0;
        newFilterCoefficients.b1 = (-2.0f * cs) * b0;
        newFilterCoefficients.b2 = (1.0f - alpha) * b0;

        return newFilterCoefficients;
    }

    private static FilterCoefficients HiPass(DSP_Properties fp)
    {
        FilterCoefficients newFilterCoefficients = new FilterCoefficients();

        float omega = fp.FilterCutoff * 2 * Mathf.PI /  AudioSettings.outputSampleRate;
        float sn = Mathf.Sin(omega);
        float cs = Mathf.Cos(omega);

        float alpha = sn * 0.5f / fp.FilterQ;

        float b0 = 1.0f / (1.0f + alpha);
        newFilterCoefficients.a2 = ((1.0f + cs) * 0.5f) * b0;
        newFilterCoefficients.a0 = newFilterCoefficients.a2;
        newFilterCoefficients.a1 = -(1.0f + cs) * b0;
        newFilterCoefficients.b1 = (-2.0f * cs) * b0;
        newFilterCoefficients.b2 = (1.0f - alpha) * b0;

        return newFilterCoefficients;
    }

    private static FilterCoefficients BandPass(DSP_Properties fp)
    {
        FilterCoefficients newFilterCoefficients = new FilterCoefficients();

        float omega = fp.FilterCutoff * 2 * Mathf.PI / AudioSettings.outputSampleRate;
        float sn = Mathf.Sin(omega);
        float cs = Mathf.Cos(omega);

        float alpha = sn * 0.5f / fp.FilterQ;

        float b0 = 1.0f / (1.0f + alpha);
        newFilterCoefficients.a0 = alpha * b0;
        newFilterCoefficients.a1 = 0.0f;
        newFilterCoefficients.a2 = -alpha * b0;
        newFilterCoefficients.b1 = -2.0f * cs * b0;
        newFilterCoefficients.b2 = (1.0f - alpha) * b0;

        return newFilterCoefficients;
    }

    private static FilterCoefficients PeakNotch(DSP_Properties fp)
    {
        FilterCoefficients newFilterCoefficients = new FilterCoefficients();

        float omega = fp.FilterCutoff * 2 * Mathf.PI / AudioSettings.outputSampleRate;
        float sn = Mathf.Sin(omega);
        float cs = Mathf.Cos(omega);

        float alpha = sn * 0.5f / fp.FilterQ;

        float A = Mathf.Sqrt(fp.FilterGain);
        float one_over_A = 1.0f / A;
        float b0 = 1.0f / (1.0f + alpha * one_over_A);

        newFilterCoefficients.a0 = (1.0f + alpha * A) * b0;
        newFilterCoefficients.b1 = (-2.0f * cs) * b0;
        newFilterCoefficients.a1 = newFilterCoefficients.b1;
        newFilterCoefficients.a2 = (1.0f - alpha * A) * b0;
        newFilterCoefficients.b2 = (1.0f - alpha * one_over_A) * b0;

        return newFilterCoefficients;
    }

    private static FilterCoefficients AllPass(DSP_Properties fp)
    {
        FilterCoefficients newFilterCoefficients = new FilterCoefficients();

        float omega = fp.FilterCutoff * 2 * Mathf.PI / AudioSettings.outputSampleRate;
        float sn = Mathf.Sin(omega);
        float cs = Mathf.Cos(omega);

        float alpha = sn * 0.5f / fp.FilterQ;

        float b0 = 1.0f / (1.0f + alpha);
        newFilterCoefficients.b1 = (-2.0f * cs) * b0;
        newFilterCoefficients.a1 = newFilterCoefficients.b1;
        newFilterCoefficients.a0 = (1.0f - alpha) * b0;
        newFilterCoefficients.b2 = newFilterCoefficients.a0;
        newFilterCoefficients.a2 = 1.0f;

        return newFilterCoefficients;
    }
}
