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
        JobHandle emitterRangeCheck = Entities.WithName("emitterRangeCheck").ForEach((ref EmitterComponent emitter, in Translation trans) =>
        {
            bool inRangeCurrent = math.distance(trans.Value, speakerManager._ListenerPos) < speakerManager._EmitterToListenerActivationRange;

            //--  If moving out of range
            if (emitter._InRange && !inRangeCurrent)
            {
                DeactivateEmitter(emitter);
            }
            //--  If moving into range
            else if (!emitter._InRange && inRangeCurrent)
            {
                EmitterInRange(emitter);
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

        JobHandle rangeCheckDeps = JobHandle.CombineDependencies(emitterRangeCheck, speakerRangeCheck);


        //----     FIND ACTIVE SPEAKERS IN RANGE
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

                // If speaker in range is found then attach
                if (closestSpeakerIndex != int.MaxValue)                
                    AssignEmitterToSpeaker(emitter, closestSpeakerIndex, dspTimer._CurrentDSPSample);     
            }
        }).WithDisposeOnCompletion(speakerEnts)
        .ScheduleParallel(rangeCheckDeps);

        this.Dependency = activeSpeakersInRange;
    }

     static void DeactivateEmitter(EmitterComponent emitter)
    {
        emitter._Playing = false;
        emitter._AttachedToSpeaker = false;
        emitter._InRange = false;
        emitter._SpeakerIndex = int.MaxValue;
    }

    static void EmitterInRange(EmitterComponent emitter)
    {
        emitter._InRange = false;
    }

    public static void AssignEmitterToSpeaker(EmitterComponent emitter, int speakerIndex, int dspTimer)
    {
        emitter._Playing = true;
        emitter._AttachedToSpeaker = true;
        emitter._SpeakerIndex = speakerIndex;
        emitter._LastGrainEmissionDSPIndex = dspTimer;
    }
}


[UpdateAfter(typeof(RangeCheckSystem))]
public class SpawnSpeakerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        //----     IF THERE ARE EMITTERS WITHOUT A SPEAKER, SPAWN A POOLED SPEAKER ON AN EMITTER IN RANGE
        DSPTimerComponent dspTimer = GetSingleton<DSPTimerComponent>();
        //NativeArray<EmitterComponent> emitters = GetEntityQuery(typeof(EmitterComponent), typeof(Translation)).ToComponentDataArray<EmitterComponent>(Allocator.TempJob);

        //bool spawned = false;
        //Entities.ForEach((ref PooledObjectComponent speakerPooled, in GrainSpeakerComponent speaker, in Translation trans) =>
        //{
        //    if (speakerPooled._State == PooledObjectState.Pooled && !spawned)
        //    {
        //        for (int i = 0; i < emitters.Length; i++)
        //        {
        //            if (emitters[i]._InRange && !emitters[i]._AttachedToSpeaker)
        //            {
        //                RangeCheckSystem.AssignEmitterToSpeaker(emitters[i], speaker._SpeakerIndex, dspTimer._CurrentDSPSample);
        //                spawned = true;
        //                return;
        //            }
        //        }
        //    }
        //}).Schedule();
    }
}


