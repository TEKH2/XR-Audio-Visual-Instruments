using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public class GrainSpeaker : MonoBehaviour
{
    #region VARIABLES
    public int _SpeakerIndex = 0;

    GrainManager _GrainManager;
    public List<GrainEmitter> _AttachedGrainEmitters = new List<GrainEmitter>();

    [HideInInspector]
    public int _CurrentDSPSampleIndex = 0; //TODO read from manager

    List<GrainPlaybackData> _ActiveGrainPlaybackData = new List<GrainPlaybackData>();
    List<GrainPlaybackData> _PooledGrainPlaybackData = new List<GrainPlaybackData>();
    int _MaxGrainDataCount = 1000; 
    int GrainDataCount { get { return _ActiveGrainPlaybackData.Count + _PooledGrainPlaybackData.Count; } }

    private FilterSignal _FilterSignal = new FilterSignal();
    private float[] _Window;

    float _GrainsPerSecond = 0;
    float _GrainsThisFrame = 0;

    float _SamplesEmittedPerSecond = 0;
    float _SamplesThisFrame = 0;
    public bool _DebugLog = false;

    public float _LayeredSamples { get; private set; }
    #endregion

    private void Start()
    {
        _GrainManager = GrainManager.Instance;         

        // Build windowing lookup table: Hanning function
        _Window = new float[512];
        for (int i = 0; i < _Window.Length; i++)        
            _Window[i] = 0.5f * (1 - Mathf.Cos(2 * Mathf.PI * i / _Window.Length));

        //_FilterSignal._Type = DSP_Filter.FilterType.None;
    }

    public void ManualUpdate(int maxDSPIndex, int sampleRate)
    {
        _GrainsThisFrame = 0;
        _SamplesThisFrame = 0;

        // Profiling notes: Most time spent here but not more allocation 5 200  4-15ms   60-72 fps
        Profiler.BeginSample("Emitter");
        // Update emitters
        foreach(GrainEmitter emitter in _AttachedGrainEmitters)
        {
            emitter.ManualUpdate(this, maxDSPIndex, sampleRate);
        }
        Profiler.EndSample();

        _GrainsPerSecond = Mathf.Lerp(_GrainsPerSecond, _GrainsThisFrame / Time.deltaTime, Time.deltaTime * 4);
        _SamplesEmittedPerSecond = Mathf.Lerp(_SamplesEmittedPerSecond, _SamplesThisFrame / Time.deltaTime, Time.deltaTime * 4);
        _LayeredSamples = _SamplesEmittedPerSecond / AudioSettings.outputSampleRate;

        if (_DebugLog)
        {
            print(name + " grains p/s:   " + _GrainsPerSecond + "   samples p/s: " + _SamplesEmittedPerSecond + "   layered samples per read: " + _LayeredSamples);
            print(_CurrentDSPSampleIndex + "   " + GrainManager.Instance._CurrentDSPSample + "     " + (_CurrentDSPSampleIndex - GrainManager.Instance._CurrentDSPSample));
        }
    }

    // TODO make single method
    public void AddGrainData(GrainData gd)
    {
        _GrainsThisFrame++;

        // Get a grain from the pool if there are any spare
        GrainPlaybackData grainPlaybackData = GetGrainPlaybackDataFromPool();
        // ... otherwise return
        if (grainPlaybackData == null)
            return;

        _FilterSignal.fc = gd._Coefficients;

        int audioClipLength = _GrainManager._AudioClipLibrary._ClipsDataArray[gd._ClipIndex].Length;
        int playheadStartSample = (int)(gd._PlayheadPos * audioClipLength);
        int durationInSamples = (int)(_GrainManager._AudioClipLibrary._Clips[gd._ClipIndex].frequency / 1000 * gd._Duration);
        _SamplesThisFrame += durationInSamples;



        Profiler.BeginSample("Add Grains 2");
        // -----------------------------------------BUILD SAMPLE ARRAY        
        //int sourceIndex = playheadStartSample;

        float increment = gd._Pitch;
        float sourceValue = 0f;
        float sourceIndex = playheadStartSample;
        float sourceIndexRemainder = 0f;

        // Construct grain sample data
        for (int i = 0; i < durationInSamples; i++)
        {
            // Replacement pingpong function
            if (sourceIndex + increment < 0 || sourceIndex + increment > audioClipLength - 1)
            {
                increment = increment * -1f;
                sourceIndex -= 1;
            }

            sourceIndex += increment;
            sourceIndexRemainder = sourceIndex % 1;

            // Interpolate sample if not integer
            if (sourceIndexRemainder != 0)
                sourceValue = Mathf.Lerp(
                    _GrainManager._AudioClipLibrary._ClipsDataArray[gd._ClipIndex][(int)sourceIndex],
                    _GrainManager._AudioClipLibrary._ClipsDataArray[gd._ClipIndex][(int)sourceIndex + 1],
                    sourceIndexRemainder);
            else
                sourceValue = _GrainManager._AudioClipLibrary._ClipsDataArray[gd._ClipIndex][(int)sourceIndex];

            grainPlaybackData._GrainSamples[i] = sourceValue;
        }
        Profiler.EndSample();


        // Apply DSP filter
        //if (_FilterSignal._Type != DSP_Filter.FilterType.None)
        for (int i = 0; i < durationInSamples; i++)
        {
            grainPlaybackData._GrainSamples[i] = _FilterSignal.Apply(grainPlaybackData._GrainSamples[i]);
        }


        Profiler.BeginSample("Windowing");
        // Window samples
        for (int i = 0; i < durationInSamples; i++)
        {
            grainPlaybackData._GrainSamples[i] *= _Window[(int)Map(i, 0, durationInSamples, 0, _Window.Length)];
        }
        Profiler.EndSample();


        grainPlaybackData._IsPlaying = true;
        grainPlaybackData._PlaybackIndex = 0;
        grainPlaybackData._PlaybackSampleCount = durationInSamples;
        grainPlaybackData._DSPStartIndex = gd._StartSampleIndex;

        _ActiveGrainPlaybackData.Add(grainPlaybackData);
    }

    public void AddGrainPlaybackData(GrainPlaybackData playbackData)
    {
        _ActiveGrainPlaybackData.Add(playbackData);

        //print("Active playback data: " + _ActiveGrainPlaybackData.Count + "    duration: " + playbackData._PlaybackSampleCount);
    }

    public void Deactivate()
    {
        // Deactivate and clear all the emitters
        for (int j = 0; j < _AttachedGrainEmitters.Count; j++)
        {
            _AttachedGrainEmitters[j].Deactivate();
        }
        _AttachedGrainEmitters.Clear();

        // Move all active grain playback data to the pool
        for (int i = _ActiveGrainPlaybackData.Count - 1; i >= 0; i--)
        {
            _PooledGrainPlaybackData.Add(_ActiveGrainPlaybackData[i]);
        }
        _ActiveGrainPlaybackData.Clear();

        gameObject.SetActive(false);
    }

    public void AttachEmitter(GrainEmitter emitter)
    {
        gameObject.SetActive(true);
        emitter.Init(_CurrentDSPSampleIndex);
        _AttachedGrainEmitters.Add(emitter);
    }

    public GrainPlaybackData GetGrainPlaybackDataFromPool()
    {
        if (_PooledGrainPlaybackData.Count >= 1)
        {
            GrainPlaybackData grainPlaybackData = _PooledGrainPlaybackData[0];
            _PooledGrainPlaybackData.Remove(grainPlaybackData);

            return grainPlaybackData;
        }
        else if(GrainDataCount < _MaxGrainDataCount)
        {
            GrainPlaybackData grainPlaybackData = new GrainPlaybackData();
            return grainPlaybackData;
        }
        else
        {
            //print(name + "------  Audio output already using max grains. " + GrainDataCount + "/" + _MaxGrainDataCount);
            //print(name + "Active / Pooled: - " + _ActiveGrainPlaybackData.Count + " / " + _PooledGrainPlaybackData.Count);
            return null;
        }      
    }

    // AUDIO BUFFER CALLS
    // DSP Buffer size in audio settings
    // Best performance - 46.43991
    // Good latency - 23.21995
    // Best latency - 11.60998

    float _SamplesPerRead= 0;
    float _SamplesPerSecond = 0;
    float prevTime = 0;
    void OnAudioFilterRead(float[] data, int channels)
    {
        _SamplesPerRead = 0;
        // For length of audio buffer, populate with grain samples, maintaining index over successive buffers
        for (int dataIndex = 0; dataIndex < data.Length; dataIndex += channels)
        {            
            for (int i = 0; i < _ActiveGrainPlaybackData.Count; i++)
            {
                GrainPlaybackData grainData = _ActiveGrainPlaybackData[i];

                if (grainData == null)
                    continue;

                if (_CurrentDSPSampleIndex >= grainData._DSPStartIndex)
                {
                    //print("here");
                    if (grainData._PlaybackIndex >= grainData._PlaybackSampleCount)
                    {
                        //print("here2");
                        grainData._IsPlaying = false;
                    }
                    else
                    {
                        _SamplesPerRead++;
                        //print("here3..    "  + grainData._GrainSamples[grainData._PlaybackIndex]);
                        data[dataIndex] += grainData._GrainSamples[grainData._PlaybackIndex];
                        grainData._PlaybackIndex++;
                    }
                }
            }

            _CurrentDSPSampleIndex++;
        }

        for (int i = _ActiveGrainPlaybackData.Count - 1; i >= 0; i--)
        {
            if (_ActiveGrainPlaybackData[i] == null)
                continue;

            if (!_ActiveGrainPlaybackData[i]._IsPlaying)
            {
                // Add to pool
                _PooledGrainPlaybackData.Add(_ActiveGrainPlaybackData[i]);
                // Remove from active pist
                _ActiveGrainPlaybackData.RemoveAt(i);

                //print("here 4");
            }
        }


        // ----------------------DEBUG
        float dt = (float)AudioSettings.dspTime - prevTime;
        prevTime = (float)AudioSettings.dspTime;
        float newSamplesPerSecond = _SamplesPerRead * (1f / dt);
        float concurrentSamples = newSamplesPerSecond / 44100;
        _SamplesPerSecond = newSamplesPerSecond;// Mathf.Lerp(_SamplesPerSecond, newSamplesPerSecond, .3f ); // lerping threw a NAN??
        //print("Filter read dt: " + dt + "  Samples p/s: " + _SamplesPerSecond + "   Concurrent grains: " + Mathf.Round(concurrentSamples));
    }

    public bool _DEBUG_LerpPitching = true;
    public static float GetValueFromNormPosInArray(float[] array, float norm, int length, bool lerpResult = true)
    {
        //Profiler.BeginSample("Pitching");
        norm %= 1;
        float floatIndex = norm * (length - 1);
        int lowerIndex = (int)Mathf.Floor(floatIndex);

        if (!lerpResult)
        {
            Profiler.EndSample();
            return array[lowerIndex];
        }

        int upperIndex = Mathf.Clamp(lowerIndex + 1, lowerIndex, length - 1);
        float lerp = norm % 1;
        float output = Mathf.Lerp(array[lowerIndex], array[upperIndex], lerp);

        //Profiler.EndSample();

        return output;       
    }

    private float Map(float val, float inMin, float inMax, float outMin, float outMax)
    {
        return outMin + ((outMax - outMin) / (inMax - inMin)) * (val - inMin);
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < _AttachedGrainEmitters.Count; i++)
            {
                Gizmos.DrawLine(_AttachedGrainEmitters[i].transform.position, transform.position);
            }
        }
    }
}
