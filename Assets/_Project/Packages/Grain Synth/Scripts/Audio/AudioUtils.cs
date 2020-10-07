using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioUtils
{
    /* Mel Scale convertions
    
     https://en.wikipedia.org/wiki/Mel_scale
    
    Outputs a value of 1000 Mels at 1000Hz, around 0 Mels at 0Hz and ~3800 at 20KHz.
    This is a scale more in-tune with human hearing.

    */
    
    public static float FreqToMel(float freq)
    {
        float mel = 2595 * Mathf.Log10(1 + freq / 700);
        return mel;
    }

    public static float MelToFreq(float mel)
    {
        float freq= 700 * ( Mathf.Pow(10, mel / 2595) - 1);
        return freq;
    }


    public static float FreqToNorm(float freq)
    {
        float norm = 2595 * Mathf.Log10(1 + freq / 700) / 3800;
        return Mathf.Clamp(norm, 0, 1);
    }

    public static float NormToFreq(float norm)
    {
        float freq = 700 * (Mathf.Pow(10, ( norm * 3800 ) / 2595) - 1);
        return Mathf.Clamp(freq, 20, 20000);
    }

    public static float DistanceAttenuation(Vector3 listener, Vector3 pos1, Vector3 pos2)
    {
        float pos1Dist = Mathf.Abs((listener - pos1).magnitude);
        float pos2Dist = Mathf.Abs((listener - pos2).magnitude);
        float amplitude = pos1Dist / pos2Dist;
        return Mathf.Clamp(amplitude, 0.0f, 2.0f);
    }
}
