using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class TestBufferCreation : MonoBehaviour
{
    void Start()
    {
        // Get entity manager
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        // Create entity
        Entity entity = entityManager.CreateEntity();

        // V1 
        // Create Dynamic buffer and add it to the entity right away
        // DynamicBuffer<IntBufferElement> dynamicBuffer = entityManager.AddBuffer<IntBufferElement>(entity);

        // V2
        // Add buffer to entity using entity manager
        entityManager.AddBuffer<IntBufferElement>(entity);
        // Get the buffer from the entity using entityManager.GetBuffer
        DynamicBuffer<IntBufferElement> dynamicBuffer = entityManager.GetBuffer<IntBufferElement>(entity);


        // Fill buffer with test data
        dynamicBuffer.Add(new IntBufferElement { Value = 1 });
        dynamicBuffer.Add(new IntBufferElement { Value = 2 });
        dynamicBuffer.Add(new IntBufferElement { Value = 3 });

        // Caste the buffer elements to a buffer with straight ints
        DynamicBuffer<int> intDynamicBuffer = dynamicBuffer.Reinterpret<int>();
        intDynamicBuffer[1] = 5;

        for (int i = 0; i < intDynamicBuffer.Length; i++)
        {
            intDynamicBuffer[i]++;
        }
    }
}
