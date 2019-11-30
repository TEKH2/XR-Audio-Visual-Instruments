using Unity.Entities;

/// <summary>
/// Note that components are always structs
/// </summary>
public struct LevelComponent : IComponentData
{
    public float level;
}