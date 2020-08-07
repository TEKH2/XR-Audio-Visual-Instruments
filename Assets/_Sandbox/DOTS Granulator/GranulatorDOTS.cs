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

public class GranulatorDOTS :  MonoBehaviour
{
    EntityManager _EntityManager;
    EntityQuery _GrainQuery;

    Entity _DSPTimerEntity;

    GrainManager _GrainManager;

    public AudioClip[] _AudioClips;
    Entity[] _AudioClipEntities;

    List<Entity> _GrainEntities = new List<Entity>();

    public int _NumEmitters = 1;
    public float _CadenceInMS = 5f;
    public float _DurationInMS = 200f;

  

    public void Start()
    {
        _GrainManager = GrainManager.Instance;
        _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        _DSPTimerEntity = _EntityManager.CreateEntity();
        _EntityManager.AddComponentData(_DSPTimerEntity, new DSPTimerComponent { _CurrentDSPSample = _GrainManager._CurrentDSPSample });

        _GrainQuery = _EntityManager.CreateEntityQuery(typeof(GrainProcessor));


        // -------------------------------------------------   CREATE AUDIO SOURCE BLOB ASSETS AND ASSIGN TO AudioClipDataComponent ENTITIES
        for (int i = 0; i < _AudioClips.Length; i++)
        {
            Entity audioClipDataEntity = _EntityManager.CreateEntity();

            float[] clipData = new float[_AudioClips[i].samples];
            _AudioClips[i].GetData(clipData, 0);

            using (BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp))
            {
                // ---------------------------------- CREATE BLOB
                ref FloatBlobAsset audioclipBlobAsset = ref blobBuilder.ConstructRoot<FloatBlobAsset>();
                BlobBuilderArray<float> audioclipArray = blobBuilder.Allocate(ref audioclipBlobAsset.array, clipData.Length);

                for (int s = 0; s < clipData.Length; s++)
                {
                    audioclipArray[s] = clipData[s];
                }

                // ---------------------------------- CREATE REFERENCE AND ASSIGN TO ENTITY
                BlobAssetReference<FloatBlobAsset> audioClipBlobAssetRef = blobBuilder.CreateBlobAssetReference<FloatBlobAsset>(Allocator.Persistent);
                _EntityManager.AddComponentData(audioClipDataEntity, new AudioClipDataComponent { _ClipDataBlobAsset = audioClipBlobAssetRef, _ClipIndex = i });
            }
        }

        // -------------------------------------------------   CREATE EMITTER
        for (int i = 0; i < _NumEmitters; i++)
        {
            Entity emitterEntity = _EntityManager.CreateEntity();
            _EntityManager.AddComponentData(emitterEntity, new EmitterComponent { _Timer = 0, _Cadence = _CadenceInMS * .001f, _DurationInSamples = (int)(_DurationInMS * .001f * 44100) });
        }
    }



    private void Update()
    {
        // Update DSP sample
        DSPTimerComponent dspTimer = _EntityManager.GetComponentData<DSPTimerComponent>(_DSPTimerEntity);
        _EntityManager.SetComponentData(_DSPTimerEntity, new DSPTimerComponent { _CurrentDSPSample = _GrainManager._CurrentDSPSample });

        NativeArray<Entity> grainEntities = _GrainQuery.ToEntityArray(Allocator.TempJob);

        if (!_GrainManager._AllSpeakers[0].gameObject.activeSelf) _GrainManager._AllSpeakers[0].gameObject.SetActive(true);

        for (int i = grainEntities.Length-1; i > 0; i--)
        {
            GrainProcessor grainProcessor = _EntityManager.GetComponentData<GrainProcessor>(grainEntities[i]);

            if(grainProcessor._Populated)
            {
                GrainPlaybackData playbackData = _GrainManager._AllSpeakers[0].GetGrainPlaybackDataFromPool();

                if (playbackData == null)
                    break;

                NativeArray<float> samples = _EntityManager.GetBuffer<FloatBufferElement>(grainEntities[i]).Reinterpret<float>().ToNativeArray(Allocator.Temp);

                playbackData._IsPlaying = true;
                playbackData._PlaybackIndex = 0;
                playbackData._PlaybackSampleCount = samples.Length;
                playbackData._DSPStartIndex = grainProcessor._DSPSamplePlaybackStart;

                //print("Current dsp time: " + _GrainManager._CurrentDSPSample + "   grain start dsp index: " + playbackData._DSPStartIndex);

                Array.ConstrainedCopy(samples.ToArray(), 0, playbackData._GrainSamples, 0, samples.Length);

                // Destroy entity once we have sapped it of it's samply goodness
                _EntityManager.DestroyEntity(grainEntities[i]);

                _GrainManager._AllSpeakers[0].AddGrainPlaybackData(playbackData);

                //print("Copying sample data over: " + samples[1000] + "     " + playbackData._GrainSamples[1000]);
                //print(".....Copying sample data over: " + samples[1500] + "     " + playbackData._GrainSamples[1500]);
            }
        }

        grainEntities.Dispose();
    }
}


