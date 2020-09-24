using Unity.Burst;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Profiling;
using System;
using System.Linq;
using Unity.Entities.UniversalDelegates;
using Unity.Entities.CodeGeneratedJobForEach;
using Random = UnityEngine.Random;

public class GrainSynth :  MonoBehaviour
{
    public static GrainSynth Instance;

    // Entity manager ref for creating and updating entities
    EntityManager _EntityManager;

    EntityQuery _GrainQuery;
    Entity _DSPTimerEntity;
    Entity _SpeakerManagerEntity;

    AudioListener _Listener;

    public AudioClip[] _AudioClips;

    public GrainSpeakerAuthoring _SpeakerPrefab;
    public List<GrainSpeakerAuthoring> _GrainSpeakers = new List<GrainSpeakerAuthoring>();
    public int _MaxGrainSpeakers = 5;

    [Range(0, 100)]
    public float _GrainQueueInMS = 50;
    int _SampleRate;
    public int _GrainQueueDurationInSamples { get { return (int)(_GrainQueueInMS * _SampleRate * .001f); } }

    public int _CurrentDSPSample;

    public float _EmitterToListenerActivationRange = 3;
    public float _EmitterToSpeakerAttachRadius = 1;


    private void Awake()
    {
        Instance = this;
    }

    public void Start()
    {
        _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _SampleRate = AudioSettings.outputSampleRate;

        for (int i = 0; i < _MaxGrainSpeakers; i++)
        {
            CreateSpeaker(transform.position);
        }
        

        _DSPTimerEntity = _EntityManager.CreateEntity();
        _EntityManager.AddComponentData(_DSPTimerEntity, new DSPTimerComponent { _CurrentDSPSample = _CurrentDSPSample, _GrainQueueDuration = (int)(AudioSettings.outputSampleRate * _GrainQueueInMS) });

        _GrainQuery = _EntityManager.CreateEntityQuery(typeof(GrainProcessor));


        // ------------------------------------------------ CREATE SPEAKER MANAGER
        _Listener = FindObjectOfType<AudioListener>();
        _SpeakerManagerEntity = _EntityManager.CreateEntity();
        DynamicBuffer<GrainSpeakerBufferElement> activeSpeakerBuffer = _EntityManager.AddBuffer<GrainSpeakerBufferElement>(_SpeakerManagerEntity);
        _EntityManager.AddComponentData(_SpeakerManagerEntity, new SpeakerManagerComponent
        {
            _ListenerPos = _Listener.transform.position,
            _EmitterToListenerActivationRange = _EmitterToListenerActivationRange,
            _EmitterToSpeakerAttachRadius = _EmitterToSpeakerAttachRadius
        });

        // -------------------------------------------------   CREATE AUDIO SOURCE BLOB ASSETS AND ASSIGN TO AudioClipDataComponent ENTITIES
        for (int i = 0; i < _AudioClips.Length; i++)
        {
            Entity audioClipDataEntity = _EntityManager.CreateEntity();

            int clipChannels = _AudioClips[i].channels;

            float[] clipData = new float[_AudioClips[i].samples];

            _AudioClips[i].GetData(clipData, 0);

            using (BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp))
            {
                // ---------------------------------- CREATE BLOB
                ref FloatBlobAsset audioclipBlobAsset = ref blobBuilder.ConstructRoot<FloatBlobAsset>();
                BlobBuilderArray<float> audioclipArray = blobBuilder.Allocate(ref audioclipBlobAsset.array, (clipData.Length / clipChannels));

                for (int s = 0; s < clipData.Length - 1; s += clipChannels)
                {
                    audioclipArray[s / clipChannels] = 0;
                    
                    // MonoSum stereo audio files
                    for (int c = 0; c < clipChannels; c++)
                    {
                        audioclipArray[s / clipChannels] += clipData[s + c];
                    }
                }

                // ---------------------------------- CREATE REFERENCE AND ASSIGN TO ENTITY
                BlobAssetReference<FloatBlobAsset> audioClipBlobAssetRef = blobBuilder.CreateBlobAssetReference<FloatBlobAsset>(Allocator.Persistent);
                _EntityManager.AddComponentData(audioClipDataEntity, new AudioClipDataComponent { _ClipDataBlobAsset = audioClipBlobAssetRef, _ClipIndex = i });
            }
        }