[UpdateAfter(typeof(RangeCheckSystem))]
public class SpeakerEmitterPairingSystem : SystemBase
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

        //SpeakerManagerComponent speakerManager = GetSingleton<SpeakerManagerComponent>();
        //DSPTimerComponent dspTimer = GetSingleton<DSPTimerComponent>();

        //// ----    PAIR IN RANGE EMITTERS WITH IN RANGE SPEAKERS
        //EntityQuery activeSpeakerQuery = GetEntityQuery(typeof(GrainSpeakerComponent), typeof(InListenerRangeTag), typeof(Translation));
        //NativeArray<GrainSpeakerComponent> activeSpeakers = activeSpeakerQuery.ToComponentDataArray<GrainSpeakerComponent>(Allocator.TempJob);
        //NativeArray<Translation> activeSpeakersTrans = activeSpeakerQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
      
        //Entities.ForEach((int entityInQueryIndex, Entity entity, ref EmitterComponent emitter, in Translation emitterTrans, in InListenerRangeTag inRange) =>
        //{
        //    float closestDist = float.MaxValue;
        //    int closestIndex = int.MaxValue;

        //    for (int i = 0; i < activeSpeakers.Length; i++)
        //    {
        //        float dist = math.distance(emitterTrans.Value, activeSpeakersTrans[i].Value);
        //        if (dist < speakerManager._EmitterToSpeakerAttachRadius)
        //        {
        //            closestDist = dist;
        //            closestIndex = i;
        //        }
        //    }

        //    // -- If an active speaker has been found within range
        //    if (closestIndex != int.MaxValue)
        //    {
        //        emitter._Playing = true;
        //        emitter._AttachedToSpeaker = true;
        //        emitter._SpeakerIndex = closestIndex;
        //        emitter._LastGrainEmissionDSPIndex = dspTimer._CurrentDSPSample;
        //    }
        //}).WithDisposeOnCompletion(activeSpeakers)
        //.WithDisposeOnCompletion(activeSpeakersTrans)
        //.ScheduleParallel();



        //// ----    PAIR IN RANGE EMITTER WITH INACTIVE SPEAKER
        //var queryDesc = new EntityQueryDesc
        //{
        //    None = new ComponentType[] { typeof(InListenerRangeTag) },
        //    All = new ComponentType[] { typeof(GrainSpeakerComponent), ComponentType.ReadOnly<Translation>() }
        //};
        //EntityQuery query = GetEntityQuery(queryDesc);
        //NativeArray<Entity> inactiveSpeakerEnts = query.ToEntityArray(Allocator.TempJob);
        //NativeArray<GrainSpeakerComponent> inactiveSpeakers = query.ToComponentDataArray<GrainSpeakerComponent>(Allocator.TempJob);
        //int count = 0;

        //Entities.ForEach((int entityInQueryIndex, Entity entity, ref EmitterComponent emitter, in Translation emitterTrans, in InListenerRangeTag inRange) =>
        //{           
        //    if (!emitter._AttachedToSpeaker && inactiveSpeakers.Length > 0 && count == 0)
        //    { 
        //        emitter._Playing = true;
        //        emitter._AttachedToSpeaker = true;
        //        emitter._SpeakerIndex = inactiveSpeakers[0]._SpeakerIndex;
        //        emitter._LastGrainEmissionDSPIndex = dspTimer._CurrentDSPSample;

        //        entityCommandBuffer.SetComponent<Translation>(entityInQueryIndex, inactiveSpeakerEnts[0], new Translation { Value = emitterTrans.Value });
        //        entityCommandBuffer.AddComponent<InListenerRangeTag>(entityInQueryIndex, inactiveSpeakerEnts[0], new InListenerRangeTag { });

        //        //Debug.Log("index: " + emitter._SpeakerIndex);
        //    }

        //}).WithDisposeOnCompletion(inactiveSpeakers)
        //.WithDisposeOnCompletion(inactiveSpeakerEnts)
        //.ScheduleParallel();



        //// Make sure that the ECB system knows about our job
        //_CommandBufferSystem.AddJobHandleForProducer(Dependency);



        //Debug.LogError("Pausing after Speaker emitter pairing " + GetEntityQuery(typeof(InListenerRangeTag)).CalculateEntityCount());











      
    }

    public static void AttachEmitter(EmitterComponent emitter, int index)
    {
        emitter._Playing = true;
        emitter._AttachedToSpeaker = true;
        emitter._SpeakerIndex = index;
    }

    public static void DetachEmitter(EmitterComponent emitter)
    {
        emitter._Playing = false;
        emitter._AttachedToSpeaker = false;
        emitter._SpeakerIndex = int.MaxValue;
    }
}


