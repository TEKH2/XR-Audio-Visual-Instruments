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

public class GranulatorDOTS :  MonoBehaviour
{
    public static GranulatorDOTS Instance;

    // Entity manager ref for creating and updating entities
    EntityManager _EntityManager;

    EntityQuery _GrainQuery;
    Entity _DSPTimerEntity;
    Entity _SpeakerManagerEntity;

    AudioListener _Listener;

    public AudioClip[] _AudioClips;

    public GrainSpeakerDOTS _SpeakerPrefab;
    public List<GrainSpeakerDOTS> _GrainSpeakers = new List<GrainSpeakerDOTS>();
    public int _MaxGrainSpeakers = 5;

    public float _LatencyInMS = 50;
    [Range(0, 100)]
    public float _EmissionLatencyMS = 80;
    int _SampleRate;
    public int EmissionLatencyInSamples { get { return (int)(_EmissionLatencyMS * _SampleRate * .001f); } }

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
        _EntityManager.AddComponentData(_DSPTimerEntity, new DSPTimerComponent { _CurrentDSPSample = _CurrentDSPSample, _EmissionLatencyInSamples = (int)(AudioSettings.outputSampleRate * _LatencyInMS) });

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
        _EntityManager.SetComponentData(_DSPTimerEntity, new DSPTimerComponent { _CurrentDSPSample = _CurrentDSPSample, _EmissionLatencyInSamples = EmissionLatencyInSamples });

        NativeArray<Entity> grainEntities = _GrainQuery.ToEntityArray(Allocator.TempJob);


        DynamicBuffer<GrainSpeakerBufferElement> activeSpeakerBuffer = _EntityManager.GetBuffer<GrainSpeakerBufferElement>(_SpeakerManagerEntity);
        // Update audio listener position
        _EntityManager.SetComponentData(_SpeakerManagerEntity, new SpeakerManagerComponent
        {
            _ListenerPos = _Listener.transform.position,
            _EmitterToListenerActivationRange = _EmitterToListenerActivationRange,
            _EmitterToSpeakerAttachRadius = _EmitterToSpeakerAttachRadius
        });

        if (!_GrainSpeakers[0].gameObject.activeSelf)
        {
            _GrainSpeakers[0].gameObject.SetActive(true);
            _GrainSpeakers[0].transform.position = transform.position;
        }

        // print("Processed grains: " + grainEntities.Length);
        for (int i = grainEntities.Length-1; i > 0; i--)
        {
            GrainProcessor grainProcessor = _EntityManager.GetComponentData<GrainProcessor>(grainEntities[i]);

            if(grainProcessor._SamplePopulated)
            {
                GrainPlaybackData playbackData = _GrainSpeakers[0].GetGrainPlaybackDataFromPool();

                if (playbackData == null)
                    break;

                NativeArray<float> samples = _EntityManager.GetBuffer<GrainSampleBufferElement>(grainEntities[i]).Reinterpret<float>().ToNativeArray(Allocator.Temp);

                playbackData._IsPlaying = true;
                playbackData._PlaybackIndex = 0;
                playbackData._PlaybackSampleCount = samples.Length;
                playbackData._DSPStartIndex = grainProcessor._DSPSamplePlaybackStart;

                //print("Current dsp time: " + _GrainManager._CurrentDSPSample + "   grain start dsp index: " + playbackData._DSPStartIndex);

                int sampleLength = samples.Length;
                for (int s = 0; s < playbackData._GrainSamples.Length; s++)
                {
                    if(s<sampleLength)
                        playbackData._GrainSamples[s] = samples[s];
                    else
                        playbackData._GrainSamples[s] = 0;
                }

                // Destroy entity once we have sapped it of it's samply goodness
                _EntityManager.DestroyEntity(grainEntities[i]);

               _GrainSpeakers[grainProcessor._SpeakerIndex].AddGrainPlaybackData(playbackData);

                //print("Copying sample data over: " + samples[1000] + "     " + playbackData._GrainSamples[1000]);
                //print(".....Copying sample data over: " + samples[1500] + "     " + playbackData._GrainSamples[1500]);
            }
        }

        grainEntities.Dispose();



    }

    public void CreateSpeaker(Vector3 pos)
    {
        GrainSpeakerDOTS speaker = Instantiate(_SpeakerPrefab, pos, quaternion.identity);
        speaker._SpeakerIndex = _GrainSpeakers.Count;
        _GrainSpeakers.Add(speaker);
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        for (int dataIndex = 0; dataIndex < data.Length; dataIndex += channels)
        {
            _CurrentDSPSample++;
        }
    }
}

