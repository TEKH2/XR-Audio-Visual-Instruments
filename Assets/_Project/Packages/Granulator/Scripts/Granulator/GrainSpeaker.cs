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
    GrainManager _GranulatorManager;
    public List<GrainEmitter> _AttachedGrainEmitters = new List<GrainEmitter>();

    [HideInInspector]
    public int _CurrentDSPSampleIndex = 0; //TODO read from manager

    List<GrainPlaybackData> _ActiveGrainPlaybackData = new List<GrainPlaybackData>();
    List<GrainPlaybackData> _PooledGrainPlaybackData = new List<GrainPlaybackData>();
    int _MaxGrainDataCount = 50; 
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
        _GranulatorManager = GrainManager.Instance;         

        _Window = new float[512];
        for (int i = 0; i < _Window.Length; i++)        
            _Window[i] = 0.5f * (1 - Mathf.Cos(2 * Mathf.PI * i / _Window.Length));        
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
        //Profiler.BeginSample("Add Grains 1");
        _GrainsThisFrame++;

        // Get a grain from the pool if there are any spare
        GrainPlaybackData grainPlaybackData = GetGrainFromPool();
        // ... otherwise return
        if (grainPlaybackData == null)
            return;

        _FilterSignal.fc = gd._Coefficients;
        float filteredSample;

        int sampleLength = _GranulatorManager._AudioClipLibrary._ClipsDataArray[gd._ClipIndex].Length;

        int playheadSampleIndex = (int)(gd._PlayheadPos * sampleLength);
        int durationInSamples = (int)(_GranulatorManager._AudioClipLibrary._Clips[gd._ClipIndex].frequency / 1000 * gd._Duration);
        _SamplesThisFrame += durationInSamples;

        //Profiler.EndSample();
        Profiler.BeginSample("Add Grains 2");

        // -----------------------------------------BUILD SAMPLE ARRAY        
        int sourceIndex;
        // Construct grain sample data
        for (int i = 0; i < durationInSamples; i++)
        {
            // Offset to source audio sample position for grain
            sourceIndex = playheadSampleIndex + i;

            // Ping-pong audio sample read
            sourceIndex = (int)Mathf.PingPong(sourceIndex, sampleLength - 1);

            if (_FilterSignal._Type != DSP_Filter.FilterType.None)
            {
                filteredSample = _FilterSignal.Apply(_GranulatorManager._AudioClipLibrary._ClipsDataArray[gd._ClipIndex][sourceIndex]);

                // Fill temp sample buffer
                grainPlaybackData._TempSampleBuffer[i] = filteredSample;
            }
            else
                grainPlaybackData._TempSampleBuffer[i] = _GranulatorManager._AudioClipLibrary._ClipsDataArray[gd._ClipIndex][sourceIndex];
        }

        Profiler.EndSample();

        Profiler.BeginSample("Windowing");
        // Window samples
        for (int i = 0; i < durationInSamples; i++)
        {
            if (_TraditionalWindowing)
                grainPlaybackData._GrainSamples[i] *= _Window[(int)Map(i, 0, durationInSamples, 0, _Window.Length)];
            else
            {
                // Find the norm along the array
                float norm = i / (durationInSamples - 1f);
                float windowedVolume = _GranulatorManager._WindowingCurve.Evaluate(norm);
                grainPlaybackData._GrainSamples[i] *= windowedVolume * gd._Volume;
            }           
        }

        Profiler.EndSample();

        Profiler.BeginSample("Pitching");
        // Pitching samples
        for (int i = 0; i < durationInSamples; i++)
        {
            // Find the norm along the array
            float norm = i / (durationInSamples - 1f);
            float pitchedNorm = norm * gd._Pitch;
            float sample = GetValueFromNormPosInArray(grainPlaybackData._TempSampleBuffer, pitchedNorm, durationInSamples, _DEBUG_LerpPitching);

            grainPlaybackData._GrainSamples[i] = sample;
        }
        Profiler.EndSample();

        grainPlaybackData._IsPlaying = true;
        grainPlaybackData._PlaybackIndex = 0;
        grainPlaybackData._PlaybackSampleCount = durationInSamples;
        grainPlaybackData._StartSampleIndex = gd._StartSampleIndex;

        _ActiveGrainPlaybackData.Add(grainPlaybackData);
    }

    public bool _TraditionalWindowing = false;

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

    GrainPlaybackData GetGrainFromPool()
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
    void OnAudioFilterRead(float[] data, int channels)
    {
        int samples = 0;
        // For length of audio buffer, populate with grain samples, maintaining index over successive buffers
        for (int dataIndex = 0; dataIndex < data.Length; dataIndex += channels)
        {
            samples++;
            for (int i = 0; i < _ActiveGrainPlaybackData.Count; i++)
            {
                GrainPlaybackData grainData = _ActiveGrainPlaybackData[i];

                if (grainData == null)
                    continue;

                if (_CurrentDSPSampleIndex >= grainData._StartSampleIndex)
                {
                    if (grainData._PlaybackIndex >= grainData._PlaybackSampleCount)
                        grainData._IsPlaying = false;
                    else
                    {
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
            }
        }
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
