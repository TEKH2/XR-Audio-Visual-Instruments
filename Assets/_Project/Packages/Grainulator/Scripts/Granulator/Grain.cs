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
    private float[] _Samples;
    private float[] _GrainSamples;
    private int _PlaybackIndex = -1;
    
    private float[] _Window;

    public int _FilterReadCounter = 0;

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
    public void Initialise(GrainData gd, float[] samples, int freq, bool debugLog = false, float startSample = 0)
    {
        _GrainData = gd;
        _Samples = samples;

        _FilterReadCounter = 0;

        int playheadSampleIndex = (int)(_GrainData._PlayheadPos * _Samples.Length);
        int durationInSamples = (int)(freq / 1000 * _GrainData._Duration);        
        _AudioSource.pitch = gd._Pitch;

       if(debugLog)
            Debug.Log(String.Format("Playhead pos {0}    Duration {1}   Pitch {2}    Time  {3}       Index {4}", playheadSampleIndex + (int)startSample, durationInSamples, gd._Pitch, Time.time, _Index));
        

        BuildSampleArray(playheadSampleIndex, durationInSamples);
    }

    //---------------------------------------------------------------------
    private void BuildSampleArray(int playheadSampleIndex, int duration)
    {
        // Grain array to pull samples into
        _GrainSamples = new float[duration];
        var tempSamples = new float[duration];

        int sourceIndex;

        // Construct grain sample data
        for (int i = 0; i < _GrainSamples.Length; i ++)
        {
            // Offset to source audio sample position for grain
            sourceIndex = playheadSampleIndex + i;

            // Loop to start if the grain is longer than source audio
            // TO DO: Change this to something more sonically pleasing.
            // Something like ping-pong/mirroring is better than flicking to the start
            if (sourceIndex > _Samples.Length)
            {
                sourceIndex -= _Samples.Length;
                sourceIndex = Mathf.Max(sourceIndex, 0);
            }

            tempSamples[i] = _Samples[sourceIndex];

             //_GrainSamples[i] = _Samples[sourceIndex] * _Window[(int)Map(i, 0, _GrainSamples.Length, 0, _Window.Length)] * _GrainData._Volume;
        }

        for (int i = 0; i < tempSamples.Length; i++)
        {
            int offesetIndex = _GrainData._SampleOffset % tempSamples.Length;
            _GrainSamples[i] = tempSamples[(offesetIndex + i) % _GrainSamples.Length] * _Window[(int)Map(i, 0, _GrainSamples.Length, 0, _Window.Length)] * _GrainData._Volume;
        }

        // Debug.Log(String.Format("Grain sample - Grain samples: {0}       Grain length: {1}     Time started: {2} ", _GrainSamples.Length, _GrainSamples.Length / 44100f, Time.time));

        // Reset the playback index and ready the grain!
        _PlaybackIndex = 0;
        _IsPlaying = true;
    }

    //---------------------------------------------------------------------
    // AUDIO BUFFER CALLS
    //---------------------------------------------------------------------
    float sample;
    void OnAudioFilterRead(float[] data, int channels)
    {
        // For length of audio buffer, populate with grain samples, maintaining index over successive buffers
        for (int dataIndex = 0; dataIndex < data.Length; dataIndex += channels)
        {
            if (_IsPlaying)
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
                data[dataIndex] = 0;
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
}
