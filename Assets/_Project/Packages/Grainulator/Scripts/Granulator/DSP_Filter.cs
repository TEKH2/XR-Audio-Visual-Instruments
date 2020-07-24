using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Security.Cryptography;
using UnityEngine;


// Front-end filter properties (shouldn't be passed to grains)
[System.Serializable]
public class FilterProperties
{
    public DSP_Filter.FilterType Type;

    const float _LowLimit = 20f;
    const float _HighLimit = 20000f;

    [Range(_LowLimit, _HighLimit)]
    [SerializeField]
    float _Cutoff = 1000;
    public float Cutoff
    {
        get
        {
            return Mathf.Clamp(_Cutoff, _LowLimit, _HighLimit);
        }
        set
        {
            _Cutoff = (int)Mathf.Clamp(value, _LowLimit, _HighLimit);
        }
    }

    [Range(0, 1)]
    [SerializeField]
    float _Gain = 1;
    public float Gain
    {
        get
        {
            return Mathf.Clamp(_Gain, 0f, 1f);
        }
        set
        {
            _Gain = (int)Mathf.Clamp(value, 0f, 1f);
        }
    }

    [Range(0.1f, 5)]
    [SerializeField]
    float _Q = 1;
    public float Q
    {
        get
        {
            return Mathf.Clamp(_Q, 0.1f, 5f);
        }
        set
        {
            _Q = (int)Mathf.Clamp(value, 0.1f, 5f);
        }
    }
}

// These are calculated from the FilterProperties for each grain and don't update
public class FilterCoefficients
{
    public float a0;
    public float a1;
    public float a2;
    public float b1;
    public float b2;

    public void PrintFC()
    {
        Debug.Log(string.Format("Filter Coefficients: {0} {1} {2} {3} {4}",
            a0, a1, a2, b1, b2));
    }
}

// Instantiated within each grain to maintain a very short history of the audio signal
public class FilterSignal
{
    public float previousX1 = 0;
    public float previousX2 = 0;
    public float previousY1 = 0;
    public float previousY2 = 0;

    float filteredSignal = 0;

    public FilterCoefficients fc = new FilterCoefficients();

    public void Reset()
    {
        previousX1 = 0;
        previousX2 = 0;
        previousY1 = 0;
        previousY2 = 0;
    }

    // Main filtering process - note that this will be called HEAVILY (every sample, per grain object)
    public float Apply(float input)
    {
        // Apply coefficients to input singal and history data
        filteredSignal = input * fc.a0 +
                         previousX1 * fc.a1 +
                         previousX2 * fc.a2 +
                         previousY1 * fc.b1 +
                         previousY2 * fc.b2;

        // Set history states for signal data
        previousX2 = previousX1;
        previousX1 = input;
        previousY2 = previousY1;
        previousY1 = filteredSignal;

        // Smash out the sample!
        return filteredSignal;
    }
}


public class DSP_Filter
{
    static float _SampleRate = AudioSettings.outputSampleRate;

    public enum FilterType
    {
        LowPass,
        HiPass,
        BandPass,
        PeakNotch
    }

    // This function is called to construct FilterData from FilterProperties based on the type
    public static FilterCoefficients CreateCoefficents(FilterProperties fp)
    {
        FilterCoefficients newFilterCoefficients;

        if (fp.Type == FilterType.LowPass)
            newFilterCoefficients = LowPass(fp);
        else if (fp.Type == FilterType.HiPass)
            newFilterCoefficients = HiPass(fp);
        else if (fp.Type == FilterType.BandPass)
            newFilterCoefficients = BandPass(fp);
        else if (fp.Type == FilterType.PeakNotch)
            newFilterCoefficients = PeakNotch(fp);
        else
            newFilterCoefficients = AllPass(fp);

        return newFilterCoefficients;
    }