public class SpeakerFinderSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        SpeakerManagerComponent speakerManager = GetSingleton<SpeakerManagerComponent>();
        DSPTimerComponent dspTimer = GetSingleton<DSPTimerComponent>();

        // ------------------------------------------------------------------------------------- CHECK SPEAKERS IN RANGE
        Entities.ForEach((ref GrainSpeakerComponent speaker, ref Translation emitterTrans) =>
        {
            bool prevInRange = speaker._InRange;

            // -------------------------------------------  CHECK IN RANGE
            speaker._InRange = math.distance(emitterTrans.Value, speakerManager._ListenerPos) < speakerManager._EmitterToListenerActivationRange;

            // If moving out of range
            if (prevInRange && !speaker._InRange)
            {
                Debug.Log("Speaker moving out of range: " + speaker._Index);
                speaker._ConnectedToEmitter = false;
            }
        });

        // ------------------------------------------------------------------------------------- CHECK EMITTERS IN RANGE AND ATTACH TO SPEAKERS
        Entities.ForEach((ref EmitterComponent emitter, ref Translation emitterTrans) =>
        {
            bool prevInRange = emitter._InRange;

            // -------------------------------------------  CHECK IN RANGE
            emitter._InRange = math.distance(emitterTrans.Value, speakerManager._ListenerPos) < speakerManager._EmitterToListenerActivationRange;
                    
            // If moving out of range
            if(prevInRange && !emitter._InRange)
            {
                Debug.Log("Emitter moving out of range.");
                emitter._AttachedToSpeaker = false;
            }

            // if in range but not active
            if(emitter._InRange && !emitter._AttachedToSpeaker)
            {
                float closestDist = speakerManager._EmitterToSpeakerAttachRadius;
                int foundSpeakerIndex = 0;
                bool speakerFound = false;
                float3 emitterPos = emitterTrans.Value;             
                Entity foundSpeakerEntity;

                // Search all currently connected speakers
                Entities.ForEach((Entity speakerEntity, ref GrainSpeakerComponent speaker, ref Translation speakerTrans) =>
                {
                    if (speaker._InRange && speaker._ConnectedToEmitter)
                    {
                        float dist = math.distance(emitterPos, speakerTrans.Value);

                        if (dist < closestDist)
                        {                            
                            closestDist = dist;

                            foundSpeakerIndex = speaker._Index;
                            foundSpeakerEntity = speakerEntity;
                            speakerFound = true;
                        }
                    }
                });

                if(speakerFound)
                {
                    Debug.Log("Found active speaker index / dist " + foundSpeakerIndex + "   " + closestDist);
                    emitter._SpeakerIndex = foundSpeakerIndex;
                    emitter._AttachedToSpeaker = true;
                    emitter._LastGrainEmissionDSPIndex = dspTimer._CurrentDSPSample;
                    return;
                }

                // Search all inactive speakers with active tag component
                Entities.ForEach((Entity speakerEntity, ref GrainSpeakerComponent speaker, ref Translation speakerTrans) =>
                {
                    // if spaker isnt found, search through inactive speakers
                    if (!speaker._ConnectedToEmitter && !speakerFound)
                    {
                        // Set speaker active and move to emitter positions
                        speaker._InRange = true;
                        speakerTrans.Value = emitterPos;

                        Debug.Log("Emitter pos: " + emitterPos);

                        foundSpeakerIndex = speaker._Index;
                        foundSpeakerEntity = speakerEntity;
                        speakerFound = true;
                    }
                });

                if (speakerFound)
                {
                    Debug.Log("Found inactive speaker index / dist " + foundSpeakerIndex);
                    emitter._SpeakerIndex = foundSpeakerIndex;
                    emitter._AttachedToSpeaker = true;
                    emitter._LastGrainEmissionDSPIndex = dspTimer._CurrentDSPSample;
                    return;
                }
            }
        });
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

    protected override void OnUpdate()
    {
        // Acquire an ECB and convert it to a concurrent one to be able to use it from a parallel job.
        EntityCommandBuffer.ParallelWriter entityCommandBuffer = _CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();


        //// ----------------------------------- CHECK IF LISTENER IS IN RANGE OF INACTIVE EMITTERS
        //SpeakerManagerComponent speakerManager = GetSingleton<SpeakerManagerComponent>();
        //EntityQuery speakerQuery = GetEntityQuery(typeof(GrainSpeakerComponent), typeof(Translation));
        //NativeArray<Entity> speakerEntities = speakerQuery.ToEntityArray(Allocator.TempJob);
        //NativeArray<Translation> speakerTranslations = speakerQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        //NativeArray<GrainSpeakerComponent> speakers = speakerQuery.ToComponentDataArray<GrainSpeakerComponent>(Allocator.TempJob);

        //Entities.ForEach
        //(
        //   (int entityInQueryIndex, ref EmitterComponent emitter, in Translation emitterTrans) =>
        //   {
        //       if (!emitter._Active)
        //       {
        //           bool inactiveSpeakerFound = false;

        //           // find distance to the listener
        //           float emitterToListenerDist = math.distance(emitterTrans.Value, speakerManager._ListenerPos);

        //           if (emitterToListenerDist < speakerManager._EmitterActivationDist)
        //           {
        //               // Set In range flag
        //               emitter._InRange = true;

        //               float closestDist = speakerManager._SpeakerEmitterAttachDist;
        //               int foundSpeakerIndex = 0;
        //               bool activeSpeakerFound = false;

        //               // ------------------------------  TRY FIND CLOSE ACTIVE SPEAKER
        //               for (int i = 0; i < speakers.Length; i++)
        //               {
        //                   if (speakers[i]._Active)
        //                   {
        //                       float dist = math.distance(emitterTrans.Value, speakerTranslations[i].Value);

        //                       if (dist < closestDist)
        //                       {
        //                           Debug.Log("Found active speaker index / dist " + speakers[i]._Index + "   " + dist);
        //                           closestDist = dist;
        //                           foundSpeakerIndex = speakers[i]._Index;
        //                           activeSpeakerFound = true;
        //                       }
        //                   }
        //               }

        //               // Only find one inactive spaeker per update to stop parallell conflicts
        //               if (!activeSpeakerFound && !inactiveSpeakerFound)
        //               {
        //                   //------------------------------FIND INACTIVE SPEAKER
        //                   for (int i = 0; i < speakers.Length; i++)
        //                   {
        //                       if (!speakers[i]._Active)
        //                       {
        //                           foundSpeakerIndex = speakers[i]._Index;
        //                           activeSpeakerFound = true;

        //                           Debug.Log("Found inactive speaker: " + foundSpeakerIndex);

        //                           entityCommandBuffer.SetComponent(entityInQueryIndex, speakerEntities[i], new GrainSpeakerComponent
        //                           {
        //                               _Active = true,
        //                               _Index = speakers[i]._Index
        //                           });

        //                           entityCommandBuffer.SetComponent(entityInQueryIndex, speakerEntities[i], new Translation
        //                           {
        //                               Value = emitterTrans.Value
        //                           });

        //                           inactiveSpeakerFound = true;
        //                       }
        //                   }
        //               }

        //               if (activeSpeakerFound)
        //               {
        //                   Debug.Log(entityInQueryIndex + "  speaker found: " + foundSpeakerIndex);
        //                   emitter._Active = true;
        //                   emitter._SpeakerIndex = foundSpeakerIndex;
        //               }
        //           }
        //       }
        //   }
        //).WithDisposeOnCompletion(speakerTranslations).WithDisposeOnCompletion(speakers).ScheduleParallel();


        // ----------------------------------- EMITTER UPDATE
        // Get all audio clip data componenets
        NativeArray<AudioClipDataComponent> audioClipData = GetEntityQuery(typeof(AudioClipDataComponent)).ToComponentDataArray<AudioClipDataComponent>(Allocator.TempJob);
        WindowingDataComponent windowingData = GetSingleton<WindowingDataComponent>();
        DSPTimerComponent dspTimer = GetSingleton<DSPTimerComponent>();
        float dt = Time.DeltaTime;
        Entities.ForEach
        (
            (int entityInQueryIndex, ref EmitterComponent emitter) =>
            {
                if (emitter._AttachedToSpeaker)
                {
                    // Max grains to stop it getting stuck in a while loop
                    int maxGrains = 20;
                    int grainCount = 0;

                    int sampleIndexNextGrainStart = emitter._LastGrainEmissionDSPIndex + emitter._CadenceInSamples;
                    while (sampleIndexNextGrainStart <= dspTimer._CurrentDSPSample + dspTimer._EmissionLatencyInSamples && grainCount < maxGrains)
                    {
                        // Create a new grain processor entity
                        Entity grainProcessorEntity = entityCommandBuffer.CreateEntity(entityInQueryIndex);

                        entityCommandBuffer.AddComponent(entityInQueryIndex, grainProcessorEntity, new GrainProcessor
                        {
                            _AudioClipDataComponent = audioClipData[0],

                            _PlaybackHeadNormPos = emitter._PlayheadPosNormalized,
                            _DurationInSamples = emitter._DurationInSamples,

                            _Pitch = emitter._Pitch,
                            _Volume = emitter._Volume,

                            _SpeakerIndex = emitter._SpeakerIndex,
                            _DSPSamplePlaybackStart = sampleIndexNextGrainStart,// + emitter._RandomOffsetInSamples,
                            _SamplePopulated = false
                        });

                        // Set last grain emitted index
                        emitter._LastGrainEmissionDSPIndex = sampleIndexNextGrainStart;
                        // Increment emission Index
                        sampleIndexNextGrainStart += emitter._CadenceInSamples;

                        grainCount++;

                        // Add sample buffer
                        entityCommandBuffer.AddBuffer<GrainSampleBufferElement>(entityInQueryIndex, grainProcessorEntity);

                        // Add bitcrush from emitter
                        entityCommandBuffer.AddComponent(entityInQueryIndex, grainProcessorEntity, new DSP_BitCrush
                        {
                            downsampleFactor = emitter._BitCrush.downsampleFactor
                        });

                        entityCommandBuffer.AddComponent(entityInQueryIndex, grainProcessorEntity, new DSP_Filter
                        {
                            a0 = emitter._Filter.a0,
                            a1 = emitter._Filter.a1,
                            a2 = emitter._Filter.a2,
                            b1 = emitter._Filter.b1,
                            b2 = emitter._Filter.b2
                        });
                    }
                }
            }
        ).WithDisposeOnCompletion(audioClipData).ScheduleParallel();


        // ----------------------------------- GRAIN PROCESSOR UPDATE
        Entities.ForEach
        (
            (int entityInQueryIndex, DynamicBuffer<GrainSampleBufferElement> sampleOutputBuffer, ref GrainProcessor grain) =>
            {
                if (!grain._SamplePopulated)
                {
                    float sourceIndex = grain._PlaybackHeadNormPos * grain._AudioClipDataComponent._ClipDataBlobAsset.Value.array.Length;
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

                        // Adjusted for volume and windowing
                        sourceValue *= grain._Volume;
                       
                        // Map doesn't work inside a job TODO investigate how to use methods in a job
                        sourceValue *= windowingData._WindowingArray.Value.array[(int)Map(i, 0, grain._DurationInSamples, 0, windowingData._WindowingArray.Value.array.Length)];
                        
                        sampleOutputBuffer.Add(new GrainSampleBufferElement { Value = sourceValue });
                    }

                    grain._SamplePopulated = true;
                }
            }
        ).ScheduleParallel();

        // Make sure that the ECB system knows about our job
        _CommandBufferSystem.AddJobHandleForProducer(Dependency);
    }

    public static float Map(float val, float inMin, float inMax, float outMin, float outMax)
    {
        return outMin + ((outMax - outMin) / (inMax - inMin)) * (val - inMin);
    }
}



