using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Collections;
using System.Linq;
using YamlDotNet.Core;
using Unity.Jobs;

// https://docs.unity3d.com/Packages/com.unity.entities@0.13/api/

[UpdateAfter(typeof(DOTS_QuadrantSystem))]
//------  Check which emitters and speakers are within the range of the listener
public class RangeCheckSystem : SystemBase
{
    protected override void OnUpdate()
    {
        SpeakerManagerComponent speakerManager = GetSingleton<SpeakerManagerComponent>();

        //----    EMITTERS RANGE CHECK
        JobHandle emitterRangeCheck = Entities.WithoutBurst().WithName("emitterRangeCheck").ForEach((ref EmitterComponent emitter, in Translation trans) =>
        {
            float dist = math.distance(trans.Value, speakerManager._ListenerPos);
            bool inRangeCurrent = dist < speakerManager._EmitterToListenerActivationRange;

            //--  If moving out of range, deactive
            if (emitter._InRange && !inRangeCurrent)
            {
                emitter._AttachedToSpeaker = false;
                emitter._InRange = false;
                emitter._SpeakerIndex = int.MaxValue;
            }
            //--  If moving into range
            else if (!emitter._InRange && inRangeCurrent)
            {
                emitter._InRange = true;
            }
        }).ScheduleParallel(this.Dependency);



        //----    SPEAKERS OUT OF RANGE CHECK
        JobHandle speakerRangeCheck = Entities.WithName("speakerRangeCheck").ForEach((ref PooledObjectComponent poolObj, in GrainSpeakerComponent speaker, in Translation trans ) =>
        {
            bool inRangeCurrent = math.distance(trans.Value, speakerManager._ListenerPos) > speakerManager._EmitterToListenerActivationRange;

            //--  If moving out of range
            if (poolObj._State == PooledObjectState.Active && !inRangeCurrent)
            {
                poolObj._State = PooledObjectState.Pooled;
            }
        }).ScheduleParallel(this.Dependency);



        //----     FIND ACTIVE SPEAKERS IN RANGE
        JobHandle rangeCheckDeps = JobHandle.CombineDependencies(emitterRangeCheck, speakerRangeCheck);

        DSPTimerComponent dspTimer = GetSingleton<DSPTimerComponent>();
        NativeArray<Entity> speakerEnts = GetEntityQuery(typeof(GrainSpeakerComponent), typeof(Translation)).ToEntityArray(Allocator.TempJob);

        JobHandle activeSpeakersInRange = Entities.WithName("activeSpeakersInRange").ForEach((ref EmitterComponent emitter, in Translation emitterTrans) =>
        {
            // if emitter is in range and not attached to a speaker then look for closest
            if (emitter._InRange && !emitter._AttachedToSpeaker)
            {
                float closestDist = float.MaxValue;
                int closestSpeakerIndex = int.MaxValue;

                for (int i = 0; i < speakerEnts.Length; i++)
                {
                    if (GetComponent<PooledObjectComponent>(speakerEnts[i])._State == PooledObjectState.Active)
                    {
                        float dist = math.distance(emitterTrans.Value, GetComponent<Translation>(speakerEnts[i]).Value);
                        if (dist < speakerManager._EmitterToSpeakerAttachRadius)
                        {
                            closestDist = dist;
                            closestSpeakerIndex = GetComponent<GrainSpeakerComponent>(speakerEnts[i])._SpeakerIndex;
                        }
                    }
                }

                // If speaker in range is found then attach emitter to speaker
                if (closestSpeakerIndex != int.MaxValue)
                {
                    emitter._AttachedToSpeaker = true;
                    emitter._SpeakerIndex = closestSpeakerIndex;
                    emitter._LastGrainEmissionDSPIndex = dspTimer._CurrentDSPSample;
                } 
            }
        }).WithDisposeOnCompletion(speakerEnts)
        .ScheduleParallel(rangeCheckDeps);



        ////----     IF THERE ARE EMITTERS WITHOUT A SPEAKER, SPAWN A POOLED SPEAKER ON AN EMITTER IN RANGE    
        //EntityQuery emitterQuery = GetEntityQuery(typeof(EmitterComponent));
        //NativeArray<Entity> emitterEnts = emitterQuery.ToEntityArray(Allocator.TempJob);
        //NativeArray<EmitterComponent> emitters = GetEntityQuery(typeof(EmitterComponent)).ToComponentDataArray<EmitterComponent>(Allocator.TempJob);

        //EntityQuery speakerQuery = GetEntityQuery(typeof(GrainSpeakerComponent), typeof(PooledObjectComponent));
        //NativeArray<Entity> pooledSpeakerEnts = speakerQuery.ToEntityArray(Allocator.TempJob);
        //NativeArray<PooledObjectComponent> pooledSpeakerStates = speakerQuery.ToComponentDataArray<PooledObjectComponent>(Allocator.TempJob);


        //JobHandle speakerActivation = Job.WithName("speakerActivation").WithoutBurst().WithCode(() =>
        //{
        //    bool spawned = false;

        //    // Look through each speaker to see if it's pooled
        //    for (int s = 0; s < pooledSpeakerStates.Length; s++)
        //    {
        //        if (pooledSpeakerStates[s]._State == PooledObjectState.Pooled && !spawned)
        //        {
        //            // Look through all emitters to find one in range but not attached to a speaker
        //            for (int e = 0; e < emitters.Length; e++)
        //            {
        //                if (emitters[e]._InRange && !emitters[e]._AttachedToSpeaker)
        //                {
        //                    // Set emitter component
        //                    EmitterComponent emitter = GetComponent<EmitterComponent>(emitterEnts[e]);
        //                    emitter._AttachedToSpeaker = false;
        //                    emitter._InRange = false;
        //                    emitter._SpeakerIndex = int.MaxValue;
        //                    SetComponent<EmitterComponent>(emitterEnts[e], emitter);


        //                    // Set speaker pooled component
        //                    PooledObjectComponent pooledObj = GetComponent<PooledObjectComponent>(pooledSpeakerEnts[s]);
        //                    pooledObj._State = PooledObjectState.Active;
        //                    SetComponent<PooledObjectComponent>(pooledSpeakerEnts[s], pooledObj);

        //                    spawned = true;
        //                }
        //            }
        //        }
        //    }
        //}).WithDisposeOnCompletion(pooledSpeakerEnts).WithDisposeOnCompletion(pooledSpeakerStates)
        //.WithDisposeOnCompletion(emitterEnts).WithDisposeOnCompletion(emitters)
        //.Schedule(activeSpeakersInRange);


        //this.Dependency = speakerActivation;
        this.Dependency = activeSpeakersInRange;
    }
}