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

        EntityQueryDesc speakerQueryDesc = new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(GrainSpeakerComponent), typeof(PooledObjectComponent), typeof(Translation) }
        };

        EntityQuery speakerQuery = GetEntityQuery(speakerQueryDesc);
        NativeArray<PooledObjectComponent> pooledSpeakers = speakerQuery.ToComponentDataArray<PooledObjectComponent>(Allocator.TempJob);
        NativeArray<Translation> speakerTranslations = speakerQuery.ToComponentDataArray<Translation>(Allocator.TempJob);

        //Debug.Log("pooledSpeakers count: " + pooledSpeakers.Length);

        //----    EMITTERS RANGE CHECK
        JobHandle emitterRangeCheck = Entities.WithName("emitterRangeCheck").ForEach((ref EmitterComponent emitter, in Translation trans) =>
        {
            float distToListener = math.distance(trans.Value, speakerManager._ListenerPos);
            bool inRangeCurrent = distToListener < speakerManager._EmitterToListenerActivationRange;



            //--  MOVING OUT OF RANGE, DEACTIVE EMITTER
            if (emitter._InRange && emitter._AttachedToSpeaker && !inRangeCurrent)
            {
                DetachEmitter(emitter);
                //emitter._AttachedToSpeaker = false;
                //emitter._InRange = false;
                //emitter._SpeakerIndex = int.MaxValue;
            }
            //--  MOVING INTO RANGE
            else if (!emitter._InRange && inRangeCurrent)
            {
                emitter._InRange = true;
            }



            //---  OUT OF RANGE TO SPEAKER OR SPEAKER HAS BEEN DEACTIVATED - DETACH EMITTER
            if(emitter._AttachedToSpeaker && !emitter._StaticallyPaired)
            {
                float distToSpeaker = math.distance(trans.Value, speakerTranslations[emitter._SpeakerIndex].Value);
                if (pooledSpeakers[emitter._SpeakerIndex]._State == PooledObjectState.Pooled || distToSpeaker > speakerManager._EmitterToSpeakerAttachRadius)
                {
                    DetachEmitter(emitter);
                    //emitter._AttachedToSpeaker = false;
                    //emitter._InRange = false;
                    //emitter._SpeakerIndex = int.MaxValue;
                }
            }
        }).WithDisposeOnCompletion(pooledSpeakers)
        .WithDisposeOnCompletion(speakerTranslations)
        .ScheduleParallel(this.Dependency);



        //----    SPEAKERS OUT OF RANGE CHECK
        JobHandle speakerRangeCheck = Entities.WithName("speakerRangeCheck").ForEach((ref PooledObjectComponent poolObj, in GrainSpeakerComponent speaker, in Translation trans ) =>
        {
            float dist = math.distance(trans.Value, speakerManager._ListenerPos);
            bool inRangeCurrent = dist < speakerManager._EmitterToListenerActivationRange;

            //Debug.Log("In range: " + inRangeCurrent + "  Dist: " + dist + " < " + speakerManager._EmitterToListenerActivationRange);

            //--  If moving out of range
            if (poolObj._State == PooledObjectState.Active && !inRangeCurrent)
            {
                poolObj._State = PooledObjectState.Pooled;
            }
        }).ScheduleParallel(this.Dependency);



        //----     FIND ACTIVE SPEAKERS IN RANGE
        JobHandle rangeCheckDeps = JobHandle.CombineDependencies(emitterRangeCheck, speakerRangeCheck);
        NativeArray<Entity> speakerEnts = GetEntityQuery(typeof(GrainSpeakerComponent), typeof(Translation)).ToEntityArray(Allocator.TempJob);
        DSPTimerComponent dspTimer = GetSingleton<DSPTimerComponent>();       

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



        //----     IF THERE ARE EMITTERS WITHOUT A SPEAKER, SPAWN A POOLED SPEAKER ON AN EMITTER IN RANGE    
        EntityQuery emitterQuery = GetEntityQuery(typeof(EmitterComponent));
        NativeArray<Entity> emitterEnts = emitterQuery.ToEntityArray(Allocator.TempJob);
        NativeArray<EmitterComponent> emitters = GetEntityQuery(typeof(EmitterComponent)).ToComponentDataArray<EmitterComponent>(Allocator.TempJob);

        
        NativeArray<Entity> speakerEntities = speakerQuery.ToEntityArray(Allocator.TempJob);
        NativeArray<PooledObjectComponent> pooledSpeakerStates = speakerQuery.ToComponentDataArray<PooledObjectComponent>(Allocator.TempJob);        

        JobHandle speakerActivation = Job.WithName("speakerActivation").WithoutBurst().WithCode(() =>
        {
            bool spawned = false;

            // Look through each speaker to see if it's pooled
            for (int s = 0; s < pooledSpeakerStates.Length; s++)
            {
                if (pooledSpeakerStates[s]._State == PooledObjectState.Pooled && !spawned)
                {
                    // Look through all emitters to find one in range but not attached to a speaker
                    for (int e = 0; e < emitters.Length; e++)
                    {
                        if (emitters[e]._InRange && !emitters[e]._AttachedToSpeaker && !spawned)
                        {
                            spawned = true;

                            int speakerIndex = GetComponent<GrainSpeakerComponent>(speakerEntities[s])._SpeakerIndex;
                            float3 speakerPos = GetComponent<Translation>(emitterEnts[e]).Value;

                            // Set emitter component
                            EmitterComponent emitter = GetComponent<EmitterComponent>(emitterEnts[e]);
                            emitter._AttachedToSpeaker = true;
                            emitter._SpeakerIndex = GetComponent<GrainSpeakerComponent>(speakerEntities[s])._SpeakerIndex;
                            emitter._LastGrainEmissionDSPIndex = dspTimer._CurrentDSPSample;
                            SetComponent<EmitterComponent>(emitterEnts[e], emitter);


                            // Set speaker translation
                            Translation speakerTrans = GetComponent<Translation>(speakerEntities[s]);
                            speakerTrans.Value = GetComponent<Translation>(emitterEnts[e]).Value;
                            SetComponent<Translation>(speakerEntities[s], speakerTrans);


                            // Set speaker pooled component
                            PooledObjectComponent pooledObj = GetComponent<PooledObjectComponent>(speakerEntities[s]);
                            pooledObj._State = PooledObjectState.Active;
                            SetComponent<PooledObjectComponent>(speakerEntities[s], pooledObj);

                            //Debug.Log("Speaker placed - Index: " + speakerIndex + " @ " + speakerPos);
                           
                            return;
                        }
                    }
                }
            }
        }).WithDisposeOnCompletion(speakerEntities).WithDisposeOnCompletion(pooledSpeakerStates)
        .WithDisposeOnCompletion(emitterEnts).WithDisposeOnCompletion(emitters)
        .Schedule(activeSpeakersInRange);


        this.Dependency = speakerActivation;
    }

    public static void DetachEmitter(EmitterComponent emitter)
    {
        emitter._AttachedToSpeaker = false;
        emitter._InRange = false;
        emitter._SpeakerIndex = int.MaxValue;
    }
}