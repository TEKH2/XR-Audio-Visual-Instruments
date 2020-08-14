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

public class GranulatorDOTS :  MonoBehaviour
{
    // Entity manager ref for creating and updating entities
    EntityManager _EntityManager;

    EntityQuery _GrainQuery;
    Entity _DSPTimerEntity;

    GrainManager _GrainManager;
    public AudioClip[] _AudioClips;

    List<GrainSpeakerDOTS> _GrainSpeakers;
    public int _MaxGrainSpeakers = 5;

    public float _LatencyInMS = 50;

    public void Start()
    {
        _GrainManager = GrainManager.Instance;

        _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        _DSPTimerEntity = _EntityManager.CreateEntity();
        _EntityManager.AddComponentData(_DSPTimerEntity, new DSPTimerComponent { _CurrentDSPSample = _GrainManager._CurrentDSPSample, _EmissionLatencyInSamples = (int)(AudioSettings.outputSampleRate * _LatencyInMS) });

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
        _EntityManager.SetComponentData(_DSPTimerEntity, new DSPTimerComponent { _CurrentDSPSample = _GrainManager._CurrentDSPSample, _EmissionLatencyInSamples = _GrainManager.EmissionLatencyInSamples });

        NativeArray<Entity> grainEntities = _GrainQuery.ToEntityArray(Allocator.TempJob);


        if (!_GrainManager._AllSpeakers[0].gameObject.activeSelf)
        {
            _GrainManager._AllSpeakers[0].gameObject.SetActive(true);
            _GrainManager._AllSpeakers[0].transform.position = transform.position;
        }

        for (int i = grainEntities.Length-1; i > 0; i--)
        {
            GrainProcessor grainProcessor = _EntityManager.GetComponentData<GrainProcessor>(grainEntities[i]);

            if(grainProcessor._SamplePopulated)
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

    protected override void OnUpdate()
    {
        // Acquire an ECB and convert it to a concurrent one to be able to use it from a parallel job.
        EntityCommandBuffer.ParallelWriter entityCommandBuffer = _CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        // Get all audio clip data componenets
        NativeArray<AudioClipDataComponent> audioClipData = GetEntityQuery(typeof(AudioClipDataComponent)).ToComponentDataArray<AudioClipDataComponent>(Allocator.TempJob);

        WindowingDataComponent windowingData = GetSingleton<WindowingDataComponent>();

        DSPTimerComponent dspTimer = GetSingleton<DSPTimerComponent>();

        float dt = Time.DeltaTime;


        // ----------------------------------- CHECK EMITTERS IN SPEAKER RADIUS


        // ----------------------------------- EMITTER UPDATE
        Entities.ForEach
        (
            (int entityInQueryIndex, ref EmitterComponent emitter) =>
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

                        _SpeakerIndex = 0,
                        _DSPSamplePlaybackStart = sampleIndexNextGrainStart,// + emitter._RandomOffsetInSamples,
                        _SamplePopulated = false
                    });

                    // Set last grain emitted index
                    emitter._LastGrainEmissionDSPIndex = sampleIndexNextGrainStart;
                    // Increment emission Index
                    sampleIndexNextGrainStart += emitter._CadenceInSamples;

                    grainCount++;

                    // Add sample buffer
                    entityCommandBuffer.AddBuffer<FloatBufferElement>(entityInQueryIndex, grainProcessorEntity);

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
        ).WithDisposeOnCompletion(audioClipData).ScheduleParallel();


        // ----------------------------------- GRAIN PROCESSOR UPDATE
        Entities.ForEach
        (
            (int entityInQueryIndex, DynamicBuffer<FloatBufferElement> sampleOutputBuffer, ref GrainProcessor grain) =>
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
                        
                        sampleOutputBuffer.Add(new FloatBufferElement { Value = sourceValue });
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
        Entities.ForEach
        (
           (int entityInQueryIndex, DynamicBuffer<FloatBufferElement> sampleOutputBuffer, in DSP_BitCrush dsp, in GrainProcessor grain) =>
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

                       sampleOutputBuffer[i] = new FloatBufferElement { Value = sampleOut };
                   }
               }
           }
        ).ScheduleParallel();

        Entities.ForEach
        (
           (int entityInQueryIndex, DynamicBuffer<FloatBufferElement> sampleOutputBuffer, in DSP_Filter dsp, in GrainProcessor grain) =>
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

                       sampleOutputBuffer[i] = new FloatBufferElement { Value = sampleOut };
                   }
               }
           }
        ).ScheduleParallel();
    }
}