using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

using UnityEngine.Profiling;


public class GrainPlaybackData
{
    public bool _IsPlaying = false;
    public float[] _GrainSamples;
    public int _PlayheadIndex = 0;
    public int _SizeInSamples = -1;

    // Used for visualizing the grain
    public float _PlayheadPos;

    // The DSP sample that the grain starts at
    public int _DSPStartTime;

    public bool _Pooled = true;


    public GrainPlaybackData(int maxGrainSize)
    {
        // Instantiate the playback data with max grain samples
        _GrainSamples = new float[maxGrainSize];
    }
}

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(ConvertToEntity))]
public class GrainSpeakerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public delegate void GrainEmitted(GrainPlaybackData data, int currentDSPSample);
    public event GrainEmitted OnGrainEmitted;

    // TODO -- Tidy up
    #region -------------------------- VARIABLES  
    EntityManager _EntityManager;
    Entity _SpeakerEntity;
    GrainSpeakerComponent _SpeakerComponenet;

    MeshRenderer _MeshRenderer;

    public GrainSynth _GrainSynth;
    public int _SpeakerIndex = int.MaxValue;

    private int _SampleRate;

    GrainPlaybackData[] _GrainPlaybackDataArray;   
    public int _PooledGrainCount = 0;
    int ActiveGrainPlaybackDataCount { get { return _GrainPlaybackDataArray.Length - _PooledGrainCount; } }

    int _GrainPlaybackDataToPool = 100;
    int _DebugTotalGrainsCreated = 0;

    //DebugGUI_Granulator _DebugGUI;
    int _PrevStartSample = 0;
    bool _Initialized = false;

    AudioSource _AudioSource;
    float _VolumeSmoothing = 4;
    float _TargetVolume = 0;

    public bool _Registered = false;

    [HideInInspector]
    public bool StaticallyPairedToEmitter { get { return _StaticallyPairedEmitters.Count > 0; } }
    public List<GameObject> _StaticallyPairedEmitters = new List<GameObject>();

    // TODO - Change to state?
    bool _ConnectedToEmitter = false;
    public bool _DebugLog = false;
    #endregion

    public void Awake()
    {
        GetComponent<ConvertToEntity>().ConversionMode = ConvertToEntity.Mode.ConvertAndInjectGameObject;
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _MeshRenderer = gameObject.GetComponentInChildren<MeshRenderer>();
        _AudioSource = gameObject.GetComponent<AudioSource>();
        _SampleRate = AudioSettings.outputSampleRate;

        print("---   CONVERT GRAIN SPEAKER...");
        //---   CREATE ENTITY, REGISTER AND NAME
        _SpeakerEntity = entity;
        _GrainSynth = FindObjectOfType<GrainSynth>();
        _GrainSynth.RegisterSpeaker(this);

#if UNITY_EDITOR
        dstManager.SetName(_SpeakerEntity, "Speaker " + _SpeakerIndex);
#endif


        //---   ADD SPEAKER COMP
        dstManager.AddComponentData(entity, new GrainSpeakerComponent { _SpeakerIndex = _SpeakerIndex });


        //---   ADD POOLING COMP IF NOT STATICALLY PAIRED TO EMITTER
        if (!StaticallyPairedToEmitter)
        {
            dstManager.AddComponentData(entity, new PooledObjectComponent { _State = PooledObjectState.Pooled });
            print("- Adding pooled component for non statically paired speaker");
        }

        //---   CREATE GRAIN PLAYBACK DATA ARRAY - CURRENT MAXIMUM LENGTH SET TO ONE SECOND OF SAMPLES (_SAMPLERATE)      
        _GrainPlaybackDataArray = new GrainPlaybackData[_GrainPlaybackDataToPool];

        for (int i = 0; i < _GrainPlaybackDataToPool; i++)
            _GrainPlaybackDataArray[i] = CreateNewGrain();

        _PooledGrainCount = _GrainPlaybackDataArray.Length;

        _AudioSource.rolloffMode = AudioRolloffMode.Custom;
        _AudioSource.maxDistance = 500;
        
        //Debug.Log(_PooledGrainCount);

        ////---   ADD RING BUFFER COMP AND INIT
        //dstManager.AddComponentData(entity, new RingBufferFiller { _StartIndex = 0, _SampleCount = 0 });
        //dstManager.AddBuffer<AudioRingBufferElement>(entity);
        //DynamicBuffer<AudioRingBufferElement> buffer = _EntityManager.GetBuffer<AudioRingBufferElement>(entity);

        //for (int i = 0; i < AudioSettings.outputSampleRate; i++)        
        //    buffer.Add(new AudioRingBufferElement { Value = 0 });        

        _Initialized = true;
    }

    public void AddPairedEmitter(GameObject emitterGO)
    {
        _StaticallyPairedEmitters.Add(emitterGO);
    }

  
    public int GetRegisterAndGetIndex()
    {     
        GrainSynth.Instance.RegisterSpeaker(this);
        return _SpeakerIndex;
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
        if (!StaticallyPairedToEmitter)
        {
            transform.position = _EntityManager.GetComponentData<Translation>(_SpeakerEntity).Value;
        }

        //---   Pool playback data object after its previous grain has reached the end of its playhead
        for (int i = 0; i < _GrainPlaybackDataArray.Length; i++)
        {
            if (!_GrainPlaybackDataArray[i]._IsPlaying && _GrainPlaybackDataArray[i]._PlayheadIndex >= _GrainPlaybackDataArray[i]._SizeInSamples && _GrainPlaybackDataArray[i]._Pooled == false)
            {
                _GrainPlaybackDataArray[i]._Pooled = true;
                _PooledGrainCount++;
            }
        }

        #region ---   CHECK PAIRING TO EMITTERS       
        if (!StaticallyPairedToEmitter)
        {
            //---   CLEAR PLAYBACK DATA IF NOT CONNECTED TOO EMITTERS
            _SpeakerComponenet = _EntityManager.GetComponentData<GrainSpeakerComponent>(_SpeakerEntity);
            bool isCurrentlyConnected = _EntityManager.GetComponentData<PooledObjectComponent>(_SpeakerEntity)._State == PooledObjectState.Active;


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

            if (_MeshRenderer != null)
                _MeshRenderer.enabled = isCurrentlyConnected;
            _ConnectedToEmitter = isCurrentlyConnected;
        }

        //if (_PooledGrainCount == _GrainPlaybackDataToPool)
        //    Debug.Log("Ignoring Speaker " + _SpeakerIndex);
        #endregion
    }


    #region GRAIN PLAYBACK DATA POOLING
    //---   FINDS A GRAIN PLAYBACK DATA THAT IS POOLED
    public GrainPlaybackData GetGrainPlaybackDataFromPool()
    {
        if (!_Initialized)
            return null;

        // If pooled grains exist then find the first one
        if (_PooledGrainCount > 0)
        {
            for (int i = 0; i < _GrainPlaybackDataArray.Length; i++)
            {
                if (_GrainPlaybackDataArray[i]._Pooled)
                {
                    _GrainPlaybackDataArray[i]._Pooled = false;
                    _PooledGrainCount--;
                    //print("GetGrainPlaybackDataFromPool - Returnign grain at index: " + i);
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

        

        //---   DEBUG
        float cadence = (playbackData._DSPStartTime - _PrevStartSample) / (float)AudioSettings.outputSampleRate;
        float duration = playbackData._SizeInSamples / (float)AudioSettings.outputSampleRate;
        float DSPStartOffset = playbackData._DSPStartTime - _GrainSynth._CurrentDSPSample;
        //print("Current DSP offset: " + DSPStartOffset + "  duration  : " + duration + "  Cadence: " + cadence);


        //Debug.Log("ADDING GRAIN AT DSP OFFSET: " + DSPStartOffset);


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

    private void OnDestroy()
    {
        DestroyEntity();
    }

    public void DestroyEntity()
    {
        print("Speaker DestroyEntity");
        if (_SpeakerEntity != null)
            _EntityManager.DestroyEntity(_SpeakerEntity);
    }

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
        // BRAD! Fixed pool count and added condition to ignore DSP processing entirely if pooling has no active grains
        if (!_Initialized || _PooledGrainCount == _GrainPlaybackDataToPool)
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

                //---   GRAIN DSP START TIME HAS BEEN REACHED
                if (_CurrentDSPSample >= grainData._DSPStartTime)
                {
                    int diff = (_CurrentDSPSample - grainData._PlayheadIndex) - grainData._DSPStartTime;
                    
                    if (grainData._PlayheadIndex >= grainData._SizeInSamples)
                    {
                        grainData._IsPlaying = false;
                    }
                    else
                    {
                        for (int chan = 0; chan < channels; chan++)
                        {
                            _SamplesPerRead++;
                            data[dataIndex + chan] += grainData._GrainSamples[grainData._PlayheadIndex];
                        }
                        grainData._PlayheadIndex++;
                    }
                }
            }           
        }


        //--- DEBUG
        //int playingCount = 0;
        //for (int i = 0; i < _GrainPlaybackDataArray.Length; i++)
        //{
        //    if (!_GrainPlaybackDataArray[i]._IsPlaying)
        //        continue;

        //    playingCount++;

           
        //}

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
