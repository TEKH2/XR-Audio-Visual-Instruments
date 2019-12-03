using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;

public class ECS_Jobs_MoveEntityExample : MonoBehaviour
{
    [SerializeField] private bool _UseJobs = false;
    [SerializeField] private Transform _TransformPrefab;
    private List<MoveableTransform> _MoveableTransformList;

    public class MoveableTransform
    {
        public Transform transform;
        public float moveY;
    }

    private void Start()
    {
        _MoveableTransformList = new List<MoveableTransform>();

        // Instantiate and randomly position transforms
        for (int i = 0; i < 200; i++)
        {
            // Create transform at random position from prefab
            Transform limeTransform = Instantiate
                                        (
                                            _TransformPrefab,
                                            new Vector3(UnityEngine.Random.Range(-12f, 12f), UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-8f, 8f)),
                                            Quaternion.identity
                                        );

            // Add a new moveable transform class too the list so it's data can be passed into the job
            _MoveableTransformList.Add(new MoveableTransform
            {
                transform = limeTransform,
                moveY = UnityEngine.Random.Range(1f, 2f)
            });
        }
    }

    // Update is called once per frame
    void Update()
    {
        float startTime = Time.realtimeSinceStartup;

        if (_UseJobs)
        {
            // Create arrays for copying data into
            NativeArray<float3> positionArray = new NativeArray<float3>(_MoveableTransformList.Count, Allocator.TempJob);
            NativeArray<float> moveYArray = new NativeArray<float>(_MoveableTransformList.Count, Allocator.TempJob);

            // Populate the arrays 
            for (int i = 0; i < _MoveableTransformList.Count; i++)
            {
                positionArray[i] = _MoveableTransformList[i].transform.position;
                moveYArray[i] = _MoveableTransformList[i].moveY;
            }

            // Create job w. data
            ReallyToughParallelJob reallyToughParallelJob = new ReallyToughParallelJob
            {
                deltaTime = Time.deltaTime,
                positionArray = positionArray,
                moveYArray = moveYArray,
            };

            // Schedule job
            JobHandle jobHandle = reallyToughParallelJob.Schedule(_MoveableTransformList.Count, 100);
            jobHandle.Complete();

            // Update to calculated values
            for (int i = 0; i < _MoveableTransformList.Count; i++)
            {
                _MoveableTransformList[i].transform.position = positionArray[i];
                _MoveableTransformList[i].moveY = moveYArray[i];
            }

            // Dispose arrays
            positionArray.Dispose();
            moveYArray.Dispose();
        }
        else
        { 
            // Move the limes up and down
            foreach (MoveableTransform lime in _MoveableTransformList)
            {
                lime.transform.position += new Vector3(0, lime.moveY * Time.deltaTime);
                if (lime.transform.position.y > 5f)
                {
                    lime.moveY = -math.abs(lime.moveY);
                }

                if (lime.transform.position.y < -5f)
                {
                    lime.moveY = +math.abs(lime.moveY);
                }

                float value = 0f;
                for (int i = 0; i < 50000; i++)
                {
                    value = math.exp10(math.sqrt(value));
                }
            }
        }
        Debug.Log(((Time.realtimeSinceStartup - startTime) * 1000f) + "ms");
    }

    [BurstCompile]
    public struct ReallyToughParallelJob : IJobParallelFor
    {
        public NativeArray<float3> positionArray;
        public NativeArray<float> moveYArray;
        public float deltaTime;

        public void Execute(int index)
        {
            positionArray[index] += new float3(0, moveYArray[index] * deltaTime, 0f);

            if (positionArray[index].y > 5f)
            {
                moveYArray[index] = -math.abs(moveYArray[index]);
            }

            if (positionArray[index].y < -5f)
            {
                moveYArray[index] = +math.abs(moveYArray[index]);
            }

            float value = 0f;
            for (int i = 0; i < 50000; i++)
            {
                value = math.exp10(math.sqrt(value));
            }
        }
    }
}