[UpdateAfter(typeof(GranulatorSystem))]
public class DSPSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Bitcrush
        //---------------------------------------------------------------------
        Entities.ForEach
        (
           (int entityInQueryIndex, DynamicBuffer<GrainSampleBufferElement> sampleOutputBuffer, in DSP_BitCrush dsp, in GrainProcessor grain) =>
           {
               if (grain._SamplePopulated)
               {
                   float prevValue = 0;
                   float count = 0;

                   for (int i = 0; i < sampleOutputBuffer.Length; i++)
                   {
                       float sampleOut = 0;
                       float sampleIn = sampleOutputBuffer[i].Value;

                       if (count >= dsp.downsampleFactor)
                       {
                           sampleOut = sampleIn;
                           prevValue = sampleOut;
                           count = 0;
                       }
                       else
                           sampleOut = prevValue;

                       count++;

                       sampleOutputBuffer[i] = new GrainSampleBufferElement { Value = sampleOut };
                   }
               }
           }
        ).ScheduleParallel();

        // Filter
        //---------------------------------------------------------------------
        Entities.ForEach
        (
           (int entityInQueryIndex, DynamicBuffer<GrainSampleBufferElement> sampleOutputBuffer, in DSP_Filter dsp, in GrainProcessor grain) =>
           {
               if (grain._SamplePopulated)
               {
                   float previousX1 = 0;
                   float previousX2 = 0;
                   float previousY1 = 0;
                   float previousY2 = 0;

                   for (int i = 0; i < sampleOutputBuffer.Length; i++)
                   {
                       float sampleOut = 0;
                       float sampleIn = sampleOutputBuffer[i].Value;

                       sampleOut = (sampleIn * dsp.a0 +
                                    previousX1 * dsp.a1 +
                                    previousX2 * dsp.a2) -
                                    (previousY1 * dsp.b1 +
                                    previousY2 * dsp.b2);

                       previousX2 = previousX1;
                       previousX1 = sampleIn;
                       previousY2 = previousY1;
                       previousY1 = sampleOut;

                       sampleOutputBuffer[i] = new GrainSampleBufferElement { Value = sampleOut };
                   }
               }
           }
        ).ScheduleParallel();
    }
}