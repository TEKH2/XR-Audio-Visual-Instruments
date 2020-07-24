using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Physics.Systems;
using Unity.Burst;

// Create a dynamic buffer of attractors
// Create a dynamic buffer of movers
// Job on both buffers at once


[UpdateBefore(typeof(BuildPhysicsWorld))]
public class AttractSystem : SystemBase
{
    protected override void OnUpdate()
    {
        EntityQuery entityQuery = GetEntityQuery(typeof(AttractComponent), typeof(Translation));
        NativeArray<AttractComponent> attractorComps = entityQuery.ToComponentDataArray<AttractComponent>(Allocator.TempJob);
        NativeArray<Translation> attractorTransComps = entityQuery.ToComponentDataArray<Translation>(Allocator.TempJob);

        // Update cube volume effect
        Entities.ForEach
        (
            (ref PhysicsVelocity physVelComponent, ref Translation translation) =>
            {                
                float3 attraction = new float3(0, 0, 0);
                for (int i = 0; i < attractorComps.Length; i++)
                {
                    float3 vectorToAttractor = attractorTransComps[i].Value - translation.Value;
                    float distSqrd = math.lengthsq(vectorToAttractor);

                    if (distSqrd < attractorComps[i]._MaxDistanceSqrd)
                    {
                        // Alter linear velocity
                        attraction += attractorComps[i]._Strength * (vectorToAttractor / math.sqrt(distSqrd));
                    }                    
                }

                physVelComponent.Linear += attraction;
            }
        ).WithDeallocateOnJobCompletion(attractorComps).WithDeallocateOnJobCompletion(attractorTransComps).ScheduleParallel();
    }

        //protected override JobHandle OnUpdate(JobHandle inputDeps)
        //{
        //    NativeArray<Entity> attractorEntities = GetEntityQuery(typeof(AttractComponent), typeof(Translation)).ToEntityArray(Allocator.TempJob);

        //    JobHandle attractJobHandle = new AttractJob
        //    {
        //        attractorEntities = attractorEntities,
        //        attractComponents = GetComponentDataFromEntity<AttractComponent>(true),
        //        attractTranslationComponents = GetComponentDataFromEntity<Translation>(true),
        //    }.Schedule(this, inputDeps);

        //    attractJobHandle.Complete();

        //    attractorEntities.Dispose();

        //    return attractJobHandle;
        //}

        //[BurstCompile]
        //struct AttractJob : IJobForEach<PhysicsVelocity, Translation>
        //{
        //    [ReadOnly] public NativeArray<Entity> attractorEntities;
        //    [ReadOnly] public ComponentDataFromEntity<AttractComponent> attractComponents;
        //    [ReadOnly] public ComponentDataFromEntity<Translation> attractTranslationComponents;

        //    public void Execute(ref PhysicsVelocity physVelComponent, [ReadOnly] ref Translation attractableTranslation)
        //    {
        //        float3 attraction = new float3(0, 0, 0);
        //        for (int i = 0; i < attractorEntities.Length; i++)
        //        {
        //            Entity entity = attractorEntities[i];
        //            AttractComponent attractorComp = attractComponents[entity];
        //            Translation attractorTranslation = attractTranslationComponents[entity];

        //            //Debug.Log(entity + "    " + attractorTranslation.Value);

        //            float3 vectorToAttractor = attractorTranslation.Value - attractableTranslation.Value;
        //            float distSqrd = math.lengthsq(vectorToAttractor);

        //            if (distSqrd < attractorComp._MaxDistanceSqrd)
        //            {
        //                // Alter linear velocity
        //                attraction += attractorComp._Strength * (vectorToAttractor / math.sqrt(distSqrd));
        //            }
        //        }

        //        physVelComponent.Linear += attraction;
        //    }
        //}
    };