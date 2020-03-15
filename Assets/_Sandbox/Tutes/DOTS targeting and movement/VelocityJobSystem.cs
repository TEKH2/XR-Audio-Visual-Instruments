using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

public class VelocityJobSystem : JobComponentSystem
{ 

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        JobHandle velJobHandle = new VelocityJob
        {
            delta = Time.DeltaTime
        }.Schedule(this, inputDeps);

        return velJobHandle;
    }

    public struct VelocityJob : IJobForEach<VelocityComponent, Translation>
    {
        public float delta;

        public void Execute(ref VelocityComponent vel, ref Translation translation)
        {
            // Apply velocity to translation
            translation.Value += vel._Velocity * delta;

            // Apply drag to velocity
            float drag = 1 - vel._Drag * delta;
            drag = Mathf.Max(drag, 0);           
            vel._Velocity *= drag;
        }
    }
}
