using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;


// Comment this in to test with a job component system
// 1000 units searching for 1000 targets - No burst - 32ms
// 1000 units searching for 1000 targets - With burst - 3.3ms
public class DOTS_FindTargetSystem : JobComponentSystem
{
    private struct EnitityWithPos
    {
        public Entity _Entity;
        public float3 _Pos;

        public EnitityWithPos(Entity e, float3 pos)
        {
            _Entity = e;
            _Pos = pos;
        }
    }

    // The command buffer system that runs after the main simulation
    private EndSimulationEntityCommandBufferSystem _EndSimCommandBufferSys;



    #region JOBS

    [RequireComponentTag(typeof(Unit))]
    [ExcludeComponent(typeof(HasTarget))]
    [BurstCompile]
    private struct FindTargetQuadrantSystemJob : IJobForEachWithEntity<Translation, QuadEntityType>
    {
        [ReadOnly] public NativeMultiHashMap<int, QuadData> _QuadMultiHashMap;
        public NativeArray<Entity> _ClosestTargetEntityArray;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation, [ReadOnly] ref QuadEntityType quadEntityType)
        {
            // Code running on all entities with 'tag' component unit
            float3 unitPos = translation.Value;

            Entity closestTargetEntity = Entity.Null;
            float closestDistance = float.MaxValue;

            int hashMapKey = DOTS_QuadrantSystem.GetPosHashMapKey(translation.Value);

            // Search own quad
            FindTarget(hashMapKey, translation.Value, quadEntityType, ref closestTargetEntity, ref closestDistance);

            // Search edge quads
            FindTarget(hashMapKey+1, translation.Value, quadEntityType, ref closestTargetEntity, ref closestDistance);
            FindTarget(hashMapKey-1, translation.Value, quadEntityType, ref closestTargetEntity, ref closestDistance);
            FindTarget(hashMapKey + DOTS_QuadrantSystem._QuadYHashMultiplier, translation.Value, quadEntityType, ref closestTargetEntity, ref closestDistance);
            FindTarget(hashMapKey - DOTS_QuadrantSystem._QuadYHashMultiplier, translation.Value, quadEntityType, ref closestTargetEntity, ref closestDistance);

            // Search corner quads
            FindTarget(hashMapKey + DOTS_QuadrantSystem._QuadYHashMultiplier + 1, translation.Value, quadEntityType, ref closestTargetEntity, ref closestDistance);
            FindTarget(hashMapKey + DOTS_QuadrantSystem._QuadYHashMultiplier - 1, translation.Value, quadEntityType, ref closestTargetEntity, ref closestDistance);
            FindTarget(hashMapKey - DOTS_QuadrantSystem._QuadYHashMultiplier + 1, translation.Value, quadEntityType, ref closestTargetEntity, ref closestDistance);
            FindTarget(hashMapKey - DOTS_QuadrantSystem._QuadYHashMultiplier - 1, translation.Value, quadEntityType, ref closestTargetEntity, ref closestDistance);
            
            _ClosestTargetEntityArray[index] = closestTargetEntity;
        }

