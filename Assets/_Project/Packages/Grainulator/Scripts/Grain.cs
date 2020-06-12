using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Grain : MonoBehaviour
{
    public bool _IsPlaying = false;
    private int _GrainPos;
    private int _GrainDuration;
    private float _GrainPitch;
    private float _GrainVol;
    private AudioClip _AudioClip;
    private AudioSource _AudioSource;
    private float[] _Samples;
    private float[] _GrainSamples;
    private int _Channels;
    private int _PlaybackIndex = -1;
    private int _GrainOffset;
    public Granulator _Granulator;
    public Granulator.GrainData _GrainData;
    public float _Mass;
    private Rigidbody _RigidBody;
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

    void Start()
    {
        _RigidBody = GetComponent<Rigidbody>();
    }


    //---------------------------------------------------------------------
    void Update()
    {
        // Turn off grain if it's far away
        //if (this.transform.position.sqrMagnitude > 10000)
        //    _IsPlaying = false;

        // Turn off grain if playback has finsihed
        if (!_IsPlaying)
        {
            //this.gameObject.SetActive(false);
            //_Granulator.GrainNotPlaying(this.gameObject);
        }
    }

    public void SetWindow(float[] window)
    {
        _Window = window;
    }


    private void FixedUpdate()
    {
        _RigidBody.AddForce(new Vector3(0, -_Mass * 9.81f, 0));
    }


    //---------------------------------------------------------------------
    public void Initialise(Granulator.GrainData gd)
    {
        gameObject.transform.localPosition = gd.objectPosition;
        gameObject.transform.parent = gd.objectParent;
        _RigidBody.velocity = gd.objectVelocity;
        _Mass = gd.objectMass;

        _AudioClip = gd.audioClip;
        _Samples = new float[_AudioClip.samples * _AudioClip.channels];
        _AudioClip.GetData(_Samples, 0);
        _Channels = _AudioClip.channels;

        _GrainPos = (int)(gd.grainPos * _AudioClip.samples / _Channels) * _Channels; // Rounding to make sure pos always starts at first channel
        //_GrainDuration = (int)(_AudioClip.frequency / 1000 * gd.grainDuration) + gd.offset;  // THIS IS WRONG (SAMPLE RATE THINGS)
        _GrainDuration = (int)(_AudioClip.frequency / 1000 * gd.grainDuration);
        _AudioSource.pitch = gd.grainPitch;
        _GrainVol = gd.grainVolume;
        _GrainOffset = gd.offset;

        BuildSampleArray();
    }


    //---------------------------------------------------------------------
    private void BuildSampleArray()
    {
        // Grain array to pull samples into
        _GrainSamples = new float[_GrainDuration];

        int sourceIndex;

        // Construct grain sample data
        for (int i = 0; i < _GrainSamples.Length - _Channels; i += _Channels)
        {
            // Offset to source audio sample position for grain
            sourceIndex = _GrainPos + i;

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
        _PlaybackIndex = -_GrainOffset;
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
                    data[bufferIndex + channel] = GetNextSample() * _GrainVol;
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