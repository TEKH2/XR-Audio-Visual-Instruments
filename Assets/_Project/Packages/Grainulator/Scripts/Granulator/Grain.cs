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
    private int _Channels = 2;
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
    public void Initialise(GrainData gd, float[] samples, int channels, int freq)
    {
        _GrainData = gd;
        _Samples = samples;
        _Channels = channels;

        int playheadPos = (int)(_GrainData._PlayheadPos * _Samples.Length / _Channels) * _Channels; // Rounding to make sure pos always starts at first channel
        int duration = (int)(freq / 1000 * _GrainData._Duration);
        _AudioSource.pitch = gd._Pitch;

        BuildSampleArray(playheadPos, duration);
    }

    //---------------------------------------------------------------------
    private void BuildSampleArray(int playheadStartPos, int duration)
    {
        // Grain array to pull samples into
        _GrainSamples = new float[duration];

        int sourceIndex;

        // Construct grain sample data
        for (int i = 0; i < _GrainSamples.Length - _Channels; i += _Channels)
        {
            // Offset to source audio sample position for grain
            sourceIndex = playheadStartPos + i;

            // Loop to start if the grain is longer than source audio
            // TO DO: Change this to something more sonically pleasing.
            // Something like ping-pong/mirroring is better than flicking to the start
            while (sourceIndex + _Channels > _Samples.Length)
                sourceIndex -= _Samples.Length;

            // HACCCCCKKKY SHIT - was getting values in the negative somehow
            // Couldn't be bothered debugging just yet
            if (sourceIndex < 0)
                sourceIndex = 0;

            // Populate with source audio and apply windowing
            for (int channel = 0; channel < _Channels; channel++)
            {
                //_GrainSamples[i + channel] = _Samples[sourceIndex + channel]
                //    * Windowing(i, _GrainSamples.Length);
                
                _GrainSamples[i + channel] = _Samples[sourceIndex + channel]
                    * _Window[(int)Map(i, 0, _GrainSamples.Length, 0, _Window.Length)];
            }
        }

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
        for (int bufferIndex = 0; bufferIndex < data.Length; bufferIndex += channels)
        {
            for (int channel = 0; channel < channels; channel++)
            {
                if (_IsPlaying)
                    data[bufferIndex + channel] = GetNextSample() * _GrainData._Volume;
                else
                    data[bufferIndex + channel] = 0;
            }
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