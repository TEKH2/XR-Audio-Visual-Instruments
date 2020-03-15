using Unity.Entities;
using Unity.Jobs;

public class SeekTargetJobSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        JobHandle seekTargetJob = new SeekTargetJob
        {
            delta = Time.DeltaTime
        }.Schedule(this, inputDeps);

        return seekTargetJob;
    }
    


    public struct SeekTargetJob : IJobForEach_BC<TargetElement, VelocityComponent>
    {
        public float delta;
        
        public void Execute(DynamicBuffer<TargetElement> targetBuffer, ref VelocityComponent velocity)
        {
            
        }
    }
}
