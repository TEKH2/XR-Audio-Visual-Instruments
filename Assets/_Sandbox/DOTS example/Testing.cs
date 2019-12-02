using UnityEngine;
using Unity.Entities;
using Unity.Transforms;     // Translation
using Unity.Collections;    // NativeArray
using Unity.Rendering;
using Unity.Mathematics;

/// <summary>
/// Monobehavior that instantiates entities for the DOTS test
/// https://www.undefinedgames.org/2019/10/14/unity-ecs-dots-introduction/
/// </summary>

public class Testing : MonoBehaviour
{
    // Mesh and material to render
    [SerializeField] private Mesh _Mesh;
    [SerializeField] private Material _Material;
    [SerializeField] private int _Count;

    private void Start()
    {
        // Create entity manager
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // Create an archetype with
        // Level component
        // Translation - needed for rendering
        // Render Mesh - needed for rendering
        // LocalToWorld - needed for rendering
        EntityArchetype entityArchetype = entityManager.CreateArchetype
        (
            typeof(LevelComponent),
            typeof(Translation), 
            typeof(RenderMesh),  
            typeof(LocalToWorld) 
        );

        // Create array of entities with a count and an allocation type
        // In this case it is temp bc it's only being used to initialize the entities
        NativeArray<Entity> entityArray = new NativeArray<Entity>(_Count, Allocator.Temp);
        // Create entities from array
        entityManager.CreateEntity(entityArchetype, entityArray);

        for (int i = 0; i < entityArray.Length; i++)
        {
            // entity ref
            Entity entity = entityArray[i];
            // Set data on components
            entityManager.SetComponentData(entity, new LevelComponent { _LevelTime = UnityEngine.Random.Range(10f, 20f) });
            entityManager.SetComponentData(entity, new Translation { Value = new float3(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(0f, 10f), (UnityEngine.Random.Range(-10f, 10f))) });
            entityManager.SetSharedComponentData(entity, new RenderMesh
            {
                mesh = _Mesh,
                material = _Material,
            });
        }

        // Dispose of the entity array because they have already been created
        entityArray.Dispose();
    }
}