        void FindTarget(int hashMapKey, float3 unitPos, QuadEntityType quadEntityType, ref Entity closestTargetEntity, ref float closestTargetDistance)
        {
            // Iterate through multi hash map
            QuadData quadData;
            NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
            if (_QuadMultiHashMap.TryGetFirstValue(hashMapKey, out quadData, out nativeMultiHashMapIterator))
            {
                do
                {
                    // Only look for target if it's of a different quad entity type
                    if (quadEntityType._Type != quadData._QuadEntityType._Type)
                    {
                        float dist = math.distance(unitPos, quadData._Pos);

                        // if closer then set closest target entity and distance
                        if (dist < closestTargetDistance)
                        {
                            closestTargetDistance = dist;
                            closestTargetEntity = quadData._Entity;
                        }
                    }
                }
                while (_QuadMultiHashMap.TryGetNextValue(out quadData, ref nativeMultiHashMapIterator));
            }
        }
    }


    /*
       // Entity command buffer can't run with burst so the jobs are seperated into 2 jobs
       // This just finds the targets and the second job adds the components using the EntityCommandBuffer after this job is finished
       [RequireComponentTag(typeof(Unit))]
       [ExcludeComponent(typeof(HasTarget))]
       [BurstCompile]
       private struct FindTargetBurstJob : IJobForEachWithEntity<Translation>
       {
           [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<EnitityWithPos> _TargetArray;
           public NativeArray<Entity> _ClosestTargetEntityArray;

           public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation)
           {
               // Code running on all entities with 'tag' component unit
               //Debug.Log("Unit: " + entity);
               float3 unitPos = translation.Value;

               Entity closestTargetEntity = Entity.Null;
               float closestDistance = float.MaxValue;

               // Cycling through all target entities
               for (int i = 0; i < _TargetArray.Length; i++)
               {
                   EnitityWithPos targetEntityWithPos = _TargetArray[i];

                   float dist = math.distance(unitPos, _TargetArray[i]._Pos);

                   // if closer then set closest target entity and distance
                   if (dist < closestDistance)
                   {
                       closestDistance = dist;
                       closestTargetEntity = _TargetArray[i]._Entity;
                   }
               }

               _ClosestTargetEntityArray[index] = closestTargetEntity;
           }
       }
       */


    [RequireComponentTag(typeof(Unit))]
    [ExcludeComponent(typeof(HasTarget))]
    private struct AddComponentJob : IJobForEachWithEntity<Translation>
    {
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> _ClosestTargetEntityArray;
        public EntityCommandBuffer.Concurrent _EntityCommandBuffer;

        public void Execute(Entity entity, int index, ref Translation translation )
        {
            if(_ClosestTargetEntityArray[index] != Entity.Null)
            {
                //_EntityCommandBuffer.AddComponent(index, entity, new HasTarget { _TargetEntity = _ClosestTargetEntityArray[index] });
            }
        }
    }

    #endregion
    
    #region CREATE AND UPDATE
       
    protected override void OnCreate()
    {
        // get ref to command buffer system on create
        _EndSimCommandBufferSys = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        base.OnCreate();
    }


    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        // Setup query for entities
        EntityQuery targetQuery = GetEntityQuery(typeof(Target), ComponentType.ReadOnly<Translation>());
        
        #region FIND TAREGT JOB

        // Setup query for entities
        EntityQuery unitQuery = GetEntityQuery(typeof(Unit), ComponentType.Exclude<HasTarget>());
        NativeArray<Entity> closestTargetEntityArray = new NativeArray<Entity>(unitQuery.CalculateEntityCount(), Allocator.TempJob);

        FindTargetQuadrantSystemJob findTargetJob = new FindTargetQuadrantSystemJob
        {
            _QuadMultiHashMap = DOTS_QuadrantSystem._QuadrantMultiHashMap,
            _ClosestTargetEntityArray = closestTargetEntityArray,
        };

        JobHandle jobHandle = findTargetJob.Schedule(this, inputDeps);

        #endregion



        #region ADD HASTARGET COMPONENT JOB
        AddComponentJob addComponentJob = new AddComponentJob
        {
            _ClosestTargetEntityArray = closestTargetEntityArray,
            _EntityCommandBuffer = _EndSimCommandBufferSys.CreateCommandBuffer().ToConcurrent()
        };

        // schedule to when first job is finished by passing in job handle
        jobHandle = addComponentJob.Schedule(this, jobHandle);
        #endregion



        // Send to system to execute after this job
        _EndSimCommandBufferSys.AddJobHandleForProducer(jobHandle);
        
        return jobHandle;
    }

    #endregion
}

/*
// Comment this in to test with a straight component system 
// 1000 units searching for 1000 targets - 315ms
public class ECS_FindTargetSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.WithAll<Unit>().WithNone<HasTarget>().ForEach((Entity entity, ref Translation unitTranslation) => 
        {
            // Code running on all entities with 'tag' component unit
            //Debug.Log("Unit: " + entity);
            Entity closestTargetEntity = Entity.Null;
            float closestDistance = float.MaxValue;
            float3 unitPos = unitTranslation.Value;

            Entities.WithAll<Target>().ForEach((Entity targetEntity, ref Translation targetTranslation) =>
            {
                // Cycling through all the entities with 'Target' tag
                //Debug.Log("Target: " + targetEntity);

                float dist = math.distance(unitPos, targetTranslation.Value);

                if(dist < closestDistance)
                {
                    // No target
                    closestDistance = dist;
                    closestTargetEntity = targetEntity;
                }
            });

            // Closest target
            if(closestTargetEntity != Entity.Null)
            {
                //Debug.DrawLine(unitPos, closestTargetPos);
                PostUpdateCommands.AddComponent(entity, new HasTarget { _TargetEntity = closestTargetEntity });
            }
        });        
    }
}
*/

