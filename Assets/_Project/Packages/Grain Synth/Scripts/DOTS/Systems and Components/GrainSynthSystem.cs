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
        int sampleRate = AudioSettings.outputSampleRate;

        // Acquire an ECB and convert it to a concurrent one to be able to use it from a parallel job.
        EntityCommandBuffer.ParallelWriter entityCommandBuffer = _CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();


        #region EMIT GRAINS
        // ----------------------------------- EMITTER UPDATE
        // Get all audio clip data componenets
        NativeArray<AudioClipDataComponent> audioClipData = GetEntityQuery(typeof(AudioClipDataComponent)).ToComponentDataArray<AudioClipDataComponent>(Allocator.TempJob);
        WindowingDataComponent windowingData = GetSingleton<WindowingDataComponent>();
        DSPTimerComponent dspTimer = GetSingleton<DSPTimerComponent>();
        float dt = Time.DeltaTime;

        //---   CREATES ENTITIES W/ GRAIN PROCESSOR + GRAIN SAMPLE BUFFER + DSP SAMPLE BUFFER + DSP PARAMS BUFFER

        JobHandle emitGrains = Entities.ForEach
        (
            (int entityInQueryIndex, ref DynamicBuffer<DSPParametersElement> dspParams, ref EmitterComponent emitter) =>
            {
                if (emitter._AttachedToSpeaker && emitter._Playing)
                {
                    //-- Max grains to stop it getting stuck in a while loop
                    int maxGrains = 30;
                    int grainCount = 0;

                    int sampleIndexNextGrainStart = emitter._LastGrainEmissionDSPIndex + emitter._CadenceInSamples;
                    int dspTailLength = 0;

                    //-- Create grain processor entity
                    while (sampleIndexNextGrainStart <= dspTimer._CurrentDSPSample + dspTimer._GrainQueueDuration && grainCount < maxGrains)
                    {
                        for (int i = 0; i < dspParams.Length; i++)
                        {
                            //-- Find the largest DSP effect tail
                            if (dspParams[i]._DSPType == DSPTypes.Flange || dspParams[i]._DSPType == DSPTypes.Delay || dspParams[i]._DSPType == DSPTypes.Chopper)
                            {
                                if (dspParams[i]._SampleTail > dspTailLength)
                                {
                                    dspTailLength = dspParams[i]._SampleTail;
                                }
                            }
                        }
                        dspTailLength = Mathf.Clamp(dspTailLength, 0, emitter._SampleRate - emitter._DurationInSamples);

                        Entity grainProcessorEntity = entityCommandBuffer.CreateEntity(entityInQueryIndex);
                        entityCommandBuffer.AddComponent(entityInQueryIndex, grainProcessorEntity, new GrainProcessor
                        {
                            _AudioClipDataComponent = audioClipData[emitter._AudioClipIndex],

                            _PlayheadNorm = emitter._PlayheadPosNormalized,
                            _SampleCount = emitter._DurationInSamples,

                            _Pitch = emitter._Pitch,
                            _Volume = emitter._Volume * emitter._DistanceAmplitude,

                            _SpeakerIndex = emitter._SpeakerIndex,
                            _DSPStartIndex = sampleIndexNextGrainStart,
                            _SamplePopulated = false,

                            _DSPEffectSampleTailLength = dspTailLength
                        }); ;


                        //-- Add sample and DSP buffers to grain processor
                        entityCommandBuffer.AddBuffer<GrainSampleBufferElement>(entityInQueryIndex, grainProcessorEntity);
                        entityCommandBuffer.AddBuffer<DSPSampleBufferElement>(entityInQueryIndex, grainProcessorEntity);

                        //--    Add DSP Parameters
                        DynamicBuffer<DSPParametersElement> dspParameters = entityCommandBuffer.AddBuffer<DSPParametersElement>(entityInQueryIndex, grainProcessorEntity);
                        for (int i = 0; i < dspParams.Length; i++)
                        {
                            DSPParametersElement tempParams = dspParams[i];
                            tempParams._SampleStartTime = sampleIndexNextGrainStart;
                            dspParameters.Add(tempParams);
                        }

                        // Set last and next grain time
                        emitter._LastGrainEmissionDSPIndex = sampleIndexNextGrainStart;
                        sampleIndexNextGrainStart += emitter._CadenceInSamples;
                        grainCount++;
                    }
                }
            }
        ).ScheduleParallel(this.Dependency);
        //.WithDisposeOnCompletion(audioClipData)


        // Make sure that the ECB system knows about our job
        _CommandBufferSystem.AddJobHandleForProducer(emitGrains);


        //
        // BRAD: Not sure if I've set up the job dependencies or audio clip disposal correctly
        //

        JobHandle emitBurst = Entities.WithoutBurst().ForEach
        (
            (int entityInQueryIndex, ref DynamicBuffer<DSPParametersElement> dspChain, ref BurstEmitterComponent burst) =>
            {
                if (burst._AttachedToSpeaker && burst._Playing)
                {
                    Debug.Log("---------------------------------------------------");
                    Debug.Log("Bursting.....");

                    int currentDSPTime = dspTimer._CurrentDSPSample + dspTimer._GrainQueueDuration;
                    
                    int dspTailLength = 0;

                    // Create and queue every grain for the burst event --- probably need to revise
                    for (int i = 0; i < burst._BurstCount; i++)
                    {
                        // Create grain processor properties based on burst shape and target duration
                        int offset = (int)Map(i, 0, burst._BurstCount, 0, burst._BurstDuration);
                        int duration = (int)Map(i, 0, burst._BurstCount, burst._DurationStart, burst._DurationEnd, burst._BurstShape);
                        float pitch = (int)Map(i, 0, burst._BurstCount, burst._PitchStart, burst._PitchEnd, burst._BurstShape);
                        float volume = (int)Map(i, 0, burst._BurstCount, burst._VolumeStart, burst._VolumeEnd, burst._BurstShape);

                        Debug.Log("DSP TIME: " + currentDSPTime + "    OFFSET: " + offset + "    TARGET DSP TIME: " + (currentDSPTime + offset));


                        for (int j = 0; j < dspChain.Length; j++)
                        {
                            //-- Find the largest DSP effect tail
                            if (dspChain[j]._DSPType == DSPTypes.Flange || dspChain[j]._DSPType == DSPTypes.Delay || dspChain[j]._DSPType == DSPTypes.Chopper)
                            {
                                if (dspChain[j]._SampleTail > dspTailLength)
                                {
                                    dspTailLength = dspChain[j]._SampleTail;
                                }
                            }
                        }

                        dspTailLength = Mathf.Clamp(dspTailLength, 0, burst._SampleRate - duration);

                        Entity grainProcessorEntity = entityCommandBuffer.CreateEntity(entityInQueryIndex);
                        entityCommandBuffer.AddComponent(entityInQueryIndex, grainProcessorEntity, new GrainProcessor
                        {
                            _AudioClipDataComponent = audioClipData[burst._AudioClipIndex],

                            _PlayheadNorm = burst._PlayheadPosNormalized,
                            _SampleCount = duration,

                            _Pitch = pitch,
                            _Volume = volume * burst._DistanceAmplitude,

                            _SpeakerIndex = burst._SpeakerIndex,
                            _DSPStartIndex = offset + currentDSPTime,
                            _SamplePopulated = false,

                            _DSPEffectSampleTailLength = dspTailLength
                        });


                        //-- Add sample and DSP buffers to grain processor
                        entityCommandBuffer.AddBuffer<GrainSampleBufferElement>(entityInQueryIndex, grainProcessorEntity);
                        entityCommandBuffer.AddBuffer<DSPSampleBufferElement>(entityInQueryIndex, grainProcessorEntity);

                        //--    Add DSP Parameters
                        DynamicBuffer<DSPParametersElement> dspParameters = entityCommandBuffer.AddBuffer<DSPParametersElement>(entityInQueryIndex, grainProcessorEntity);

                        for (int j = 0; j < dspChain.Length; j++)
                        {
                            DSPParametersElement tempParams = dspChain[j];
                            tempParams._SampleStartTime = currentDSPTime;
                            dspParameters.Add(tempParams);
                        }
                    }

                    burst._Playing = false;
                }
            }
        ).WithDisposeOnCompletion(audioClipData)
        .ScheduleParallel(emitGrains);


        // Make sure that the ECB system knows about our job
        _CommandBufferSystem.AddJobHandleForProducer(emitBurst);
        #endregion



        #region POPULATE GRAIN & DSP
        // ----------------------------------- GRAIN PROCESSOR UPDATE
        //---   TAKES GRAIN PROCESSOR INFORMATION AND FILLS THE SAMPLE BUFFER + DSP BUFFER (W/ 0s TO THE SAME LENGTH AS SAMPLE BUFFER)
        JobHandle processGrains = Entities.ForEach
        (
            (DynamicBuffer<GrainSampleBufferElement> sampleOutputBuffer, DynamicBuffer<DSPSampleBufferElement> dspBuffer, ref GrainProcessor grain) =>
            {
                if (!grain._SamplePopulated)
                {
                    float sourceIndex = grain._PlayheadNorm * grain._AudioClipDataComponent._ClipDataBlobAsset.Value.array.Length;
                    float increment = grain._Pitch;

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

                        sampleOutputBuffer.Add(new GrainSampleBufferElement { Value = sourceValue });
                        dspBuffer.Add(new DSPSampleBufferElement { Value = 0 });
                    }

                    // --Add additional samples to increase grain playback size based on DSP effect tail length
                    for (int i = 0; i < grain._DSPEffectSampleTailLength; i++)
                    {
                        sampleOutputBuffer.Add(new GrainSampleBufferElement { Value = 0 });
                        dspBuffer.Add(new DSPSampleBufferElement { Value = 0 });
                    }

                    grain._SamplePopulated = true; // TODO - SWAP THIS TO A TAG COMPONENT TO STOP HAVING TO USE GRAINM PROCESSOR AS A REF INPUT AND AVOID IF STATEMENTS IN THIS AND DSP JOB
                }
            }
        ).ScheduleParallel(emitBurst);



        //----    DSP CHAIN
        JobHandle dspGrains = Entities.ForEach
        (
           (DynamicBuffer<DSPParametersElement> dspParamsBuffer, DynamicBuffer<GrainSampleBufferElement> sampleOutputBuffer, DynamicBuffer < DSPSampleBufferElement > dspBuffer, ref GrainProcessor grain) =>
           {
               if (grain._SamplePopulated)
               {
                   for (int i = 0; i < dspParamsBuffer.Length; i++)
                   {
                       switch (dspParamsBuffer[i]._DSPType)
                       {
                           case DSPTypes.Bitcrush:
                               DSP_Bitcrush.ProcessDSP(dspParamsBuffer[i], sampleOutputBuffer, dspBuffer);
                               break;
                           case DSPTypes.Delay:
                               break;
                           case DSPTypes.Flange:
                               DSP_Flange.ProcessDSP(dspParamsBuffer[i], sampleOutputBuffer, dspBuffer);
                               break;
                           case DSPTypes.Filter:
                               DSP_Filter.ProcessDSP(dspParamsBuffer[i], sampleOutputBuffer, dspBuffer);
                               break;
                           case DSPTypes.Chopper:
                               DSP_Chopper.ProcessDSP(dspParamsBuffer[i], sampleOutputBuffer, dspBuffer);
                               break;
                       }
                   }
               }
           }
        ).ScheduleParallel(processGrains);
        #endregion



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

    public static float Map(float val, float inMin, float inMax, float outMin, float outMax, float exp)
    {
        return Mathf.Pow((val - inMin) * (inMax - inMin), exp) * (outMax - outMin) + outMin;
    }
}


