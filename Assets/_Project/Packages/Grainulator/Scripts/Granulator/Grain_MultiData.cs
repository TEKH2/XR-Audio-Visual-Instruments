using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class GrainPlaybackData
{
    public bool _IsPlaying = true;
    public float[] _GrainSamples;
    public int _PlaybackIndex = 0;

    //public GrainPlaybackData(float[] grainSamples, int playbackIndex)
    //{
    //    _GrainSamples = grainSamples;
    //    _PlaybackIndex = playbackIndex;
    //}
}

public class Grain_MultiData : MonoBehaviour
{
    List<GrainPlaybackData> _GrainPlaybackData = new List<GrainPlaybackData>();

    public void Update()
    {
        for (int i = _GrainPlaybackData.Count - 1; i >= 0; i--)
        {
            if (!_GrainPlaybackData[i]._IsPlaying)
                _GrainPlaybackData.RemoveAt(i);
        }

    }

    //---------------------------------------------------------------------
    public void AddGrainData(GrainData gd, float[] clipSamples, int freq, AnimationCurve windowCurve, bool debugLog = false, float startSample = 0, bool traditionalWindowing = false)
    {
        GrainPlaybackData grainPlaybackData = new GrainPlaybackData();

        int playheadSampleIndex = (int)(gd._PlayheadPos * clipSamples.Length);
        int durationInSamples = (int)(freq / 1000 * gd._Duration);

        // -----------------------------------------BUILD SAMPLE ARRAY
        // Grain array to pull samples into
        grainPlaybackData._GrainSamples = new float[durationInSamples];

        var tempSamples = new float[durationInSamples];
        int sourceIndex;
        // Construct grain sample data
        for (int i = 0; i < grainPlaybackData._GrainSamples.Length; i++)
        {
            // Offset to source audio sample position for grain
            sourceIndex = playheadSampleIndex + i;

            // Ping-pong audio sample read
            sourceIndex = (int)Mathf.PingPong(sourceIndex, clipSamples.Length - 1);

            // Fill temp sample buffer
            tempSamples[i] = clipSamples[sourceIndex];
        }

        // Window samples
        for (int i = 0; i < tempSamples.Length; i++)
        {
            // Set start index
            int index = gd._SampleOffset % tempSamples.Length;

            // find the norm along the array
            float norm = i / (tempSamples.Length - 1f);
            float windowedVolume = windowCurve.Evaluate(norm);

            float pitchedNorm = norm * gd._Pitch;
            float sample = GetValueFromNormPosInArray(tempSamples, pitchedNorm);

            grainPlaybackData._GrainSamples[i] = sample * windowedVolume * gd._Volume;
        }

        grainPlaybackData._IsPlaying = true;

        _GrainPlaybackData.Add(grainPlaybackData);

        if (debugLog)
            Debug.Log(String.Format("Playhead pos {0}    Duration {1}   Pitch {2}    Time  {3} ", playheadSampleIndex + (int)startSample, durationInSamples, gd._Pitch, Time.time));
    }

    //---------------------------------------------------------------------
    // AUDIO BUFFER CALLS
    // DSP Buffer size in audio settings
    // Best performance - 46.43991
    // Good latency - 23.21995
    // Best latency - 11.60998
    //---------------------------------------------------------------------

    void OnAudioFilterRead(float[] data, int channels)
    {
        float sample = 0;

        // For length of audio buffer, populate with grain samples, maintaining index over successive buffers
        for (int dataIndex = 0; dataIndex < data.Length; dataIndex += channels)
        {
            for (int i = 0; i < _GrainPlaybackData.Count; i++)
            {
                GrainPlaybackData grainData = _GrainPlaybackData[i];

                if (grainData == null)
                    continue;

                if (grainData._PlaybackIndex >= grainData._GrainSamples.Length)
                    grainData._IsPlaying = false;
                else
                {                   
                    data[dataIndex] += grainData._GrainSamples[grainData._PlaybackIndex];
                    grainData._PlaybackIndex++;
                }
            }
        }
    }

    public static float GetValueFromNormPosInArray(float[] array, float norm)
    {
        norm %= 1;
        float floatIndex = norm * (array.Length - 1);

        int lowerIndex = (int)Mathf.Floor(floatIndex);
        int upperIndex = Mathf.Clamp(lowerIndex + 1, lowerIndex, array.Length - 1);
        float lerp = norm % 1;

        return Mathf.Lerp(array[lowerIndex], array[upperIndex], lerp);
    }
}
