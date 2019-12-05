using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Rendering;
using Unity.Entities;
using Unity.Transforms;

public struct QuadEntityType : IComponentData
{
    public QuadEntityTypeEnum _Type;

    public enum QuadEntityTypeEnum
    {
        Unit,
        Target,
    }
}

public struct QuadData
{
    public Entity _Entity;
    public float3 _Pos;
    public QuadEntityType _QuadEntityType;

    public QuadData( Entity entity, float3 pos, QuadEntityType quadEntity)
    {
        _Entity = entity;
        _Pos = pos;
        _QuadEntityType = quadEntity;
    }
}

public class DOTS_QuadrantSystem : ComponentSystem
{
    // used to scale the y component of the hash map so values don't overlap
    public const int _QuadYHashMultiplier = 1000;
    private const int _QuadCellSize = 5;

    public static NativeMultiHashMap<int, QuadData> _QuadrantMultiHashMap;

    protected override void OnCreate()
    {
        // Multi hash map that has a single key which holds many values, in this case a has for a quadrant which holds entities
        // This is created as a persistant collection so it can be used by other classes
        _QuadrantMultiHashMap = new NativeMultiHashMap<int, QuadData>(0, Allocator.Persistent);
        base.OnCreate();
    }

    protected override void OnDestroy()
    {
        _QuadrantMultiHashMap.Dispose();
        base.OnDestroy();
    }

    // returns a unique hashmap int key using the position
    public static int GetPosHashMapKey(float3 pos)
    {
        return (int)(math.floor(pos.x / _QuadCellSize) + (_QuadYHashMultiplier * math.floor(pos.y / _QuadCellSize)));
    }

    // Draws debug lines areound the quadrant
    private static void DebugDrawQuadrant(float3 pos)
    {
        Vector3 lowerLeft = new Vector3( math.floor(pos.x / _QuadCellSize) * _QuadCellSize, math.floor(pos.y / _QuadCellSize) *_QuadCellSize, 0);

        Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(1, 0) * _QuadCellSize);
        Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(0, 1) * _QuadCellSize);
        Debug.DrawLine(lowerLeft + new Vector3(1, 0) * _QuadCellSize, lowerLeft + new Vector3(1, 1) * _QuadCellSize);
        Debug.DrawLine(lowerLeft + new Vector3(1, 0) * _QuadCellSize, lowerLeft + new Vector3(1, 1) * _QuadCellSize);

        //Debug.Log("Hashmap key: " + GetPosHashMapKey(pos) + "     Position: " + pos);
    }

    private static int GetEntityCountInHashMap(NativeMultiHashMap<int, QuadData> quadMultiHashMap, int hashMapKey )
    {
        QuadData quadData;
        NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
        int count = 0;

        if( quadMultiHashMap.TryGetFirstValue(hashMapKey, out quadData, out nativeMultiHashMapIterator) )
        {
            do
            {
                count++;
            }
            while (quadMultiHashMap.TryGetNextValue(out quadData, ref nativeMultiHashMapIterator));            
        }

        return count;
    }

    [BurstCompile]
    private struct SetQuadrantDataHashMapJob : IJobForEachWithEntity<Translation, QuadEntityType>
    {
        public NativeMultiHashMap<int, QuadData>.ParallelWriter quadrantMultiHashMap;

        public void Execute(Entity entity, int index, ref Translation translation, ref QuadEntityType quadEntity)
        {
            int hashMapKey = GetPosHashMapKey(translation.Value);

            quadrantMultiHashMap.Add
            (
                hashMapKey,
                new QuadData
                (
                    entity,
                    translation.Value,
                    quadEntity
                )                
            );
        }
    }

    protected override void OnUpdate()
    {
        // Build query for entitys we want to work on
        EntityQuery entityQuery = GetEntityQuery(typeof(Translation), typeof(QuadEntityType));

        // Clear the hash map each update
        _QuadrantMultiHashMap.Clear();

        // Expand capacity of the quadrant multi hash map if there are more entitys in teh query than capacity
        if (entityQuery.CalculateEntityCount() > _QuadrantMultiHashMap.Capacity)
        {
            _QuadrantMultiHashMap.Capacity = entityQuery.CalculateEntityCount();
        }

        // JOB - Add quad data
        SetQuadrantDataHashMapJob setQuadrantDataHashMapJob = new SetQuadrantDataHashMapJob
        {
            quadrantMultiHashMap = _QuadrantMultiHashMap.AsParallelWriter(),
        };
        JobHandle jobHandle = JobForEachExtensions.Schedule(setQuadrantDataHashMapJob, entityQuery);
        jobHandle.Complete();

        #region DEBUG
        // Get mouse pos in world
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z));
        // Draw debug quadrant
        DebugDrawQuadrant(mouseWorldPos);
        //Debug.Log("Entity in selected quadrant: " + GetEntityCountInHashMap(_QuadrantMultiHashMap, GetPosHashMapKey(mouseWorldPos) ) );
        #endregion
    }
}
