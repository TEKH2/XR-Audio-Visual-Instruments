using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Rendering;
using Unity.Entities;
using Unity.Transforms;

/*
public class DOTS_MoveToTargetSystem : JobComponentSystem
{
    // The command buffer system that runs after the main simulation
    private EndSimulationEntityCommandBufferSystem _EndSimCommandBufferSys;

    protected override void OnCreate()
    {
        // get ref to command buffer system on create
        _EndSimCommandBufferSys = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        base.OnCreate();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        MoveToTargetJob addComponentJob = new MoveToTargetJob
        {
            _EndSimCommandBufferSys = _EndSimCommandBufferSys.CreateCommandBuffer().ToConcurrent()
        };

        // schedule to when first job is finished by passing in job handle
        JobHandle jobHandle = addComponentJob.Schedule(this, inputDeps);

        _EndSimCommandBufferSys.AddJobHandleForProducer(jobHandle);

        return jobHandle;

    }


    [RequireComponentTag(typeof(Unit), typeof(HasTarget))]
    public struct MoveToTargetJob: IJobForEachWithEntity<Translation, HasTarget>
    {
        // The command buffer system that runs after the main simulation
        public EntityCommandBuffer.Concurrent _EndSimCommandBufferSys;

        public void Execute(Entity entity, int index, ref Translation translation, [ReadOnly] ref HasTarget hasTarget)
        {
            if (World.Active.EntityManager.Exists(hasTarget._TargetEntity))
            {
                Translation targetTrans = World.Active.EntityManager.GetComponentData<Translation>(hasTarget._TargetEntity);

                float3 targetDir = math.normalize(targetTrans.Value - trans.Value);
                float moveSpeed = 5f;
                translation.Value += targetDir * moveSpeed * Time.deltaTime;

                if (math.distance(trans.Value, targetTrans.Value) < .2f)
                {
                    // Close to target, destroy it and remove has target component
                    _EndSimCommandBufferSys.DestroyEntity(index, hasTarget._TargetEntity);
                    _EndSimCommandBufferSys.RemoveComponent(index, entity, typeof(HasTarget));
                }
            }
            else
            {
                // Remove componenet bc target entity is already destroyed
                _EndSimCommandBufferSys.RemoveComponent(index, entity, typeof(HasTarget));
            }
        }
    }
}
*/


// To turn into a job would need to store the translation of the target so I dont have to use the
// World.Active.EntityManager.GetComponentData<Translation>(hasTarget._TargetEntity); call
public class DOTS_MoveToTargetSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach
        (
            (Entity unitEntity, ref HasTarget hasTarget, ref Translation trans) =>
            {
                if (World.Active.EntityManager.Exists(hasTarget._TargetEntity))
                {
                    Translation targetTrans = World.Active.EntityManager.GetComponentData<Translation>(hasTarget._TargetEntity);

                    float3 targetDir = math.normalize(targetTrans.Value - trans.Value);
                    float moveSpeed = 5f;
                    trans.Value += targetDir * moveSpeed * Time.deltaTime;

                    if (math.distance(trans.Value, targetTrans.Value) < .2f)
                    {
                        // Close to target, destroy it and remove has target component
                        PostUpdateCommands.DestroyEntity(hasTarget._TargetEntity);
                        PostUpdateCommands.RemoveComponent(unitEntity, typeof(HasTarget));
                    }
                }
                else
                {
                    // Remove componenet bc target entity is already destroyed
                    PostUpdateCommands.RemoveComponent(unitEntity, typeof(HasTarget));
                }
            }
        );
    }
}

