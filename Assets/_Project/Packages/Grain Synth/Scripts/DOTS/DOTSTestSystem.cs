using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Collections;
using System.Linq;

/// <summary>
/// Testing assumptions about DOTS
/// - If I add a compoenent 3 times in a singel frame and set value, the value seems to come out randomly. Makes sense since the jobs may happen in a random order
/// - If I seperate the ecb add component out to 2 seperate systems they appear to execute and assign values in the correct order
/// - If you add a component to an object that already has a component then it will be overwritten with the latest add componenet value
/// </summary>
//public class DOTSTestSystem : SystemBase
//{
//    // Command buffer for removing tween componants once they are completed
//    private EndSimulationEntityCommandBufferSystem _CommandBufferSystem;

//    protected override void OnCreate()
//    {
//        base.OnCreate();
//        _CommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
//    }

//    protected override void OnUpdate()
//    {
//        Debug.LogError("Test component count -1: " + GetEntityQuery(typeof(TestComp)).CalculateEntityCount());

//        // Acquire an ECB and convert it to a concurrent one to be able to use it from a parallel job.
//        EntityCommandBuffer.ParallelWriter entityCommandBuffer = _CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();


//        // ----    TEST 1
//        Entities.ForEach((int entityInQueryIndex, Entity entity, in Translation trans) =>
//        {
//            entityCommandBuffer.AddComponent(entityInQueryIndex, entity, new TestComp { testVar = 1 });
//        }).ScheduleParallel();


//        //// ----    TEST 2
//        //Entities.ForEach((int entityInQueryIndex, Entity entity, in EmitterComponent emitter, in Translation trans) =>
//        //{
//        //    entityCommandBuffer.AddComponent(entityInQueryIndex, entity, new TestComp { testVar = 2 });
//        //}).ScheduleParallel();


//        //// ----    TEST 3
//        //Entities.ForEach((int entityInQueryIndex, Entity entity, in EmitterComponent emitter, in Translation trans) =>
//        //{
//        //    entityCommandBuffer.AddComponent(entityInQueryIndex, entity, new TestComp { testVar = .5f });
//        //}).ScheduleParallel();

//        // Make sure that the ECB system knows about our job
//        _CommandBufferSystem.AddJobHandleForProducer(Dependency);

//        Debug.LogError("Test component count 0: " + GetEntityQuery(typeof(TestComp)).CalculateEntityCount());
//    }
//}

//[UpdateAfter(typeof(DOTSTestSystem))]
//public class DOTSTestSystem2 : SystemBase
//{
//    // Command buffer for removing tween componants once they are completed
//    private EndSimulationEntityCommandBufferSystem _CommandBufferSystem;

//    protected override void OnCreate()
//    {
//        base.OnCreate();
//        _CommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
//    }

//    protected override void OnUpdate()
//    {
//        // Acquire an ECB and convert it to a concurrent one to be able to use it from a parallel job.
//        EntityCommandBuffer.ParallelWriter entityCommandBuffer = _CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();


//        // ----    TEST 3
//        Entities.ForEach((int entityInQueryIndex, Entity entity, in Translation trans) =>
//        {
//            entityCommandBuffer.AddComponent(entityInQueryIndex, entity, new TestComp { testVar = .5f });
//        }).ScheduleParallel();



//        // Make sure that the ECB system knows about our job
//        _CommandBufferSystem.AddJobHandleForProducer(Dependency);

//        Debug.LogError("Test component count 1: " + GetEntityQuery(typeof(TestComp)).CalculateEntityCount());
//    }
//}

//[UpdateAfter(typeof(DOTSTestSystem2))]
//public class DOTSTestSystem3 : SystemBase
//{
//    // Command buffer for removing tween componants once they are completed
//    private EndSimulationEntityCommandBufferSystem _CommandBufferSystem;

//    protected override void OnCreate()
//    {
//        base.OnCreate();
//        _CommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
//    }

//    protected override void OnUpdate()
//    {
//        // Acquire an ECB and convert it to a concurrent one to be able to use it from a parallel job.
//        EntityCommandBuffer.ParallelWriter entityCommandBuffer = _CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();


//        // ----    INCREMENT TEST VAR
//        Entities.ForEach((int entityInQueryIndex, Entity entity, ref TestComp test) =>
//        {
//            test.testVar++;
//        }).ScheduleParallel();

//        //// ----    REMOVE COMPONENT
//        //Entities.ForEach((int entityInQueryIndex, Entity entity, in Translation trans) =>
//        //{
//        //    entityCommandBuffer.RemoveComponent<TestComp>(entityInQueryIndex, entity);
//        //}).ScheduleParallel();



//        //// Make sure that the ECB system knows about our job
//        //_CommandBufferSystem.AddJobHandleForProducer(Dependency);

//        Debug.LogError("Test component count 2: " + GetEntityQuery(typeof(TestComp)).CalculateEntityCount());
//    }
//}