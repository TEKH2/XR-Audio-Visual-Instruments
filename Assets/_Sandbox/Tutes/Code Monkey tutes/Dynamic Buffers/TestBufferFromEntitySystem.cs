using Unity.Entities;

[DisableAutoCreation]
public class TestBufferFromEntitySystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.WithAll<Tag_Player>().ForEach((Entity bobEntity) =>
        {
            // Get buffer from entity with Bob Tag
            BufferFromEntity<IntBufferElement> intBufferFromEntity = GetBufferFromEntity<IntBufferElement>();

            // Create local alice entity
            Entity aliceEntity = Entity.Null;

            // Get the alice entity
            Entities.WithAll<Tag_Enemies>().ForEach((Entity aliceEntityTmp) =>
            {
                aliceEntity = aliceEntityTmp;
            });

            // Get a dynamic buffer from the alice entity
            DynamicBuffer<IntBufferElement> aliceDynamicBuffer = intBufferFromEntity[aliceEntity];

            // Increment the data in the alice buffer
            for (int i = 0; i < aliceDynamicBuffer.Length; i++)
            {
                IntBufferElement intBufferElement = aliceDynamicBuffer[i];
                intBufferElement.Value++;
                aliceDynamicBuffer[i] = intBufferElement;
            }
        });
    }
}
