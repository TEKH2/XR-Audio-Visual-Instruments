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


[UpdateAfter(typeof(GrainSynthSystem))]
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
