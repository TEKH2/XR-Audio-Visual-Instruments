using Unity.Entities;
using Unity.Mathematics;

public struct SpawnerComponent : IComponentData
{
    public float _SpawnRate;
    public float _SpawnTimer;
    public float3 _SpawnAreaDimensions;
}