public class GranulatorSystem : SystemBase
{
    // Command buffer for removing tween componants once they are completed
    private EndSimulationEntityCommandBufferSystem _CommandBufferSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        _CommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    bool _UseBurst = false;
    protected override void OnUpdate()
    {
        // Acquire an ECB and convert it to a concurrent one to be able to use it from a parallel job.
        EntityCommandBuffer.ParallelWriter entityCommandBuffer = _CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        // Get all audio clip data componenets
        NativeArray<AudioClipDataComponent> audioClipData = GetEntityQuery(typeof(AudioClipDataComponent)).ToComponentDataArray<AudioClipDataComponent>(Allocator.TempJob);

        DSPTimerComponent dspTimer = GetSingleton<DSPTimerComponent>();

        float dt = Time.DeltaTime;

        Entities.ForEach
        (
            (int entityInQueryIndex, ref EmitterComponent emitter) =>
            {
                emitter._Timer += dt;
                // If timer is up
                if (emitter._Timer >= emitter._Cadence)
                {
                    // Emit grain
                    emitter._Timer -= emitter._Cadence;

                    // Create a new grain processor entity
                    Entity e = entityCommandBuffer.CreateEntity(entityInQueryIndex);

                    entityCommandBuffer.AddComponent(entityInQueryIndex, e, new GrainProcessor
                    {
                        _AudioClipDataComponent = audioClipData[0],

                        _PlaybackHeadSamplePos = .1f,
                        _DurationInSamples = emitter._DurationInSamples,

                        _Pitch = -3.1f,
                        _Volume = 2,

                        _SpeakerIndex = 0,
                        _DSPSamplePlaybackStart = dspTimer._CurrentDSPSample + 100,
                        _Populated = false                     
                    });

                    entityCommandBuffer.AddBuffer<FloatBufferElement>(entityInQueryIndex, e);
                }
            }
        ).WithDisposeOnCompletion(audioClipData).ScheduleParallel();


        Entities.ForEach
        (
          (int entityInQueryIndex, DynamicBuffer<FloatBufferElement> sampleOutputBuffer, ref GrainProcessor grain) =>
          {
              if (!grain._Populated)
              {
                  float sourceIndex = grain._PlaybackHeadSamplePos;
                  float increment = grain._Pitch;

                  for (int i = 0; i < grain._DurationInSamples; i++)
                  {
                      // PING PONG
                      if (sourceIndex + increment < 0 || sourceIndex + increment > grain._AudioClipDataComponent._ClipDataBlobAsset.Value.array.Length - 1)
                      {
                          increment = increment * -1f;
                          sourceIndex -= 1;
                      }

                      // PITCHING - Interpolate sample if not integer to create 
                      sourceIndex += increment;
                      float sourceIndexRemainder = sourceIndex % 1;
                      float sourceValue;
                      if (sourceIndexRemainder != 0)
                      {
                          sourceValue = math.lerp(
                              grain._AudioClipDataComponent._ClipDataBlobAsset.Value.array[(int)sourceIndex],
                              grain._AudioClipDataComponent._ClipDataBlobAsset.Value.array[(int)sourceIndex + 1],
                              sourceIndexRemainder);
                      }
                      else
                      {
                          sourceValue = grain._AudioClipDataComponent._ClipDataBlobAsset.Value.array[(int)sourceIndex];
                      }

                      sampleOutputBuffer.Add(new FloatBufferElement { Value = sourceValue });
                  }

                  grain._Populated = true;
              }
          }
        ).ScheduleParallel();

        // Make sure that the ECB system knows about our job
        _CommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}




//float increment = grain._Pitch;
//            float sourceIndex = grain._StartSampleIndex;

//            DynamicBuffer<ClipDataBufferElement> audioClipData = GetBufferFromEntity<ClipDataBufferElement>(true)[grain._ClipDataEntity];
//            NativeArray<float> audioClipDataArray = audioClipData.Reinterpret<float>().ToNativeArray(Allocator.TempJob);

//            DynamicBuffer<ClipDataBufferElement> sampleBuffer = GetBufferFromEntity<ClipDataBufferElement>(false)[entity];

//            int audioClipLength = audioClipData.Length;

//            float sourceValue = 0f;
//            float sourceIndexRemainder = 0f;

//            for (int i = 0; i < grain._LengthInSamples; i++)
//            {
//                // Replacement pingpong function
//                if (sourceIndex + increment < 0 || sourceIndex + increment > audioClipLength - 1)
//                {
//                    increment = increment * -1f;
//                    sourceIndex -= 1;
//                }

//                sourceIndex += increment;
//                sourceIndexRemainder = sourceIndex % 1;

//                // PITCHING - Interpolate sample if not integer to create 
//                if (sourceIndexRemainder != 0)
//                    sourceValue = math.lerp(
//                        audioClipDataArray[(int)sourceIndex],
//                        audioClipDataArray[(int)sourceIndex + 1],
//                        sourceIndexRemainder);
//                else
//                    sourceValue = audioClipDataArray[(int)sourceIndex];

//                sampleBuffer.Add(new ClipDataBufferElement { Value = sourceValue });
//            }

//            audioClipDataArray.Dispose();
//            grain._Populated = true;






//    _AudioClipEntities = new Entity[_AudioClips.Length];        
//    // Create audio clip entities
//    for (int i = 0; i < _AudioClips.Length; i++)
//    {
//        // Create entity
//        Entity clipEntity = _EntityManager.CreateEntity();

//        // Add it to local array to it can be added to the library buffer later
//        _AudioClipEntities[i] = clipEntity;

//        // Assign a dynamic buffer to hold all the clip samples
//        DynamicBuffer<ClipDataBufferElement> buffer = _EntityManager.AddBuffer<ClipDataBufferElement>(clipEntity);
//        float[] samples = new float[_AudioClips[i].samples];
//        _AudioClips[i].GetData(samples, 0);
//        for (int s = 0; s < samples.Length; s++)
//        {
//            buffer.Add( new ClipDataBufferElement { Value = samples[s] });
//        }
//    }

//    // Create entity
//    Entity clipLibraryEntity = _EntityManager.CreateEntity();

//    // Add AudioClipLibraryComponent component so we can look it up in the system
//    _EntityManager.AddComponentData(clipLibraryEntity, new AudioClipLibraryComponent());

//    // Add a buffer that holds referecnes to all the entities with audio clip data
//    DynamicBuffer<EntityBufferElement> libraryBuffer = _EntityManager.AddBuffer<EntityBufferElement>(clipLibraryEntity);
//    for (int i = 0; i < _AudioClipEntities.Length; i++)
//    {
//        libraryBuffer.Add(new EntityBufferElement { Value = _AudioClipEntities[i] });
//    }
//}

//int _GrainIndex = 0;
//public void ProcessGrainSample(GrainData grainData, int speakerIndex)
//{
//    _GrainIndex++;
//    Entity grainEntity = _EntityManager.CreateEntity();
//    _EntityManager.AddComponentData(grainEntity,
//        new GrainProcessor2()
//        {
//            _ClipDataEntity = grainEntity,
//            _DSPStartSampleIndex = grainData._StartSampleIndex,
//            _Populated = false,
//            _SpeakerIndex = speakerIndex
//        });


//    int lengthInSamples = (int)(_GrainManager._AudioClipLibrary._Clips[grainData._ClipIndex].frequency / 1000 * grainData._Duration);

//    // Create sample processors
//    for (int i = 0; i < lengthInSamples; i++)
//    {
//        Entity sampleProcessorEntity = _EntityManager.CreateEntity();

//        _EntityManager.AddComponentData(sampleProcessorEntity, new SampleProcessor()
//        {
//            _GrainEntity = grainEntity,
//            _SampleOutputArrayIndex = grainData._StartSampleIndex,
//            _ClipDataEntity = _AudioClipEntities[grainData._ClipIndex],
//            _Pitch = grainData._Pitch,
//            _Volume = grainData._Volume
//        });
//    }

//    _EntityManager.AddBuffer<ClipDataBufferElement>(grainEntity);
//}

//private void Update()
//{      
//    for (int i = _GrainEntities.Count - 1; i > 0; i--)
//    {
//        GrainProcessor processedGrain = _EntityManager.GetComponentData<GrainProcessor>(_GrainEntities[i]);
//        if (processedGrain._Populated)
//        {
//            DynamicBuffer<ClipDataBufferElement> sampleBuffer = _EntityManager.GetBuffer<ClipDataBufferElement>(_GrainEntities[i]);
//            NativeArray<float> sampleBufferFloats = sampleBuffer.Reinterpret<float>().ToNativeArray(Allocator.Temp);
//            GrainPlaybackData playbackData = _GrainManager._AllSpeakers[processedGrain._SpeakerIndex].GetGrainPlaybackDataFromPool();

//            playbackData._IsPlaying = true;
//            playbackData._PlaybackIndex = 0;
//            playbackData._PlaybackSampleCount = processedGrain._LengthInSamples;
//            playbackData._StartSampleIndex = processedGrain._StartSampleIndex;
//            playbackData._GrainSamples = sampleBufferFloats.ToArray();

//            _GrainManager._AllSpeakers[processedGrain._SpeakerIndex].AddGrainPlaybackData(playbackData);

//            _EntityManager.DestroyEntity(_GrainEntities[i]);
//            _GrainEntities.RemoveAt(i);
//        }

//    }
//}