        // -------------------------------------------------- CREATE WINDOWING BLOB ASSET
        Entity windowingBlobEntity = _EntityManager.CreateEntity();
        using (BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp))
        {
            // ---------------------------------- CREATE BLOB
            ref FloatBlobAsset windowingBlobAsset = ref blobBuilder.ConstructRoot<FloatBlobAsset>();
            BlobBuilderArray<float> windowArray = blobBuilder.Allocate(ref windowingBlobAsset.array, 512);

            for (int i = 0; i < windowArray.Length; i++)
                windowArray[i] = 0.5f * (1 - Mathf.Cos(2 * Mathf.PI * i / windowArray.Length));

            // ---------------------------------- CREATE REFERENCE AND ASSIGN TO ENTITY
            BlobAssetReference<FloatBlobAsset> windowingBlobAssetRef = blobBuilder.CreateBlobAssetReference<FloatBlobAsset>(Allocator.Persistent);
            _EntityManager.AddComponentData(windowingBlobEntity, new WindowingDataComponent { _WindowingArray = windowingBlobAssetRef });
        }
    }

    private void Update()
    {
        // Update DSP sample
        DSPTimerComponent dspTimer = _EntityManager.GetComponentData<DSPTimerComponent>(_DSPTimerEntity);
        _EntityManager.SetComponentData(_DSPTimerEntity, new DSPTimerComponent { _CurrentDSPSample = _CurrentDSPSample + (int)(Time.deltaTime * AudioSettings.outputSampleRate), _GrainQueueDuration = _GrainQueueDurationInSamples });

        NativeArray<Entity> grainEntities = _GrainQuery.ToEntityArray(Allocator.TempJob);


        DynamicBuffer<GrainSpeakerBufferElement> activeSpeakerBuffer = _EntityManager.GetBuffer<GrainSpeakerBufferElement>(_SpeakerManagerEntity);
        // Update audio listener position
        _EntityManager.SetComponentData(_SpeakerManagerEntity, new SpeakerManagerComponent
        {
            _ListenerPos = _Listener.transform.position,
            _EmitterToListenerActivationRange = _EmitterToListenerActivationRange,
            _EmitterToSpeakerAttachRadius = _EmitterToSpeakerAttachRadius
        });

       
        for (int i = 0; i < grainEntities.Length; i++)
        {           
            GrainProcessor grainProcessor = _EntityManager.GetComponentData<GrainProcessor>(grainEntities[i]);

            if(grainProcessor._SamplePopulated)
            {
                Profiler.BeginSample("Grain update 1");
                GrainPlaybackData playbackData = _GrainSpeakers[0].GetGrainPlaybackDataFromPool();
                Profiler.EndSample();

                if (playbackData == null)
                    break;

                NativeArray<float> samples = _EntityManager.GetBuffer<GrainSampleBufferElement>(grainEntities[i]).Reinterpret<float>().ToNativeArray(Allocator.Temp);

                playbackData._IsPlaying = true;
                playbackData._PlayheadIndex = 0;
                playbackData._SizeInSamples = samples.Length;
                playbackData._DSPStartTime = grainProcessor._DSPSamplePlaybackStart;
                playbackData._PlayheadPos = grainProcessor._PlaybackHeadNormPos;


                int sampleLength = samples.Length;
                for (int s = 0; s < playbackData._GrainSamples.Length; s++)
                {
                    if(s < sampleLength)
                        playbackData._GrainSamples[s] = samples[s];
                    else
                        playbackData._GrainSamples[s] = 0;
                }

                // Destroy entity once we have sapped it of it's samply goodness
                _EntityManager.DestroyEntity(grainEntities[i]);

                _GrainSpeakers[grainProcessor._SpeakerIndex].AddGrainPlaybackData(playbackData);            
            }
        }       

        grainEntities.Dispose();
    }

    public void CreateSpeaker(Vector3 pos)
    {
        GrainSpeakerAuthoring speaker = Instantiate(_SpeakerPrefab, pos, quaternion.identity, transform);    
    }

    public void RegisterSpeaker(GrainSpeakerAuthoring speaker)
    {
        if (_GrainSpeakers.Contains(speaker))
            print("Speaker already regsitered.");

        speaker._SpeakerIndex = _GrainSpeakers.Count;
        speaker._Registered = true;
        speaker.name = "Speaker " + _GrainSpeakers.Count;
        _GrainSpeakers.Add(speaker);
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        for (int dataIndex = 0; dataIndex < data.Length; dataIndex += channels)
        {
            _CurrentDSPSample++;
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;
         
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(_Listener.transform.position, _EmitterToListenerActivationRange);
    }
}

[System.Serializable]
public class AudioClipLibrary
{
    public AudioClip[] _Clips;
    public List<float[]> _ClipsDataArray = new List<float[]>();

    public void Initialize()
    {
        if (_Clips.Length == 0)
            Debug.LogError("No clips in clip library");
        else
            Debug.Log("Initializing clip library.");

        for (int i = 0; i < _Clips.Length; i++)
        {
            AudioClip audioClip = _Clips[i];

            if (audioClip.channels > 1)
            {
                Debug.LogError("Audio clip not mono");
            }

            float[] samples = new float[audioClip.samples];
            _Clips[i].GetData(samples, 0);
            _ClipsDataArray.Add(samples);

            Debug.Log(String.Format("Clip {0}      Samples: {1}        Time length: {2} ", _Clips[i].name, _ClipsDataArray[i].Length, _ClipsDataArray[i].Length / (float)_Clips[i].frequency));
        }


    }
}