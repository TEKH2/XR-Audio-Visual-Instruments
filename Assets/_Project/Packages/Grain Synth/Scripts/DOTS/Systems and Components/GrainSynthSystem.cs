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
using System.ComponentModel;

[UpdateAfter(typeof(RangeCheckSystem))]
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


        // ----------------------------------- EMITTER UPDATE
        // Get all audio clip data componenets
        NativeArray<AudioClipDataComponent> audioClipData = GetEntityQuery(typeof(AudioClipDataComponent)).ToComponentDataArray<AudioClipDataComponent>(Allocator.TempJob);
        WindowingDataComponent windowingData = GetSingleton<WindowingDataComponent>();
        DSPTimerComponent dspTimer = GetSingleton<DSPTimerComponent>();
        float dt = Time.DeltaTime;

        // Process 
        JobHandle emitGrains = Entities.ForEach
        (
            (int entityInQueryIndex, DynamicBuffer<DSPParametersElement> dspTypeBuffer, ref EmitterComponent emitter) =>
            {
                if (emitter._AttachedToSpeaker && emitter._Playing)
                {
                    //-- Max grains to stop it getting stuck in a while loop
                    int maxGrains = 30;
                    int grainCount = 0;

                    int sampleIndexNextGrainStart = emitter._LastGrainEmissionDSPIndex + emitter._CadenceInSamples;
                    while (sampleIndexNextGrainStart <= dspTimer._CurrentDSPSample + dspTimer._GrainQueueDuration && grainCount < maxGrains)
                    {
                        //-- Create a new grain processor entity
                        Entity grainProcessorEntity = entityCommandBuffer.CreateEntity(entityInQueryIndex);
                        entityCommandBuffer.AddComponent(entityInQueryIndex, grainProcessorEntity, new GrainProcessor
                        {
                            _AudioClipDataComponent = audioClipData[emitter._AudioClipIndex],

                            _PlayheadNorm = emitter._PlayheadPosNormalized,
                            _SampleCount = emitter._DurationInSamples,

                            _Pitch = emitter._Pitch,
                            _Volume = emitter._Volume * emitter._DistanceAmplitude,

                            _SpeakerIndex = emitter._SpeakerIndex,
                            _DSPStartIndex = sampleIndexNextGrainStart,// + emitter._RandomOffsetInSamples,
                            _SamplePopulated = false
                        });


                        //-- Add sample buffer to grain processor
                        entityCommandBuffer.AddBuffer<GrainSampleBufferElement>(entityInQueryIndex, grainProcessorEntity);


                        //--    Add DSP Buffer
                        DynamicBuffer<DSPParametersElement> dspBuffer = entityCommandBuffer.AddBuffer<DSPParametersElement>(entityInQueryIndex, grainProcessorEntity);
                        for (int i = 0; i < dspTypeBuffer.Length; i++)
                        {
                            dspBuffer.Add(dspTypeBuffer[i]);
                        }


                        // Set last and next grain time
                        emitter._LastGrainEmissionDSPIndex = sampleIndexNextGrainStart;
                        sampleIndexNextGrainStart += emitter._CadenceInSamples;
                        grainCount++;
                    }
                }
            }
        ).WithDisposeOnCompletion(audioClipData)
        .ScheduleParallel(this.Dependency);


        // Make sure that the ECB system knows about our job
        _CommandBufferSystem.AddJobHandleForProducer(emitGrains);



        // ----------------------------------- GRAIN PROCESSOR UPDATE
        JobHandle processGrains = Entities.ForEach
        (
            (DynamicBuffer<GrainSampleBufferElement> sampleOutputBuffer, ref GrainProcessor grain) =>
            {
                if (!grain._SamplePopulated)
                {
                    float sourceIndex = grain._PlayheadNorm * grain._AudioClipDataComponent._ClipDataBlobAsset.Value.array.Length;
                    float increment = grain._Pitch;
                    int sampleCount = 0;

                    for (int i = 0; i < grain._SampleCount; i++)
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
                        sourceValue *= windowingData._WindowingArray.Value.array[(int)Map(i, 0, grain._SampleCount, 0, windowingData._WindowingArray.Value.array.Length)];

                        sampleOutputBuffer.Add(new GrainSampleBufferElement { Value = sourceValue, DSPSampleIndex = grain._DSPStartIndex + sampleCount });
                        sampleCount++;
                    }

                    grain._SamplePopulated = true;
                }
            }
        ).ScheduleParallel(emitGrains);



        //----    DSP CHAIN
        JobHandle dspGrains = Entities.ForEach
        (
           (DynamicBuffer<DSPParametersElement> dspParamsBuffer, DynamicBuffer<GrainSampleBufferElement> sampleOutputBuffer, ref GrainProcessor grain) =>
           {
               if (grain._SamplePopulated)
               {
                   for (int i = 0; i < dspParamsBuffer.Length; i++)
                   {
                       switch (dspParamsBuffer[i]._DSPType)
                       {
                           case DSPTypes.Bitcrush:
                               DSP_Bitcrush.ProcessDSP(dspParamsBuffer[i], sampleOutputBuffer);
                               break;
                           case DSPTypes.Delay:
                               break;
                           case DSPTypes.Flange:
                               DSP_Flange.ProcessDSP(dspParamsBuffer[i], sampleOutputBuffer);
                               break;
                       }
                   }
               }
           }
        ).ScheduleParallel(processGrains);



        //// ----------------------------------- AUDIO SAMPLE BUFFER TEST
        //Entities.ForEach((ref DynamicBuffer<AudioSampleBufferElement> audioBuffer, in RollingBufferFiller rollingBufferFiller) =>
        //{
        //    // Testing audio buffer, filling with a sin wave. Doesn't take start time into account yet
        //    float freq = 500;
        //    int startIndex = dspTimer._CurrentDSPSample + 1000;

        //    for (int i = 0; i < rollingBufferFiller._SampleCount; i++)
        //    {
        //        int currentIndex = (startIndex + i) % audioBuffer.Length;

        //        float norm = currentIndex / (audioBuffer.Length - 1f);
        //        float sinWaveSample = math.sin(freq * norm * Mathf.PI * 2) * .5f;
        //        audioBuffer[currentIndex] = new AudioSampleBufferElement { Value = sinWaveSample };
        //    }
        //}).ScheduleParallel();


        this.Dependency = dspGrains;

        
    }

    public static void TestHalfVolSynth(DynamicBuffer<GrainSampleBufferElement> sampleOutputBuffer, DSPParametersElement dsp)
    {
        for (int s = 0; s < sampleOutputBuffer.Length; s++)
        {
            sampleOutputBuffer[s] = new GrainSampleBufferElement { Value = sampleOutputBuffer[s].Value * dsp._Value0 };
        }
    }

    public static float Map(float val, float inMin, float inMax, float outMin, float outMax)
    {
        return outMin + ((outMax - outMin) / (inMax - inMin)) * (val - inMin);
    }
}


