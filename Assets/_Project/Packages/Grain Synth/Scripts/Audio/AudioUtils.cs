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

    public static float EmitterFromSpeakerVolumeAdjust(Vector3 listener, Vector3 speaker, Vector3 emitter)
    {
        float speakerDist = Mathf.Abs((listener - speaker).magnitude);
        float emitterDist = Mathf.Abs((listener - emitter).magnitude);
        float amplitude = speakerDist / emitterDist;
        return Mathf.Clamp(amplitude, 0.0f, 2.0f);
    }

    // Inverse square attenuation for audio sources based on distance
    public static float EmitterFromListenerVolumeAdjust(Vector3 listener, Vector3 emitter, float maxDistance)
    {
        float emitterDist = Mathf.Clamp(Mathf.Abs((listener - emitter).magnitude) / maxDistance, 0f, 1f);
        return Mathf.Clamp(Mathf.Pow(2, -10 * emitterDist), 0f, 1f);
    }
}
