using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;



public class ECS_Jobs_UnitMoveToTargetSystem : ComponentSystem
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
                    float moveSpeed = 3f;
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
