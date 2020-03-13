using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using System;
using UnityEngine;

[DisableAutoCreation]
public class PipeMoveSystemV1 : JobComponentSystem
{
    public event EventHandler OnPipePassed;

    public struct PipePassedEvent { public int testInt; }
    private NativeQueue<PipePassedEvent> eventQueue;

    protected override void OnCreate()
    {
        eventQueue = new NativeQueue<PipePassedEvent>(Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        eventQueue.Dispose();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        float deltaTime = Time.DeltaTime;
        float3 moveDir = new float3(-1, 0, 0);
        float moveSpeed = 4;

        NativeQueue<PipePassedEvent>.ParallelWriter eventQueueParallelWriter = eventQueue.AsParallelWriter();

        JobHandle jobHandle = Entities.ForEach((ref Translation translation, ref Pipe pipe) =>
        {
            float xBefore = translation.Value.x;
            translation.Value += moveDir * moveSpeed * deltaTime;
            float xAfter = translation.Value.x;

            if (xBefore > 0 && xAfter <= 0)
            {
                // Passed the player
                //Debug.Log("Here");
                eventQueueParallelWriter.Enqueue(new PipePassedEvent { testInt = 2 });
            }

        }).Schedule(inputDeps);

        jobHandle.Complete();

        while (eventQueue.TryDequeue(out PipePassedEvent pipePassedEvent))
        {
            OnPipePassed?.Invoke(this, EventArgs.Empty);
        }

        return jobHandle;
    }
}
