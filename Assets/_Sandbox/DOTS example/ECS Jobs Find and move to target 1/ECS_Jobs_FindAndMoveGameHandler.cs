using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Rendering;
using Unity.Entities;
using Unity.Transforms;

public class ECS_Jobs_FindAndMoveGameHandler : MonoBehaviour
{
    [SerializeField] private Material _UnitMat;
    [SerializeField] private Material _TargetMat;
    [SerializeField] private Mesh _UnitMesh;
    [SerializeField] private Mesh _TargetMesh;

    private static EntityManager _EntityManager;

    public int _UnitCount = 50;
    public int _TargetCount = 1000;

    float _SpawnTargetTimer = .1f;
    float _SpawnDuration = .1f;

    // Start is called before the first frame update
    void Start()
    {
        _EntityManager = World.Active.EntityManager;

        for (int i = 0; i < _UnitCount; i++)
        {
            SpawnUnitEntity();
        }

        for (int i = 0; i < _TargetCount; i++)
        {
            SpawnTargetEntity();
        }
    }

    private void Update()
    {
        _SpawnTargetTimer -= Time.deltaTime;

        if (_SpawnTargetTimer <= 0)
        {
            _SpawnTargetTimer = _SpawnDuration;

            for (int i = 0; i < 10; i++)
            {
                SpawnTargetEntity();
            }
        }
    }

    void SpawnUnitEntity()
    {
        SpawnUnitEntity( new float3(UnityEngine.Random.Range(-10f,10f), UnityEngine.Random.Range(-10f, 10f), 0));
    }

    void SpawnUnitEntity(float3 pos)
    {
        Entity unitEntity = _EntityManager.CreateEntity
        (
            typeof(Translation),
            typeof(LocalToWorld),
            typeof(RenderMesh),
            typeof(Scale),
            typeof(Unit)
        );

        SetEntityComponentData(unitEntity, pos, _UnitMesh, _UnitMat, .4f );
       
    }
    
    void SpawnTargetEntity()
    {
        Entity targetEntity = _EntityManager.CreateEntity
        (
           typeof(Translation),
           typeof(LocalToWorld),
           typeof(RenderMesh),
           typeof(Scale),
           typeof(Target)
        );

        SetEntityComponentData(targetEntity, new float3(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f), 0), _TargetMesh, _TargetMat, .5f);
        _EntityManager.SetComponentData(targetEntity, new Scale { Value = .2f });
    }

    void SetEntityComponentData(Entity entity, float3 pos, Mesh mesh, Material mat, float scale)
    {
        _EntityManager.SetSharedComponentData<RenderMesh>
        (
            entity,
            new RenderMesh
            {
                material = mat,
                mesh = mesh,
            }
        );

        _EntityManager.SetComponentData
        (
            entity,
            new Translation
            {
                Value = pos
            }
        );

        _EntityManager.SetComponentData(entity, new Scale { Value = scale });
    }
}
/*
public class HasTargetDebug : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach
        (
            (Entity entity, ref Translation translation, ref HasTarget hasTarget) =>
            {
                if (World.Active.EntityManager.Exists(hasTarget._TargetEntity))
                {
                    Translation targetTrans = World.Active.EntityManager.GetComponentData<Translation>(hasTarget._TargetEntity);
                    Debug.DrawLine(translation.Value, targetTrans.Value);
                }
                else
                {
                    // Remove componenet bc target entity is already destroyed
                    PostUpdateCommands.RemoveComponent(entity, typeof(HasTarget));
                }
            }
        );
    }
}
*/