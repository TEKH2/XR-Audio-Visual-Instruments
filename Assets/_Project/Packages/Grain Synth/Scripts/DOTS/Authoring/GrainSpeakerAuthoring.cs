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

    // TODO -- Tidy up
    #region -------------------------- VARIABLES  
    EntityManager _EntityManager;
    Entity _Entity;
    GrainSpeakerComponent _SpeakerComponenet;

    MeshRenderer _MeshRenderer;

    GrainSynth _GrainSynth;
    public int _SpeakerIndex = int.MaxValue;

    private int _SampleRate;

    List<GrainPlaybackData> _ActiveGrainPlaybackData = new List<GrainPlaybackData>();
    GrainPlaybackData[] _GrainPlaybackDataArray;   
    int _PooledGrainCount = 0;
    int ActiveGrainPlaybackDataCount { get { return _GrainPlaybackDataArray.Length - _PooledGrainCount; } }

    int _GrainPlaybackDataToPool = 100;
    int _DebugTotalGrainsCreated = 0;

    //DebugGUI_Granulator _DebugGUI;
    int _PrevStartSample = 0;
    bool _Initialized = false;

    AudioSource _AudioSource;
    float _VolumeSmoothing = 4;

    float _TargetVolume = 0;

    bool _ConnectedToEmitter = false;


    public bool _Registered = false;

    [HideInInspector]
    public bool _StaticallyPairedToEmitter = false;

    public bool _DebugLog = false;
    #endregion


    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        //---   CREATE ENTITY, REGISTER AND NAME
        _Entity = entity;
        _GrainSynth = FindObjectOfType<GrainSynth>();
        _GrainSynth.RegisterSpeaker(this);
        dstManager.SetName(_Entity, "Speaker " + _SpeakerIndex);


        //---   ADD SPEAKER COMP
        dstManager.AddComponentData(entity, new GrainSpeakerComponent { _SpeakerIndex = _SpeakerIndex });


        //---   ADD POOLING COMP IF NOT STATICALLY PAIRED TO EMITTER
        if(!_StaticallyPairedToEmitter)
            dstManager.AddComponentData(entity, new PooledObjectComponent { _State = PooledObjectState.Pooled });


        //---   CREATE GRAIN PLAYBACK DATA ARRAY - CURRENT MAXIMUM LENGTH SET TO ONE SECOND OF SAMPLES (_SAMPLERATE)      
        _GrainPlaybackDataArray = new GrainPlaybackData[_GrainPlaybackDataToPool];

        for (int i = 0; i < _GrainPlaybackDataToPool; i++)
            _GrainPlaybackDataArray[i] = CreateNewGrain();

        _PooledGrainCount = _GrainPlaybackDataArray.Length;



        //---   ADD RING BUFFER COMP AND INIT
        dstManager.AddComponentData(entity, new RingBufferFiller { _StartIndex = 0, _SampleCount = 0 });
        dstManager.AddBuffer<AudioRingBufferElement>(entity);
        DynamicBuffer<AudioRingBufferElement> buffer = _EntityManager.GetBuffer<AudioRingBufferElement>(entity);

        for (int i = 0; i < AudioSettings.outputSampleRate; i++)        
            buffer.Add(new AudioRingBufferElement { Value = 0 });
        

        _Initialized = true;
    }

    public int GetRegisterAndGetIndex()
    {
        _GrainSynth.RegisterSpeaker(this);
        return _SpeakerIndex;
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


        //---   UPDATE TRANSLATION COMPONENT
        transform.position = _EntityManager.GetComponentData<Translation>(_Entity).Value;


        #region ---   CHECK PAIRING TO EMITTERS       
        if (!_StaticallyPairedToEmitter)
        {
            //---   CLEAR PLAYBACK DATA IF NOT CONNECTED TOO EMITTERS
            _SpeakerComponenet = _EntityManager.GetComponentData<GrainSpeakerComponent>(_Entity);
            bool isCurrentlyConnected = _EntityManager.GetComponentData<PooledObjectComponent>(_Entity)._State == PooledObjectState.Active;


            //---   IF PREVIOUSLY CONNCETED AND NOW DISCONNECTED
            if (_ConnectedToEmitter && !isCurrentlyConnected)
            {               
                for (int i = 0; i < _GrainPlaybackDataArray.Length; i++)
                {
                    _GrainPlaybackDataArray[i]._Pooled = true;
                    _GrainPlaybackDataArray[i]._IsPlaying = false;
                }                    
               
                _PooledGrainCount = _GrainPlaybackDataArray.Length;                
            }


            //---   SET MESH VISIBILITY AND VOLUME BASED ON CONNECTION TO EMITTER
            _TargetVolume = isCurrentlyConnected ? 1 : 0;
            _AudioSource.volume = Mathf.Lerp(_AudioSource.volume, _TargetVolume, Time.deltaTime * _VolumeSmoothing);
            if (_TargetVolume == 0 && _AudioSource.volume < .005f)
                _AudioSource.volume = 0;

            _MeshRenderer.enabled = isCurrentlyConnected;
            _ConnectedToEmitter = isCurrentlyConnected;
        }
        #endregion
    }


    #region GRAIN PLAYBACK DATA POOLING
    //---   FINDS A GRAIN PLAYBACK DATA THAT IS POOLED
    public GrainPlaybackData GetGrainPlaybackDataFromPool()
    {
        print("GetGrainPlaybackDataFromPool - _Pooled data count: " + _PooledGrainCount + "  Total data count: " + _GrainPlaybackDataArray.Length);
        // If pooled grains exist then find the first one
        if (_PooledGrainCount > 0)
        {
            for (int i = 0; i < _GrainPlaybackDataArray.Length; i++)
            {
                if (_GrainPlaybackDataArray[i]._Pooled)
                {
                    _GrainPlaybackDataArray[i]._Pooled = false;
                    return _GrainPlaybackDataArray[i];
                }
            }
        }
        else
        {
            return null;
        }        

        return null;
    }

    //---   ADDS GRAIN PLAYBACK DATA BACK TO THE POOL
    public void AddGrainPlaybackDataToPool(GrainPlaybackData playbackData)
    {
        if (!_Initialized)
            return;
       
        playbackData._Pooled = true;        
        _PooledGrainCount++;

        _PrevStartSample = playbackData._DSPStartTime;

        // Fire event if hooked up
        OnGrainEmitted?.Invoke(playbackData, _GrainSynth._CurrentDSPSample);

        //if (_DebugLog)
        //{
        //int samplesBetweenGrains = playbackData._DSPStartTime - prevStartSample;
        //float msBetweenGrains = (samplesBetweenGrains / (float)AudioSettings.outputSampleRate) * 1000;
        //float DSPSampleDiff = playbackData._DSPStartTime - _GrainSynth._CurrentDSPSample;
        //int DSPMSDiff = (int)((DSPSampleDiff / (float)AudioSettings.outputSampleRate) * 1000);
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

       

        //print("Grain added. Sample 1000: " + playbackData._GrainSamples[1000] + "  playbackData duration: " + playbackData._GrainSamples.Length);
    }
    #endregion



    void ReportGrainsDebug(string action)
    {
        //if (!_DebugLog)
        //    return;
                
        print(name + "---------------------------  " + action + "       A: " + ActiveGrainPlaybackDataCount + "  P: " + _PooledGrainCount + "      T: " + _DebugTotalGrainsCreated);        
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

        // ----------------------DEBUG
        float dt = (float)AudioSettings.dspTime - prevTime;
        prevTime = (float)AudioSettings.dspTime;
        float newSamplesPerSecond = _SamplesPerRead * (1f / dt);
        float concurrentSamples = newSamplesPerSecond / _SampleRate;
        _SamplesPerSecond = newSamplesPerSecond;
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
