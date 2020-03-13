using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public class EntitySpawnerSystem : ComponentSystem
{
    /*
    float _SpawnTimer;
    public float _SpawnInterval = .2f;
    Random _Random = new Random(56);

    bool _SpawnFromPrefabEntityComponenet = false;
    bool _SpawnFromPrefabEntityComponenetSingleton = false;
    bool _SpawnFromPrefabEntities = false;
    bool _SpawnFromPrefabEntitiesV2 = true;

    protected override void OnUpdate()
    {
        if (_SpawnFromPrefabEntityComponenet)
        {
            _SpawnTimer -= Time.DeltaTime;

            if (_SpawnTimer <= 0f)
            {
                _SpawnTimer = _SpawnInterval;
                SpawnPrefab(GetSingleton<PrefabEntityComponent>()._PrefabEntity);             
            }
        }

        if (_SpawnFromPrefabEntityComponenetSingleton)
        {
            _SpawnTimer -= Time.DeltaTime;

            if (_SpawnTimer <= 0f)
            {
                _SpawnTimer = _SpawnInterval;

                Entities.ForEach((ref PrefabEntityComponent prefabEntityComponent) =>
                {
                    SpawnPrefab(prefabEntityComponent._PrefabEntity);
                });
            }
        }

        if (_SpawnFromPrefabEntities)
        {
            _SpawnTimer -= Time.DeltaTime;

            if (_SpawnTimer <= 0f)
            {
                _SpawnTimer = _SpawnInterval;
                SpawnPrefab(PrefabEntities._PrefabEntity);
            }
        }

        if (_SpawnFromPrefabEntitiesV2)
        {
            _SpawnTimer -= Time.DeltaTime;

            if (_SpawnTimer <= 0f)
            {
                _SpawnTimer = _SpawnInterval;
                SpawnPrefab(PrefabEntities_V2._PrefabEntity);            
            }
        }
    }

    void SpawnPrefab(Entity prefabEntity)
    {
        // Instantiate entity from prefab entity component
        Entity spawnedEntity = EntityManager.Instantiate(prefabEntity);
        EntityManager.SetComponentData(spawnedEntity, new Translation { Value = _Random.NextFloat3(-3f, 3f) });
    }
    */
    protected override void OnUpdate()
    {
        
    }
}
