using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using UnityEditor.PackageManager.UI;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;


public class DSP_Effects
{

}


// Front-end filter properties (shouldn't be passed to grains)
[System.Serializable]
public class DSP_Properties
{
    // Filter
    //---------------------------------------------------------------------
    
    [Header("Filter")]
    public FilterConstruction.FilterType Type;
    
    [Range(0f, 1f)]
    [SerializeField]
    float _FilterCutoffNorm = 0.5f;
    float _FilterCutoffFreq = 2000;

    public float FilterCutoff
    {
        get {return AudioUtils.NormToFreq(_FilterCutoffNorm);}
        set {_FilterCutoffFreq = AudioUtils.NormToFreq(value);}
    }

    [Range(0.05f, 1)]
    [SerializeField]
    float _FilterGain = 1;
    public float FilterGain
    {
        get {return Mathf.Clamp(_FilterGain, 0.5f, 1f);}
        set {_FilterGain = (int)Mathf.Clamp(value, 0.5f, 1f);}
    }

    [Range(0.1f, 5)]
    [SerializeField]
    float _FilterQ = 1;
    public float FilterQ
    {
        get {return Mathf.Clamp(_FilterQ, 0.1f, 5f);}
        set {_FilterQ = (int)Mathf.Clamp(value, 0.1f, 5f);}
    }

    // Bitcrush
    //---------------------------------------------------------------------
    [Header("Bitcrush")]
    [Range(0, 50f)]
    [SerializeField]
    float _DownsampleFactor = 0;
    public float DownsampleFactor
    {
        get {return Mathf.Clamp(_DownsampleFactor, 0, 50f);}
        set {_DownsampleFactor = (int)Mathf.Clamp(value,0, 50f);}
    }

    // Chorus
    //---------------------------------------------------------------------
    [Header("Chorus")]
    [Range(0, 1000)]
    [SerializeField]
    int _ChorusCentre = 0;
    public int ChorusCentre
    {
        get { return Mathf.Clamp(_ChorusCentre, 0, 1000); }
        set { _ChorusCentre = Mathf.Clamp(value, 0, 1000); }
    }
    [Range(0.1f, 1000f)]
    [SerializeField]
    float _ChorusBW = 0.1f;
    public float ChorusBW
    {
        get { return Mathf.Clamp(_ChorusBW, 0.1f, 1000f); }
        set { _ChorusBW = (int)Mathf.Clamp(value, 0.1f, 1000f); }
    }
    [Range(0.1f, 300f)]
    [SerializeField]
    float _ChorusRate = 0.1f;
    public float ChorusRate
    {
        get { return Mathf.Clamp(_ChorusRate, 0.1f, 300f); }
        set { _ChorusRate = (int)Mathf.Clamp(value, 0.1f, 300f); }
    }
    [Range(0, 0.99f)]
    [SerializeField]
    float _ChorusFB = 0;
    public float ChorusFB
    {
        get { return Mathf.Clamp(_ChorusFB, 0, 0.99f); }
        set { _ChorusFB = (int)Mathf.Clamp(value, 0, 0.99f); }
    }
}

[System.Serializable]
// These are calculated from the FilterProperties for each grain and don't update
public class FilterCoefficients
{
    public float a0;
    public float a1;
    public float a2;
    public float b1;
    public float b2;
}

// Instantiated within each grain to maintain a very short history of the audio signal
public class Filter
{
    public FilterConstruction.FilterType _Type = FilterConstruction.FilterType.None;

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
    public float Apply(float sampleIn)
    {
        // Apply coefficients to input singal and history data
        filteredSignal = ( sampleIn * fc.a0 +
                         previousX1 * fc.a1 +
                         previousX2 * fc.a2 ) -
                         ( previousY1 * fc.b1 +
                         previousY2 * fc.b2 );

        // Set history states for signal data
        previousX2 = previousX1;
        previousX1 = sampleIn;
        previousY2 = previousY1;
        previousY1 = filteredSignal;

        // Smash out the sample!
        return filteredSignal;
    }
}

public class BitCrush
{
    public float downsampleFactor = 0;
    private float previousValue = 0;
    private int count = 0;

    public float Apply(float sampleIn)
    {
        float sampleOut;

        if (count >= downsampleFactor)
        {
            sampleOut = sampleIn;
            previousValue = sampleOut;
            count = 0;
        }
        else
            sampleOut = previousValue;

        return sampleOut;
    }
}


public class ChorusProperties
{
    public int centre;
    public float bw;
    public float rate;
    public float fb;
}

public class ChorusMono
{
    private float[] delayBuffer;
    private int delayReadIndex = 0;
    private int delayWriteIndex = 0;
    private float feedback = 0;
    private float sinInput = 0;
    private float oscRate = 0;
    private int sampleRate = AudioSettings.outputSampleRate;

    private ChorusProperties chorusProperties = new ChorusProperties();

    public void SetProperties(ChorusProperties properties)
    {
        chorusProperties = properties;
        delayBuffer = new float[(int)(properties.bw + properties.centre)];
        oscRate = chorusProperties.rate / sampleRate * 2 * Mathf.PI;
    }

    public float Apply(float sampleIn)
    {
        // Increment oscillation and get index for delay writting
        sinInput += oscRate;
        delayWriteIndex = (delayReadIndex + Mathf.Max(chorusProperties.centre + (int)(chorusProperties.bw * Mathf.Sin(sinInput)), 0)) % delayBuffer.Length;

        // Read from delay buffer and increment index
        float delaySampleOutput = delayBuffer[delayReadIndex];
        delayReadIndex = delayReadIndex++ % delayBuffer.Length;

        // Write to delay buffer
        feedback = -delaySampleOutput * chorusProperties.fb;
        delayBuffer[delayWriteIndex] += sampleIn + feedback;

        return delaySampleOutput + sampleIn;
    }
}


public class FilterConstruction
{
    static float _SampleRate = AudioSettings.outputSampleRate;

    public enum FilterType
    {
        LowPass,
        HiPass,
        BandPass,
        PeakNotch,
        None
    }

    // This function is called to construct FilterData from FilterProperties based on the type
    public static FilterCoefficients CreateCoefficents(DSP_Properties fp)
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

    private static FilterCoefficients LowPass (DSP_Properties fp)
    {
        FilterCoefficients newFilterCoefficients = new FilterCoefficients();

        float omega = fp.FilterCutoff * 2 * Mathf.PI / _SampleRate;
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

    private static FilterCoefficients HiPass (DSP_Properties fp)
    {
        FilterCoefficients newFilterCoefficients = new FilterCoefficients();

        float omega = fp.FilterCutoff * 2 * Mathf.PI / _SampleRate;
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

    private static FilterCoefficients BandPass (DSP_Properties fp)
    {
        FilterCoefficients newFilterCoefficients = new FilterCoefficients();

        float omega = fp.FilterCutoff * 2 * Mathf.PI / _SampleRate;
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

    private static FilterCoefficients PeakNotch (DSP_Properties fp)
    {
        FilterCoefficients newFilterCoefficients = new FilterCoefficients();

        float omega = fp.FilterCutoff * 2 * Mathf.PI / _SampleRate;
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

    private static FilterCoefficients AllPass (DSP_Properties fp)
    {
        FilterCoefficients newFilterCoefficients = new FilterCoefficients();

        float omega = fp.FilterCutoff * 2 * Mathf.PI / _SampleRate;
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




// BIQUAD FILTER COEFFICENT GENERATION (TAKEN FROM MAX/MSP GEN~.biqud)
//
// INPUT VARIABLES
//  - cf (centre frequency / cutoff)
//  - gain
//  - q (spread)
//