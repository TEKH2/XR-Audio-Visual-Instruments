using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Physics.Systems;
using Unity.Burst;

[UpdateBefore(typeof(BuildPhysicsWorld))]
public class AttractSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        NativeArray<Entity> attractorEntities = GetEntityQuery(typeof(AttractComponent), typeof(Translation)).ToEntityArray(Allocator.TempJob);

        JobHandle attractJobHandle = new AttractJob
        {
            attractorEntities = attractorEntities,
            attractComponents = GetComponentDataFromEntity<AttractComponent>(true),
            attractTranslationComponents = GetComponentDataFromEntity<Translation>(true),
        }.Schedule(this, inputDeps);

        attractJobHandle.Complete();

        attractorEntities.Dispose();

        return attractJobHandle;
    }

    [BurstCompile]
    struct AttractJob : IJobForEach<PhysicsVelocity, Translation>
    {
        [ReadOnly] public NativeArray<Entity> attractorEntities;
        [ReadOnly] public ComponentDataFromEntity<AttractComponent> attractComponents;
        [ReadOnly] public ComponentDataFromEntity<Translation> attractTranslationComponents;

        public void Execute(ref PhysicsVelocity physVelComponent, [ReadOnly] ref Translation attractableTranslation)
        {
            float3 attraction = new float3(0, 0, 0);
            foreach (var entity in attractorEntities)
            {
                AttractComponent attractorComp = attractComponents[entity];
                Translation attractorTranslation = attractTranslationComponents[entity];

                Debug.Log(entity + "    " + attractorTranslation.Value);

                float3 vectorToAttractor = attractorTranslation.Value - attractableTranslation.Value;
                float distSqrd = math.lengthsq(vectorToAttractor);

                if (distSqrd < attractorComp._MaxDistanceSqrd)
                {
                    // Alter linear velocity
                    attraction += attractorComp._Strength * (vectorToAttractor / math.sqrt(distSqrd));
                }
            }

            physVelComponent.Linear += attraction;
        }
    }
};