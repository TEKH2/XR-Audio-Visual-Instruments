using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class DSP_Utils_DOTS : MonoBehaviour
{
    public static float BufferGetSample(DynamicBuffer<DSPSampleBufferElement> dspBuffer, int writeIndex, float readIndex)
    {
        float localIndex = (float)writeIndex - readIndex;

        while (localIndex >= dspBuffer.Length)
            localIndex -= dspBuffer.Length;

        while (localIndex < 0)
            localIndex += dspBuffer.Length;

        return LinearInterpolate(dspBuffer, localIndex);
    }

    public static void BufferAddSample(DynamicBuffer<DSPSampleBufferElement> dspBuffer, ref int writeIndex, float sampleValue)
    {
        writeIndex = writeIndex % dspBuffer.Length;
        dspBuffer[writeIndex] = new DSPSampleBufferElement { Value = sampleValue };
        writeIndex++;
    }

    public static float SineOcillator(ref float phase, float freq, int sampleRate)
    {
        float inc = freq * 2 * Mathf.PI / sampleRate;
        float value = Mathf.Sin(phase);
        phase += inc;

        while (phase >= Mathf.PI * 2)
        {
            phase -= Mathf.PI * 2;
        }

        return value;
    }

    public static float LinearInterpolate(DynamicBuffer<DSPSampleBufferElement> dspBuffer, float index)
    {
        int lower = (int)index;
        int upper = lower + 1;
        if (upper == dspBuffer.Length)
            upper = 0;

        float difference = index - lower;

        return (dspBuffer[upper].Value * difference) + (dspBuffer[lower].Value * (1 - difference));
    }
}
