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
        //NativeArray<Entity> attractorEntities = GetEntityQuery(typeof(AttractComponent), typeof(Translation)).ToEntityArray(Allocator.TempJob);

        inputDeps = new AttractJob2
        {
            //attractorEntities = attractorEntities,
            //attractComponents = GetComponentDataFromEntity<AttractComponent>(),
            //attractTranslationComponents = GetComponentDataFromEntity<Translation>(),
        }.Schedule(this, inputDeps);

        inputDeps.Complete();

        //attractorEntities.Dispose();

        return inputDeps;
    }

    [BurstCompile]
    struct AttractJob2 : IJobForEach<PhysicsVelocity, Translation>
    {
        //[ReadOnly] public NativeArray<Entity> attractorEntities;
        //[ReadOnly] public ComponentDataFromEntity<AttractComponent> attractComponents;
        //[ReadOnly] public ComponentDataFromEntity<Translation> attractTranslationComponents;

        public void Execute(ref PhysicsVelocity physVelComponent, [ReadOnly] ref Translation attractableTranslation)
        {
            physVelComponent.Linear = new float3(0, .4f, 0);
            //foreach (var entity in attractorEntities)
            //{
            //    AttractComponent attractorComp = attractComponents[entity];
            //    Translation attractorTranslation = attractTranslationComponents[entity];

            //    float3 vectorToAttractor = attractorTranslation.Value - attractableTranslation.Value;
            //    float distSqrd = math.lengthsq(vectorToAttractor);

            //    if (distSqrd < attractorComp._MaxDistanceSqrd)
            //    {
            //        // Alter linear velocity
            //        physVelComponent.Linear += attractorComp._Strength * (vectorToAttractor / math.sqrt(distSqrd));
            //    }
            //}
        }
    }


    struct AttractJob : IJobForEach<Translation, AttractComponent>
    {
        public NativeArray<Entity> attractableEntities;
        public ComponentDataFromEntity<PhysicsVelocity> physVelComponents;
        public ComponentDataFromEntity<Translation> transComponents;

        public void Execute(ref Translation attractorTranslation, ref AttractComponent attract)
        {
            foreach (var entity in attractableEntities)
            {
                PhysicsVelocity physVelComponent = physVelComponents[entity];
                Translation attractableTranslation = transComponents[entity];

                float3 vectorToAttractor = attractorTranslation.Value - attractableTranslation.Value;
                float distSqrd = math.lengthsq(vectorToAttractor);

                if (distSqrd < attract._MaxDistanceSqrd)
                {
                    // Alter linear velocity
                    physVelComponent.Linear += attract._Strength * (vectorToAttractor / math.sqrt(distSqrd));
                }
            }
        }
    }
};