[UpdateAfter(typeof(GrainSynthSystem))]
public class GrainsToAudioBuffersSystem : SystemBase
{
    protected override void OnCreate()
    {

    }

    protected override void OnUpdate()
    {
        ////----    FILL GRAIN SPEAKER ROLLING BUFFERS
        //NativeArray<Entity> grainEnts = GetEntityQuery(typeof(GrainProcessor), typeof(DynamicBuffer<GrainSampleBufferElement>)).ToEntityArray(Allocator.TempJob);
        //NativeArray<GrainProcessor> grains = GetEntityQuery(typeof(GrainProcessor)).ToComponentDataArray<GrainProcessor>(Allocator.TempJob);

        //Entities.WithName("FillSpeakerRingBuffer").ForEach
        //(
        //   (ref DynamicBuffer<AudioRingBufferElement> ringBuffer, ref RingBufferFiller ringBufferFiller, in GrainSpeakerComponent grainSpeaker) =>
        //   {
        //       BufferFromEntity<GrainSampleBufferElement> bufferLookup = GetBufferFromEntity<GrainSampleBufferElement>(true);

        //       //--  Fill buffer with grains
        //       int startIndex = int.MaxValue;
        //       int endIndexUnclamped = 0;

        //       for (int i = 0; i < grainEnts.Length; i++)
        //       {
        //           Entity grainEnt = grainEnts[i];

        //           if (!bufferLookup.HasComponent(grainEnt))
        //               return;
                                    
        //           DynamicBuffer <GrainSampleBufferElement> grainSampleBuffer = bufferLookup[grainEnt];

        //           //--  If the grain is routing to this speaker
        //           if (grains[i]._SpeakerIndex == grainSpeaker._SpeakerIndex && grains[i]._SamplePopulated && grainSampleBuffer.Length > 0)
        //           {
        //               //-- Update values for the ring buffer filler
        //               startIndex = math.min(startIndex, grains[i]._DSPStartIndex);
        //               endIndexUnclamped = math.max(endIndexUnclamped, grains[i]._DSPStartIndex + grains[i]._SampleCount);

        //               //Debug.Log("ring buffer sample count: " + ringBufferFiller._SampleCount + "  grainSampleBuffer:  " + grainSampleBuffer.Length + "   Audio buffer length: " + ringBuffer.Length);

        //               //--  Fill ring buffer from grain samples
        //               for (int s = 0; s < grainSampleBuffer.Length; s++)
        //               {
        //                   float grainSampleVal = grainSampleBuffer[s].Value;

        //                   int ringBuffIndex = (grains[i]._DSPStartIndex + s) % (ringBuffer.Length - 1);
        //                   float ringBufferVal = ringBuffer[ringBuffIndex].Value;
        //                   ringBuffer[ringBuffIndex] = new AudioRingBufferElement { Value = ringBufferVal + grainSampleVal };
        //               }
        //           }
        //       }







        //       //-- Clear previous samples
        //       //int prevSampleCount = ringBufferFiller._SampleCount;
        //       //int prevStartIndex = ringBufferFiller._StartIndex;
        //       //ringBufferFiller._StartIndex = startIndex;
        //       //ringBufferFiller._EndIndex = endIndexUnclamped % ringBuffer.Length;
        //       //ringBufferFiller._SampleCount = endIndexUnclamped - startIndex;

        //       //for (int s = 0; s < prevSampleCount; s++)
        //       //{
        //       //    int index = (prevStartIndex + s) % ringBuffer.Length;
        //       //    ringBuffer[s] = new AudioRingBufferElement { Value = 0 };
        //       //}
        //   }
        //).WithDisposeOnCompletion(grainEnts)
        ////.WithDisposeOnCompletion(grains)
        //.ScheduleParallel();
    }
}
