using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Rendering;
using Unity.Entities;
using Unity.Transforms;

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
