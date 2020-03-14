using Unity.Entities;

[DisableAutoCreation]
public class TestBufferSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        // Iterate through all dynamic buffers with int buffer elements and increment
        Entities.ForEach((DynamicBuffer<IntBufferElement> dynamicBuffer) =>
        {
            for (int i = 0; i < dynamicBuffer.Length; i++)
            {
                // Get element
                IntBufferElement intBufferElement = dynamicBuffer[i];
                // Increment
                intBufferElement.Value++;
                // Assign back to the buffer (bc its a struct and not a reference)
                dynamicBuffer[i] = intBufferElement;
            }
        });
    }
}
