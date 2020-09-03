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

public class GrainSynthSystem : SystemBase
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
                    while (sampleIndexNextGrainStart <= dspTimer._CurrentDSPSample + dspTimer._EmissionDurationInSamples && grainCount < maxGrains)
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


