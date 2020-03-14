using Unity.Entities;

// Sets the internal buffer capacity. This is the defualt allocation lenght for this array
// When it runs over it reallocates which is a processing hit
[GenerateAuthoringComponent]
[InternalBufferCapacity(5)]
public struct IntBufferElement : IBufferElementData
{
    public int Value;
}
