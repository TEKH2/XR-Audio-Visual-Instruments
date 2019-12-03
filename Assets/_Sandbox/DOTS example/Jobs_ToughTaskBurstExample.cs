using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

public class Jobs_ToughTaskBurstExample : MonoBehaviour
{
    [SerializeField] private bool useJobs;

    // Update is called once per frame
    void Update()
    {
        float startTime = Time.realtimeSinceStartup;

        if(useJobs)
        {
            // List of jobs to be executed in parallel
            NativeList<JobHandle> jobHandleList = new NativeList<JobHandle>(Allocator.Temp);

            for (int i = 0; i < 10; i++)
            {
                JobHandle jobHandle = ReallyToughTaskJob();
                jobHandleList.Add(jobHandle);
            }

            // Pauses main thread until all contained jobhandles are completed
            JobHandle.CompleteAll(jobHandleList);
            jobHandleList.Dispose(); // Dispose list
        }
        else
        {
            for (int i = 0; i < 10; i++)
            {
                ReallyToughTask();
            }
        }

        Debug.Log(((Time.realtimeSinceStartup - startTime) * 1000f) + "ms");
    }

    private void ReallyToughTask()
    {
        float value = 0f;
        for (int i = 0; i < 50000; i++)
        {
            value = math.exp10(math.sqrt(value));
        }
    }

    private JobHandle ReallyToughTaskJob()
    {
        ReallyToughJob job = new ReallyToughJob();
        job.value = 0f;
        return job.Schedule();
    }

    [BurstCompile]
    public struct ReallyToughJob : IJob
    {
        public float value;
        public void Execute()
        {
            for (int i = 0; i < 50000; i++)
            {
                value = math.exp10(math.sqrt(value));
            }
        }
    }
}