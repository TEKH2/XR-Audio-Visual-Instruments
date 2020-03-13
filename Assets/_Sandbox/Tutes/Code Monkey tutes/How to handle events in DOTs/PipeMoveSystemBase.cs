using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using System;
using UnityEngine;

/*
public class PipeMoveSystemBase : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        float deltaTime = Time.DeltaTime;
        float3 moveDir = new float3(-1, 0, 0);
        float moveSpeed = 4;
       
        JobHandle jobHandle = Entities.ForEach((ref Translation translation, ref Pipe pipe) =>
        {
            float xBefore = translation.Value.x;
            translation.Value += moveDir * moveSpeed * deltaTime;
            float xAfter = translation.Value.x;

            if(xBefore > 0 && xAfter <= 0)
            {
                // Passed the player
                Debug.Log("Here");
            }

        }).Schedule(inputDeps);

        return jobHandle;
    }
}
*/
