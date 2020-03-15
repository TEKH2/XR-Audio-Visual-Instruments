using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

public class TargetingSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        // *** Note the dynamic buffer isnt ref'd ***
        Entities.ForEach((ref Translation playerTranslation, DynamicBuffer<TargetElement> targetDynamicBuffer) =>
        {
            // Get player pos
            float3 playerPos = playerTranslation.Value;

            // Clear buffer
            targetDynamicBuffer.Clear();

            // Create list of target entities
            NativeList<Entity> targetEntityList = new NativeList<Entity>(Allocator.Temp);

            // Add all targets within range to the list
            Entities.WithAll<Tag_Target>().ForEach((Entity targetEntity, ref Translation targetTranslation) =>
            {
                float targetRange = 12f;
                float targetDistance = math.distance(playerPos, targetTranslation.Value);

                if(targetDistance < targetRange)
                {
                    targetEntityList.Add(targetEntity);
                }
            });
            
            // Iterate through the list and fill the first 5 (buffer capacity) with targets
            foreach(Entity targetEntity in targetEntityList)
            {
                if(targetDynamicBuffer.Length < 5)
                {
                    targetDynamicBuffer.Add(new TargetElement { targetEntity = targetEntity });
                }
            }

            // Dispose of the native list
            targetEntityList.Dispose();
        });
    }
}
