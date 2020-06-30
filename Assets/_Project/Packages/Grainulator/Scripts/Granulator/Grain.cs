using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;


public class Grain : MonoBehaviour
{
    public GrainData _GrainData;

    public bool _IsPlaying = false;
      
    private AudioSource _AudioSource;
    private float[] _Samples;
    private float[] _GrainSamples;
    private int _PlaybackIndex = -1;
    
    private float[] _Window;


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
    public void Initialise(GrainData gd, float[] samples, int freq)
    {
        _GrainData = gd;
        _Samples = samples;

        int playheadSampleIndex = (int)(_GrainData._PlayheadPos * _Samples.Length);
        int durationInSamples = (int)(freq / 1000 * _GrainData._Duration);
        _AudioSource.pitch = gd._Pitch;

        Debug.Log(String.Format("Playhead pos {0}    Duration {1}   Pitch {2}", playheadSampleIndex, durationInSamples, gd._Pitch));

        BuildSampleArray(playheadSampleIndex, durationInSamples);
    }

    //---------------------------------------------------------------------
    private void BuildSampleArray(int playheadSampleIndex, int duration)
    {
        // Grain array to pull samples into
        _GrainSamples = new float[duration];

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

            _GrainSamples[i] = _Samples[sourceIndex] * _Window[(int)Map(i, 0, _GrainSamples.Length, 0, _Window.Length)];
        }

        Debug.Log(String.Format("Sample 100 {0}   Sample 4000 {1}   Sample 6000 {2} ", _GrainSamples[100], _GrainSamples[4000], _GrainSamples[6000]));

        // Reset the playback index and ready the grain!
        _PlaybackIndex = -_GrainData._SampleOffset;
        _IsPlaying = true;
        //this.gameObject.SetActive(true);
    }

    //---------------------------------------------------------------------
    // AUDIO BUFFER CALLS
    //---------------------------------------------------------------------
    void OnAudioFilterRead(float[] data, int channels)
    {
        // For length of audio buffer, populate with grain samples, maintaining index over successive buffers
        for (int bufferIndex = 0; bufferIndex < data.Length; bufferIndex++)
        {            
            if (_IsPlaying)
                data[bufferIndex] = GetNextSample() * _GrainData._Volume;
            else
                data[bufferIndex] = 0;
        }
    }
    
    //---------------------------------------------------------------------
    // GET SAMPLE FROM AUDIO ARRAY TO POPULATE OTUPUT BUFFER
    //---------------------------------------------------------------------
    private float GetNextSample()
    {
        float returnSample = 0;

        if (_PlaybackIndex >= _GrainSamples.Length)        
            _IsPlaying = false;        

        if (_PlaybackIndex >= 0 && _IsPlaying)
            returnSample = _GrainSamples[_PlaybackIndex];

        _PlaybackIndex++;

        return returnSample;
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