using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class IntBufferElementAuthoring : IConvertGameObjectToEntity
{
    public int[] _ValueArray;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        // Create a dynamic buffer then add it to the dest manager using the entity we are converting
        DynamicBuffer<IntBufferElement> dynamicBuffer = dstManager.AddBuffer<IntBufferElement>(entity);

        // Fill buffer data
        foreach(int value in _ValueArray)
        {
            dynamicBuffer.Add(new IntBufferElement { Value = value });
        }
    }
}
