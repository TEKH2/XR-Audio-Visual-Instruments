using Unity.Entities;

[InternalBufferCapacity(5)]
public struct TargetElement : IBufferElementData
{
    public Entity targetEntity;
}
