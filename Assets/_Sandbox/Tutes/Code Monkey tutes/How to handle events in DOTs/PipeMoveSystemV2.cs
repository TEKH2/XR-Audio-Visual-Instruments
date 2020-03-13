using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;

public class PipeMoveSystemV2 : JobComponentSystem
{
    public event EventHandler OnPassedXEvent;

    // create a component to add to entities that fire the event
    public struct EventComponent : IComponentData
    {
        public double elapsedTime;
    }

    // create buffer to add the components so we dont break the job by calling the entity manager
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        float deltaTime = Time.DeltaTime;
        float3 moveDir = new float3(-1, 0, 0);
        float moveSpeed = 4;

        // create entity command buffer from teh command buffer system
        EntityCommandBuffer entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer();


        // Change the command buffer to concurrent so it can be used in the job
        EntityCommandBuffer.Concurrent entityCommandBufferConCurrent = entityCommandBuffer.ToConcurrent();
        // Create and archetype for our event entity
        EntityArchetype EventEntityArchetype = EntityManager.CreateArchetype(typeof(EventComponent));

        // Get elapsed time to pass into event
        double elapsedTime = Time.ElapsedTime;

        JobHandle jobHandle = Entities.ForEach((int entityInQueryIndex, ref Translation translation, ref Pipe pipe) => {
            float xBefore = translation.Value.x;
            translation.Value += moveDir * moveSpeed * deltaTime;
            float xAfter = translation.Value.x;

            if(xBefore > 0 && xAfter <= 0)
            {
               Entity e = entityCommandBufferConCurrent.CreateEntity(entityInQueryIndex, EventEntityArchetype);
               entityCommandBufferConCurrent.SetComponent(entityInQueryIndex, e, new EventComponent { elapsedTime = elapsedTime });
            }

        }).Schedule(inputDeps);

        //// COmplete job so command buffer can run
        //jobHandle.Complete();

        // Playback adn dispose of command buffer, which create teh entities with the event components
        //entityCommandBuffer.Playback(EntityManager);
        //entityCommandBuffer.Dispose();


        // Make sure that the command buffer only runs once this job has been completed
        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);

        EntityCommandBuffer captureEventsEntityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer();

        // Run through all the entities with event components and fire the event
        Entities.WithoutBurst().ForEach((Entity entity, ref EventComponent eventComponent) =>
        {
            Debug.Log(elapsedTime + "    " + eventComponent.elapsedTime);
            OnPassedXEvent?.Invoke(this, EventArgs.Empty);
            captureEventsEntityCommandBuffer.DestroyEntity(entity);
        }).Run();        

        return jobHandle;
    }
}
