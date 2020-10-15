using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using UnityEngine;
using UnityEngine.PlayerLoop;

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
    int _ChorusDelay = 0;
    public int ChorusDelay
    {
        get { return Mathf.Clamp(_ChorusDelay, 0, 1000); }
        set { _ChorusDelay = Mathf.Clamp(value, 0, 1000); }
    }
    [Range(0.1f, 1000f)]
    [SerializeField]
    float _ChorusMod = 0.1f;
    public float ChorusMod
    {
        get { return Mathf.Clamp(_ChorusMod, 0.1f, 1000f); }
        set { _ChorusMod = (int)Mathf.Clamp(value, 0.1f, 1000f); }
    }
    [Range(0.1f, 300f)]
    [SerializeField]
    float _ChorusFrequency = 0.1f;
    public float ChorusFreq
    {
        get { return Mathf.Clamp(_ChorusFrequency, 0.1f, 300f); }
        set { _ChorusFrequency = (int)Mathf.Clamp(value, 0.1f, 300f); }
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


// ---------------------------------------------------------------------------------------------------
// ---------------------------------------------------------------------------------------------------
public class BitCrush_DOTS_Example
{
    public void BitCrush_Process()
    {
        // Grain Buffers
        float[] inputBuffer = new float[1000];
        float[] outputBuffer = new float[inputBuffer.Length];

        // Parameters
        float downsampleFactor = 0;

        // Processing
        outputBuffer = BitCrush_DOTS_Static.ApplyBitCrush_Static(inputBuffer, downsampleFactor);

        // DO SOMETHING WITH OUTPUTBUFFER
    }
}

public class BitCrush_DOTS_Static
{
    // Static Method
    public static float[] ApplyBitCrush_Static(float[] inputBuffer, float downsampleFactor)
    {
        float[] outputBuffer = new float[inputBuffer.Length];
        int count = 0;
        float previousSample = 0;

        for (int i = 0; i < inputBuffer.Length; i++)
        {
            if (count >= downsampleFactor)
            {
                outputBuffer[i] = inputBuffer[i];
                previousSample = outputBuffer[i];
                count = 0;
            }
            else
            {
                outputBuffer[i] = previousSample;
                count++;
            }
        }

        return outputBuffer;
    }
}
// ---------------------------------------------------------------------------------------------------
// ---------------------------------------------------------------------------------------------------

public class Filter_DOTS_Example
{
    public void Filter_Process()
    {
        // Grain Buffers
        float[] inputBuffer = new float[1000];
        float[] outputBuffer = new float[inputBuffer.Length];

        // Parameters
        float[] coefficients = new float[5];

        // Processing
        outputBuffer = Filter_DOTS_Static.Applyfilter_Static(inputBuffer, coefficients);

        // DO SOMETHING WITH OUTPUTBUFFER
    }
}

public class Filter_DOTS_Static
{
    public static float[] Applyfilter_Static(float[] inputBuffer, float[] coefficients)
    {
        // Coefficient reference
        //public float a0;
        //public float a1;
        //public float a2;
        //public float b1;
        //public float b2;

        float previousX1 = 0;
        float previousX2 = 0;
        float previousY1 = 0;
        float previousY2 = 0;

        float[] outputBuffer = new float[inputBuffer.Length];

        for (int i = 0; i < inputBuffer.Length; i++)
        {
            // Apply coefficients to input singal and history data
            outputBuffer[i] = (inputBuffer[i] * coefficients[0] +
                             previousX1 * coefficients[1] +
                             previousX2 * coefficients[2]) -
                             (previousY1 * coefficients[3] +
                             previousY2 * coefficients[4]);

            // Set history states for signal data
            previousX2 = previousX1;
            previousX1 = inputBuffer[i];
            previousY2 = previousY1;
            previousY1 = outputBuffer[i];
        }

        return outputBuffer;
    }
}
// ---------------------------------------------------------------------------------------------------
// ---------------------------------------------------------------------------------------------------


public class ChorusProperties
{
    public int delay;
    public float mod;
    public float frequency;
    public float fb;
}

public class ChorusMono
{
    private int delayReadIndex = 0;
    private int delayWriteIndex = 0;
    private float feedback = 0;
    private float phase = 0;
    private float phaseIncrement = 0;
    private int sampleRate = AudioSettings.outputSampleRate;

    private DSP_Buffer delayBuffer;
    private DSP_Ocillator chorusOscillator;

    private ChorusProperties chorusProperties = new ChorusProperties();

    public void SetProperties(ChorusProperties properties)
    {
        chorusProperties = properties;

        delayBuffer = new DSP_Buffer();
        chorusOscillator = new DSP_Ocillator();

        delayBuffer.SetBufferSize(4410);
        chorusOscillator.SetSampleRate(sampleRate);
        chorusOscillator.SetFrequency(chorusProperties.frequency);
    }

    // PORTED FROM https://github.com/marcwilhite/StereoChorus/blob/master/Source/FractionalDelayBuffer.cpp
    public float Apply(float sampleIn)
    {
        float delayTime = chorusProperties.delay + (chorusOscillator.NextSample() + 1.01f) * chorusProperties.mod;
        float delayOutput = delayBuffer.GetSample(delayTime);

        float combined = sampleIn + delayOutput * chorusProperties.fb;
        delayBuffer.AddSample(combined);

        return combined;
    }
}


// PORTED FROM https://github.com/marcwilhite/StereoChorus/blob/master/Source/FractionalDelayBuffer.cpp
public class DSP_Ocillator
{
    private float frequency = 0;
    private int sampleRate = AudioSettings.outputSampleRate;
    private float phaseIncrement = 0;
    private float phase = 0;

    public void SetFrequency(float freq)
    {
        frequency = freq;
        UpdateIncrement();
    }
    public void SetSampleRate(int rate)
    {
        sampleRate = rate;
        UpdateIncrement();
    }

    private void UpdateIncrement()
    {
        phaseIncrement = frequency * 2 * Mathf.PI / sampleRate;
    }

    public float NextSample()
    {
        float value = 0.0f;

        value = Mathf.Sin(phase);

        phase += phaseIncrement;
        while (phase >= Mathf.PI * 2)
        {
            phase -= Mathf.PI * 2;
        }
        return value;
    }
}


// PORTED FROM https://github.com/marcwilhite/StereoChorus/blob/master/Source/FractionalDelayBuffer.cpp
public class DSP_Buffer
{
    private int index;
    private int bufferSize;
    private float[] buffer;

    public void SetBufferSize(int size)
    {
        bufferSize = size;
        buffer = new float[bufferSize];
    }

    public void AddSample(float sample)
    {
        index = index % bufferSize;
        buffer[index] = sample;
        index++;
    }

    public float GetSample(float sampleIndex)
    {
        float localIndex = (float)index - sampleIndex;

        while (localIndex >= bufferSize)
            localIndex -= bufferSize;

        while (localIndex < 0)
            localIndex += bufferSize;

        return LinearInterpolate(buffer, localIndex);
    }

    public static float LinearInterpolate(float[] data, float position)
    {
        int lower = (int)position;
        int upper = lower + 1;
        if (upper == data.Length)
            upper = 0;

        float difference = position - lower;

        return (data[upper] * difference) + (data[lower] * (1 - difference));
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