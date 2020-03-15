using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

public class AttackSystem : ComponentSystem
{
    private float _AttackTimer;

    protected override void OnUpdate()
    {
        _AttackTimer -= Time.DeltaTime;

        if(_AttackTimer <= 0)
        {
            // Attack
            _AttackTimer = .6f;

            float3 playerPos = float3.zero;

            Entities.ForEach((DynamicBuffer<TargetElement> targetDynamicBuffer) =>
            {
                for (int i = 0; i < targetDynamicBuffer.Length; i++)
                {
                    Entity targetEntity = targetDynamicBuffer[i].targetEntity;

                    // Check that the entity exists. 
                    // Not sure this needs to be done since we have found it in the targetting system
                    if(targetEntity != Entity.Null && EntityManager.Exists(targetEntity))
                    {
                        // Get the component data from the target entity
                        ComponentDataFromEntity<Translation> translationComponentData = GetComponentDataFromEntity<Translation>(true);
                        float3 targetPos = translationComponentData[targetEntity].Value;

                        // Spawn bullet entity and set component data

                    }
                }
            });
        }
    }
}