[UpdateAfter(typeof(GrainSynthSystem))]
public class GrainsToAudioBuffersSystem : SystemBase
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



        //// Acquire an ECB and convert it to a concurrent one to be able to use it from a parallel job.
        //EntityCommandBuffer.ParallelWriter entityCommandBuffer = _CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        //NativeArray<Entity> speakerEnts = GetEntityQuery(ComponentType.ReadOnly(typeof(GrainSpeakerComponent))).ToEntityArray(Allocator.TempJob);

        ////---- ADD ALL GRAIN PROCESSOR SAMPLES TO THE ASSIGNED SPEAKER
        //JobHandle addGrainsToSpeakers = Entities.ForEach
        //((int entityInQueryIndex, Entity entity, DynamicBuffer<GrainSampleBufferElement> grainOutputBuffer, ref GrainProcessor grain) =>
        //{
        //    //--  If not attached to a speaker then return
        //    if (grain._SpeakerIndex == int.MaxValue) return;

        //    Entity speakerEntity = speakerEnts[grain._SpeakerIndex];
        //    for (int i = 0; i < grainOutputBuffer.Length; i++)            
        //        entityCommandBuffer.AppendToBuffer(entityInQueryIndex, speakerEntity, grainOutputBuffer[i] );            
        //}).WithDisposeOnCompletion(speakerEnts)
        //.ScheduleParallel(this.Dependency);



        ////----    AGGREGATE GRAIN SAMPLES
        //DSPTimerComponent dspTimer = GetSingleton<DSPTimerComponent>();

        //JobHandle aggregateGrainSamplesInSpeaker = Entities.ForEach
        //((int entityInQueryIndex, Entity entity, DynamicBuffer<GrainSpeakerComponent> grainSampleElementBuffer, DynamicBuffer<AudioRingBufferElement> outputBuffer) =>
        //{
        //    //Debug.Log("here1");
        //    //--  If not attached to a speaker then return
        //    if (grainSampleElementBuffer.Length == 0)
        //        return;

        //    int startDSPIndex = dspTimer._CurrentDSPSample;
        //    //Debug.Log("here2");
        //    for (int i = 0; i < grainSampleElementBuffer.Length; i++)
        //    {
        //        int index = grainSampleElementBuffer[i].DSPSampleIndex - startDSPIndex;
        //        outputBuffer[index] = new AudioRingBufferElement { Value = outputBuffer[index].Value + grainSampleElementBuffer[i].Value };
        //    }
        //})
        //.ScheduleParallel(addGrainsToSpeakers);


        //this.Dependency = aggregateGrainSamplesInSpeaker;

        //// Make sure that the ECB system knows about our job
        //_CommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}