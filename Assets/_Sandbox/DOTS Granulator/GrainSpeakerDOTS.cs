﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrainSpeakerDOTS : MonoBehaviour
{
    GrainManager _GrainManager;
    public int _SpeakerIndex = 0;

    List<GrainPlaybackData> _ActiveGrainPlaybackData = new List<GrainPlaybackData>();
    List<GrainPlaybackData> _PooledGrainPlaybackData = new List<GrainPlaybackData>();

    int _GrainPlaybackDataToPool = 100;
    int _MaxGrainPlaybackDataCout = 200;

    int GrainDataCount { get { return _ActiveGrainPlaybackData.Count + _PooledGrainPlaybackData.Count; } }

    DebugGUI_Granulator _DebugGUI;
    int prevStartSample = 0;

    public bool _DebugLog = false;

    public void Start()
    {
        // Get ref to grain manager
        _GrainManager = GrainManager.Instance;

        // Pool grain playback data
        for (int i = 0; i < _GrainPlaybackDataToPool; i++)        
            _PooledGrainPlaybackData.Add(new GrainPlaybackData());        
    }

    public void AddGrainPlaybackData(GrainPlaybackData playbackData)
    {
        _ActiveGrainPlaybackData.Add(playbackData);

        int samplesBetweenGrains = playbackData._DSPStartIndex - prevStartSample;
        float msBetweenGrains = (samplesBetweenGrains / (float)AudioSettings.outputSampleRate) * 1000;
        float DSPSampleDiff = playbackData._DSPStartIndex - _GrainManager._CurrentDSPSample;
        int DSPMSDiff = (int)((DSPSampleDiff / (float)AudioSettings.outputSampleRate) * 1000);

        if (_DebugLog)
        {
            print
            (
                "Grain added. Start sample: " + playbackData._DSPStartIndex +
                " Cadence samples: " + samplesBetweenGrains +
                " Cadence m/s:   " + msBetweenGrains +
                " DSP sample diff:   " + DSPSampleDiff +
                " DSP m/s diff:   " + DSPMSDiff
            );
        }

        _DebugGUI.LogLatency(DSPMSDiff);

        prevStartSample = playbackData._DSPStartIndex;
    }

    public void Deactivate()
    {       
        // Move all active grain playback data to the pool
        for (int i = _ActiveGrainPlaybackData.Count - 1; i >= 0; i--)
        {
            _PooledGrainPlaybackData.Add(_ActiveGrainPlaybackData[i]);
        }
        _ActiveGrainPlaybackData.Clear();

        gameObject.SetActive(false);
    }

    public GrainPlaybackData GetGrainPlaybackDataFromPool()
    {
        if (_PooledGrainPlaybackData.Count >= 1)
        {
            GrainPlaybackData grainPlaybackData = _PooledGrainPlaybackData[0];
            _PooledGrainPlaybackData.Remove(grainPlaybackData);

            return grainPlaybackData;
        }
        else if (GrainDataCount < _MaxGrainPlaybackDataCout)
        {
            GrainPlaybackData grainPlaybackData = new GrainPlaybackData();
            return grainPlaybackData;
        }
        else
        {
            print(name + "------  Audio output already using max grains. " + GrainDataCount + "/" + _MaxGrainPlaybackDataCout);
            return null;
        }
    }

    // AUDIO BUFFER CALLS
    // DSP Buffer size in audio settings
    // Best performance - 46.43991
    // Good latency - 23.21995
    // Best latency - 11.60998

    float _SamplesPerRead = 0;
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

                if (_GrainManager._CurrentDSPSample >= grainData._DSPStartIndex)
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


        // ----------------------DEBUG
        float dt = (float)AudioSettings.dspTime - prevTime;
        prevTime = (float)AudioSettings.dspTime;
        float newSamplesPerSecond = _SamplesPerRead * (1f / dt);
        float concurrentSamples = newSamplesPerSecond / 44100;
        _SamplesPerSecond = newSamplesPerSecond;
    }
}
