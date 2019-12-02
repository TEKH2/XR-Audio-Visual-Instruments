using Unity.Entities;

/// <summary>
/// Note that components are always structs
/// https://www.undefinedgames.org/2019/10/14/unity-ecs-dots-introduction/
/// </summary>
public struct LevelComponent : IComponentData
{
    public float _LevelTime;
}