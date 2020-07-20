using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;


public class Grain : MonoBehaviour
{
    public GrainData _GrainData;

    public bool _IsPlaying = false;

    public int _Index;

    public AudioSource _AudioSource;
    private float[] _GrainSamples;
    private int _PlaybackIndex = -1;
    
    private float[] _Window;

    public int _FilterReadCounter = 0;

    public bool _Prewarmed = false;

    //---------------------------------------------------------------------

    private void Awake()
    {
        _Window = new float[512];

        for (int i = 0; i < _Window.Length; i++)
        {
            _Window[i] = 0.5f * (1 - Mathf.Cos(2 * Mathf.PI * i / _Window.Length));
        }

        _AudioSource = this.gameObject.GetComponent<AudioSource>();

        if (_AudioSource == null)
            _AudioSource = this.gameObject.AddComponent<AudioSource>();
    }

    //---------------------------------------------------------------------
    public void Initialise(GrainData gd, float[] samples, int freq, AnimationCurve windowCurve, bool debugLog = false, float startSample = 0, bool traditionalWindowing = false)
    {
        _GrainData = gd;

        _Prewarmed = false;

        _FilterReadCounter = 0;

        int playheadSampleIndex = (int)(_GrainData._PlayheadPos * samples.Length);
        int durationInSamples = (int)(freq / 1000 * _GrainData._Duration);      

       // -----------------------------------------BUILD SAMPLE ARRAY
        // Grain array to pull samples into
        _GrainSamples = new float[durationInSamples];
        var tempSamples = new float[durationInSamples];

        int sourceIndex;

        // Construct grain sample data
        for (int i = 0; i < _GrainSamples.Length; i++)
        {
            // Offset to source audio sample position for grain
            sourceIndex = playheadSampleIndex + i;

            // Ping-pong audio sample read
            sourceIndex = (int)Mathf.PingPong(sourceIndex, samples.Length - 1);

            // Fill temp sample buffer
            tempSamples[i] = samples[sourceIndex];
        }

        // Window samples
        for (int i = 0; i < tempSamples.Length; i++)
        {
            // Set start index
            //int index = _GrainData._SampleOffset % tempSamples.Length;

            // find the norm along the array
            float norm = i / (tempSamples.Length - 1f);            
            float windowedVolume = windowCurve.Evaluate(norm);

            float pitchedNorm = norm * _GrainData._Pitch;
            float sample = GetValueFromNormPosInArray(tempSamples, pitchedNorm);
            sample = sample * windowedVolume;

            if (traditionalWindowing)
                _GrainSamples[i] = sample * _Window[(int)Map(i, 0, _GrainSamples.Length, 0, _Window.Length)] * _GrainData._Volume;
            else
                _GrainSamples[i] = sample * windowedVolume * _GrainData._Volume;         
        }

        // Reset the playback index and ready the grain!
        _PlaybackIndex = 0;
        _IsPlaying = true;

        if (debugLog)
            Debug.Log(String.Format("Playhead pos {0}    Duration {1}   Pitch {2}    Time  {3}       Index {4}", playheadSampleIndex + (int)startSample, durationInSamples, gd._Pitch, Time.time, _Index));


        // Debug.Log(String.Format("Grain sample - Grain samples: {0}       Grain length: {1}     Time started: {2} ", _GrainSamples.Length, _GrainSamples.Length / 44100f, Time.time));
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
            if (_IsPlaying)// && _Prewarmed)
            {
                sample = 0;

                // Finish playing if playback index is larger than the grain sample length
                if (_PlaybackIndex >= _GrainSamples.Length)
                    _IsPlaying = false;
                // Otherwise, if grain is playing and has reached the offset, get the next sample
                else if (_PlaybackIndex >= 0 && _IsPlaying)
                    sample = _GrainSamples[_PlaybackIndex];

                data[dataIndex] = sample;

                _PlaybackIndex++;

            }
            else
            {
                data[dataIndex] = sample;
                _FilterReadCounter++;
            }
        }
    }

    public void Activate(bool active)
    {
        _FilterReadCounter = 0;
        gameObject.SetActive(active);        
    }

    //---------------------------------------------------------------------
    private float Windowing(int currentSample, int grainLength)
    {
        float outputSample = 0.5f * (1 - Mathf.Cos(2 * Mathf.PI * currentSample / grainLength));
        return outputSample;
    }

    //---------------------------------------------------------------------
    private float Map(float val, float inMin, float inMax, float outMin, float outMax)
    {
        return outMin + ((outMax - outMin) / (inMax - inMin)) * (val - inMin);
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