    private static FilterCoefficients LowPass (FilterProperties fp)
    {
        FilterCoefficients newFilterCoefficients = new FilterCoefficients();

        float omega = fp.Cutoff * 2 * Mathf.PI / _SampleRate;
        float sn = Mathf.Sin(omega);
        float cs = Mathf.Cos(omega);

        float igain = 1.0f / fp.Gain;
        float one_over_Q = 1.0f / fp.Q;
        float alpha = sn * 0.5f * one_over_Q;

        float b0 = 1.0f / (1.0f + alpha);

        newFilterCoefficients.a2 = ((1.0f - cs) * 0.5f) * b0;
        newFilterCoefficients.a0 = newFilterCoefficients.a2;
        newFilterCoefficients.a1 = (1.0f - cs) * b0;
        newFilterCoefficients.b1 = (-2.0f * cs) * b0;
        newFilterCoefficients.b2 = (1.0f - alpha) * b0;

        return newFilterCoefficients;
    }

    private static FilterCoefficients HiPass (FilterProperties fp)
    {
        FilterCoefficients newFilterCoefficients = new FilterCoefficients();

        float omega = fp.Cutoff * 2 * Mathf.PI / _SampleRate;
        float sn = Mathf.Sin(omega);
        float cs = Mathf.Cos(omega);

        float alpha = sn * 0.5f / fp.Q;

        float b0 = 1.0f / (1.0f + alpha);
        newFilterCoefficients.a2 = ((1.0f + cs) * 0.5f) * b0;
        newFilterCoefficients.a0 = newFilterCoefficients.a2;
        newFilterCoefficients.a1 = -(1.0f + cs) * b0;
        newFilterCoefficients.b1 = (-2.0f * cs) * b0;
        newFilterCoefficients.b2 = (1.0f - alpha) * b0;

        return newFilterCoefficients;
    }

    private static FilterCoefficients BandPass (FilterProperties fp)
    {
        FilterCoefficients newFilterCoefficients = new FilterCoefficients();

        float omega = fp.Cutoff * 2 * Mathf.PI / _SampleRate;
        float sn = Mathf.Sin(omega);
        float cs = Mathf.Cos(omega);

        float alpha = sn * 0.5f / fp.Q;

        float b0 = 1.0f / (1.0f + alpha);
        newFilterCoefficients.a0 = alpha * b0;
        newFilterCoefficients.a1 = 0.0f;
        newFilterCoefficients.a2 = -alpha * b0;
        newFilterCoefficients.b1 = -2.0f * cs * b0;
        newFilterCoefficients.b2 = (1.0f - alpha) * b0;

        return newFilterCoefficients;
    }

    private static FilterCoefficients PeakNotch (FilterProperties fp)
    {
        FilterCoefficients newFilterCoefficients = new FilterCoefficients();

        float omega = fp.Cutoff * 2 * Mathf.PI / _SampleRate;
        float sn = Mathf.Sin(omega);
        float cs = Mathf.Cos(omega);

        float alpha = sn * 0.5f / fp.Q;

        float A = Mathf.Sqrt(fp.Gain);
        float one_over_A = 1.0f / A;
        float b0 = 1.0f / (1.0f + alpha * one_over_A);

        newFilterCoefficients.a0 = (1.0f + alpha * A) * b0;
        newFilterCoefficients.b1 = (-2.0f * cs) * b0;
        newFilterCoefficients.a1 = newFilterCoefficients.b1;
        newFilterCoefficients.a2 = (1.0f - alpha * A) * b0;
        newFilterCoefficients.b2 = (1.0f - alpha * one_over_A) * b0;

        return newFilterCoefficients;
    }

    private static FilterCoefficients AllPass (FilterProperties fp)
    {
        FilterCoefficients newFilterCoefficients = new FilterCoefficients();

        float omega = fp.Cutoff * 2 * Mathf.PI / _SampleRate;
        float sn = Mathf.Sin(omega);
        float cs = Mathf.Cos(omega);

        float alpha = sn * 0.5f / fp.Q;

        float b0 = 1.0f / (1.0f + alpha);
        newFilterCoefficients.b1 = (-2.0f * cs) * b0;
        newFilterCoefficients.a1 = newFilterCoefficients.b1;
        newFilterCoefficients.a0 = (1.0f - alpha) * b0;
        newFilterCoefficients.b2 = newFilterCoefficients.a0;
        newFilterCoefficients.a2 = 1.0f;

        return newFilterCoefficients;
    }
}




// BIQUAD FILTER COEFFICENT GENERATION (TAKEN FROM MAX/MSP GEN~.biqud)
//
// INPUT VARIABLES
//  - cf (centre frequency / cutoff)
//  - gain
//  - q (spread)
//