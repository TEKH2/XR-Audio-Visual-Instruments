using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

using UnityEngine.Profiling;


public class GrainPlaybackData
{
    public bool _IsPlaying = true;
    public float[] _GrainSamples;
    public float[] _TempSampleBuffer;
    public int _PlayheadIndex = 0;
    public int _SizeInSamples;

    // Used for visualizing the grain
    public float _PlayheadPos;

    // The DSP sample that the grain starts at
    public int _DSPStartTime;

    public bool _Pooled = true;


    public GrainPlaybackData(int maxGrainSize)
    {
        // instantiate the grain samples at a given maximum length
        _GrainSamples = new float[maxGrainSize];
        _TempSampleBuffer = new float[maxGrainSize];
    }
}

public class GrainSpeakerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public delegate void GrainEmitted(GrainPlaybackData data, int currentDSPSample);
    public event GrainEmitted OnGrainEmitted;

    #region -------------------------- VARIABLES
    EntityManager _EntityManager;
    Entity _Entity;
    GrainSpeakerComponent _SpeakerComponenet;

    MeshRenderer _MeshRenderer;

    GrainSynth _GrainSynth;
    public int _SpeakerIndex = 0;

    private int _SampleRate;

    List<GrainPlaybackData> _ActiveGrainPlaybackData = new List<GrainPlaybackData>();
    List<GrainPlaybackData> _PooledGrainPlaybackData = new List<GrainPlaybackData>();
    GrainPlaybackData[] _GrainPlaybackDataArray;
    int _ActiveGrains = 0;
    int _PooledGrains = 0;

    int _GrainPlaybackDataToPool = 100;
    int _MaxGrainPlaybackDataCount = 200;

    int GrainDataCount { get { return _ActiveGrainPlaybackData.Count + _PooledGrainPlaybackData.Count; } }
    int _DebugTotalGrainsCreated = 0;

    //DebugGUI_Granulator _DebugGUI;
    int prevStartSample = 0;
    bool _Initialized = false;

    AudioSource _AudioSource;
    float _VolumeSmoothing = 4;

    float _TargetVolume = 0;

    bool _ConnectedToEmitter = false;


    public bool _Registered = false;

    [HideInInspector]
    public bool _StaticallyPaired = false;

    public bool _UseSingleGrainPlaybackDataArray = false;

    public bool _DebugLog = false;
    #endregion


    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        _Entity = entity;
        _GrainSynth = FindObjectOfType<GrainSynth>();
        _GrainSynth.RegisterSpeaker(this);
        dstManager.SetName(_Entity, "Speaker " + _SpeakerIndex);


        // Register the speaker and get the index
        dstManager.AddComponentData(entity, new GrainSpeakerComponent { _SpeakerIndex = _SpeakerIndex });

        // Pool grain playback data - current maximum length set to one second of samples (_SampleRate)
        if (_UseSingleGrainPlaybackDataArray)
        {
            _GrainPlaybackDataArray = new GrainPlaybackData[_GrainPlaybackDataToPool];

            for (int i = 0; i < _GrainPlaybackDataToPool; i++)
                _GrainPlaybackDataArray[i] = CreateNewGrain();

            _PooledGrains = _GrainPlaybackDataArray.Length;
        }
        else
        {
            for (int i = 0; i < _GrainPlaybackDataToPool; i++)
            {
                _PooledGrainPlaybackData.Add(CreateNewGrain());
            }
        }


        // Add pooling componenet
        dstManager.AddComponentData(entity, new PooledObjectComponent { _State = PooledObjectState.Pooled });

        // Add audio buffer component
        dstManager.AddComponentData(entity, new RollingBufferFiller { _StartIndex = 0, _SampleCount = 400 });
        dstManager.AddBuffer<AudioSampleBufferElement>(entity);

        //ReportGrainsDebug("Pooling");

        _Initialized = true;
    }

    public void Start()
    {
        _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        _MeshRenderer = gameObject.GetComponentInChildren<MeshRenderer>();
        _AudioSource = gameObject.GetComponent<AudioSource>();

        _SampleRate = AudioSettings.outputSampleRate;      
    }

    GrainPlaybackData CreateNewGrain()
    {
        _DebugTotalGrainsCreated++;
        return new GrainPlaybackData(_SampleRate);
    }

    public void Update()
    {
        if (!_Initialized)
            return;

        if (_DebugLog)
            ReportGrainsDebug("");


        //----     Update translation component
        transform.position = _EntityManager.GetComponentData<Translation>(_Entity).Value;


        //----     Check pairing to emitters
        if (!_StaticallyPaired)
        {
            //--   Clear playback data if not connected too emitters
            _SpeakerComponenet = _EntityManager.GetComponentData<GrainSpeakerComponent>(_Entity);
            bool isCurrentlyConnected = _EntityManager.GetComponentData<PooledObjectComponent>(_Entity)._State == PooledObjectState.Active;

            //--   If previously connceted and now disconnected
            if (_ConnectedToEmitter && !isCurrentlyConnected)
            {
                if(_UseSingleGrainPlaybackDataArray)
                {
                    for (int i = 0; i < _GrainPlaybackDataArray.Length; i++)
                    {
                        _GrainPlaybackDataArray[i]._Pooled = true;
                        _GrainPlaybackDataArray[i]._IsPlaying = false;
                    }
                    
                    _ActiveGrains = 0;
                    _PooledGrains = _GrainPlaybackDataArray.Length;
                }
                else
                {
                    // ----    Move all active grain playback data to the pool
                    for (int i = _ActiveGrainPlaybackData.Count - 1; i >= 0; i--)
                    {
                        _PooledGrainPlaybackData.Add(_ActiveGrainPlaybackData[i]);
                    }
                    _ActiveGrainPlaybackData.Clear();
                }
            }


            //Profiler.BeginSample("here2");
            // Set mesh visibility and volume based on connection to emitter
            _TargetVolume = isCurrentlyConnected ? 1 : 0;
            _AudioSource.volume = Mathf.Lerp(_AudioSource.volume, _TargetVolume, Time.deltaTime * _VolumeSmoothing);
            if (_TargetVolume == 0 && _AudioSource.volume < .005f)
                _AudioSource.volume = 0;

            _MeshRenderer.enabled = isCurrentlyConnected;            

            _ConnectedToEmitter = isCurrentlyConnected;

            //Profiler.EndSample();
        }
    }

    public GrainPlaybackData GetGrainPlaybackDataFromPool()
    {
        //if (_DebugLog)
        //    print("Active: " + _ActiveGrainPlaybackData.Count + "   Pooled: " + _PooledGrainPlaybackData.Count);

        if (_UseSingleGrainPlaybackDataArray)
        {
            // If pooled grains exist then find the first one
            if (_PooledGrains > 0)
            {
                for (int i = 0; i < _GrainPlaybackDataArray.Length; i++)
                {
                    if (_GrainPlaybackDataArray[i]._Pooled)
                    {
                        return _GrainPlaybackDataArray[i];
                    }
                }
            }
            else
            {
                //ReportGrainsDebug("Creating new grain");
                return null;
            }
        }
        else
        {
            if (_PooledGrainPlaybackData.Count >= 1)
            {
                GrainPlaybackData grainPlaybackData = _PooledGrainPlaybackData[0];

                return grainPlaybackData;
            }
            else if (GrainDataCount < _MaxGrainPlaybackDataCount)
            {
                //ReportGrainsDebug("Creating new grain");
                GrainPlaybackData grainPlaybackData = CreateNewGrain();
                return grainPlaybackData;
            }
            else
            {
                print(name + "------  Audio output already using max grains. " + GrainDataCount + "/" + _MaxGrainPlaybackDataCount);
                return null;
            }
        }

        return null;
    }

    public void AddGrainPlaybackData(GrainPlaybackData playbackData)
    {
        if (!_Initialized)
            return;

        if(_UseSingleGrainPlaybackDataArray)
        {
            playbackData._Pooled = false;
            _ActiveGrains++;
            _PooledGrains--;

            //print("Adding grain too speaker: " + _SpeakerIndex);

            //ReportGrainsDebug("Adding grain playback data");
        }
        else
        {
            _ActiveGrainPlaybackData.Add(playbackData);
            _PooledGrainPlaybackData.Remove(playbackData);
            //ReportGrainsDebug("Removing pooled data");
        }
           

        int samplesBetweenGrains = playbackData._DSPStartTime - prevStartSample;
        float msBetweenGrains = (samplesBetweenGrains / (float)AudioSettings.outputSampleRate) * 1000;
        float DSPSampleDiff = playbackData._DSPStartTime - _GrainSynth._CurrentDSPSample;
        int DSPMSDiff = (int)((DSPSampleDiff / (float)AudioSettings.outputSampleRate) * 1000);

        // Fire event if hooked up
        OnGrainEmitted?.Invoke(playbackData, _GrainSynth._CurrentDSPSample);

        //if (_DebugLog)
        //{
        //    print
        //    (
        //        "Grain added. Start sample: " + playbackData._DSPStartTime +
        //        " Cadence samples: " + samplesBetweenGrains +
        //        " Cadence m/s:   " + msBetweenGrains +
        //        " DSP sample diff:   " + DSPSampleDiff +
        //        " DSP m/s diff:   " + DSPMSDiff
        //    );
        //}

        //_DebugGUI.LogLatency(DSPMSDiff);

        prevStartSample = playbackData._DSPStartTime;

        //print("Grain added. Sample 1000: " + playbackData._GrainSamples[1000] + "  playbackData duration: " + playbackData._GrainSamples.Length);
    }

    void ReportGrainsDebug(string action)
    {
        //if (!_DebugLog)
        //    return;

        if (_UseSingleGrainPlaybackDataArray)
        {
            print(name + "---------------------------  " + action + "       A: " + _ActiveGrains + "  P: " + _PooledGrains + "      T: " + _DebugTotalGrainsCreated + " List Count:" + (_ActiveGrains + _PooledGrains));
        }
        else
        {
            print(name + "---------------------------  " + action + "       A: " + _ActiveGrainPlaybackData.Count + "  P: " + _PooledGrainPlaybackData.Count + "      T: " + _DebugTotalGrainsCreated + " List Count:" + GrainDataCount);
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

    int _CurrentDSPSample;

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!_Initialized)
            return;

        _SamplesPerRead = 0;
        _CurrentDSPSample = _GrainSynth._CurrentDSPSample;

        // For length of audio buffer, populate with grain samples, maintaining index over successive buffers
        for (int dataIndex = 0; dataIndex < data.Length; dataIndex += channels)
        {
            if (_UseSingleGrainPlaybackDataArray)
            {
                for (int i = 0; i < _GrainPlaybackDataArray.Length; i++)
                {
                    if (!_GrainPlaybackDataArray[i]._IsPlaying)
                        continue;

                    GrainPlaybackData grainData = _GrainPlaybackDataArray[i];
                    //print("playing grain " + i);

                    // If the grain's DSP start time has been reached
                    if (_CurrentDSPSample >= grainData._DSPStartTime)
                    {
                        if (grainData._PlayheadIndex >= grainData._SizeInSamples)
                        {
                            grainData._IsPlaying = false;
                        }
                        else
                        {
                            _SamplesPerRead++;
                            data[dataIndex] += grainData._GrainSamples[grainData._PlayheadIndex];
                            grainData._PlayheadIndex++;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < _ActiveGrainPlaybackData.Count; i++)
                {
                    GrainPlaybackData grainData = _ActiveGrainPlaybackData[i];

                    if (grainData == null)
                        continue;

                    // If the grain's DSP start time has been reached
                    if (_GrainSynth._CurrentDSPSample >= grainData._DSPStartTime)
                    {
                        if (grainData._PlayheadIndex >= grainData._SizeInSamples)
                        {
                            grainData._IsPlaying = false;
                        }
                        else
                        {
                            _SamplesPerRead++;
                            data[dataIndex] += grainData._GrainSamples[grainData._PlayheadIndex];
                            grainData._PlayheadIndex++;
                        }
                    }
                }
            }
        }

        //if(!_UseSingleGrainPlaybackDataArray)
            UpdatePoolingLists();


        // ----------------------DEBUG
        float dt = (float)AudioSettings.dspTime - prevTime;
        prevTime = (float)AudioSettings.dspTime;
        float newSamplesPerSecond = _SamplesPerRead * (1f / dt);
        float concurrentSamples = newSamplesPerSecond / _SampleRate;
        _SamplesPerSecond = newSamplesPerSecond;
    }

    void UpdatePoolingLists()
    {
        if (_UseSingleGrainPlaybackDataArray)
        {
            for (int i = 0; i < _GrainPlaybackDataArray.Length; i++)
            {
                // If it isn't playing and it isn't pooled, then return to pool
                if (!_GrainPlaybackDataArray[i]._IsPlaying && !_GrainPlaybackDataArray[i]._Pooled)
                {
                    _GrainPlaybackDataArray[i]._Pooled = true;
                    _PooledGrains++;
                    _ActiveGrains--;
                }
            }

            //if (_DebugLog)
            //    ReportGrainsDebug("Updating pooling list");
        }
        else
        {
            // Removed finished playback data from pool
            for (int i = _ActiveGrainPlaybackData.Count - 1; i >= 0; i--)
            {
                if (_ActiveGrainPlaybackData[i] == null)
                    continue;

                if (!_ActiveGrainPlaybackData[i]._IsPlaying)
                {
                    //if (_DebugLog)                    
                    //    ReportGrainsDebug("Adding new grain");                    

                    // Add to pool
                    _PooledGrainPlaybackData.Add(_ActiveGrainPlaybackData[i]);
                    // Remove from active pist
                    _ActiveGrainPlaybackData.Remove(_ActiveGrainPlaybackData[i]);
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (_ConnectedToEmitter)
        {
            if(_SpeakerIndex == 0)
                Gizmos.color = Color.blue;
            else
                Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(transform.position, _GrainSynth._EmitterToSpeakerAttachRadius);
        }
    }
}
