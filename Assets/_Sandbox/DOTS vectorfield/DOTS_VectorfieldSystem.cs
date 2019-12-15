using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Rendering;
using Unity.Entities;
using Unity.Transforms;


// Vector field cell contain a direction
public struct VectorfieldCell : IComponentData
{
    public float3 _Force;
    public float3 _Pos;
}

// Vector field cell contain a direction
public struct EntityInVoxelData : IComponentData
{
    public Entity _Entity;
    public Mover _Mover;
    public Translation _EntityTranslation;
}

public struct PosHash : IComponentData
{
    public int _Hash;
}

// Mover objects have basic physics
public struct Mover : IComponentData
{
    public float3 _Acc;
    public float3 _Velocity;
    public float _Drag;
}

// Mover objects have basic physics
public struct MoverAvoid : IComponentData
{
    public float _AvoidDist;
    public float _AvoidStrength;
}


public class DOTS_VectorfieldSystem : MonoBehaviour
{
    #region VARIABLES
    // Reference ot entity manager
    private static EntityManager _EntityManager;

    // Voxel vars
    public int _VoxelCount = 50;
    public float _VoxelSize = 1;
    public float _VectorLength = 1;
    float3 _VectorfieldCenter;
    float Size { get { return _VoxelCount * _VoxelSize; } }

    // Mover vars
    [SerializeField] Material _MoverMat;
    [SerializeField] Mesh _MoverMesh;
    [SerializeField] int _MoverCount = 10;

    MoverSystem _MoverSystem;

    #endregion

    private void Awake()
    {
        // Get entity manager
        _EntityManager = World.Active.EntityManager;

        World.Active.GetExistingSystem<DOTS_QuadrantSystem>().Enabled = false;
        World.Active.GetExistingSystem<DOTS_MoveToTargetSystem>().Enabled = false;
        World.Active.GetExistingSystem<DOTS_FindTargetSystem>().Enabled = false;
        World.Active.GetExistingSystem<ECS_ComponentSystemExample>().Enabled = false;


        // Init mover system
        _MoverSystem = World.Active.GetExistingSystem(typeof(MoverSystem)) as MoverSystem;
      
        // Init vector field
        _VectorfieldCenter = new float3(((_VoxelSize * _VoxelCount) * .5f) - (_VoxelSize * .5f));
        for (int x = 0; x < _VoxelCount; x++)
        {
            for (int y = 0; y < _VoxelCount; y++)
            {
                for (int z = 0; z < _VoxelCount; z++)
                {
                    float3 pos = new float3(x * _VoxelSize, y * _VoxelSize, z * _VoxelSize);                  
                    SpawnVectorfieldCellEntity(pos);
                }
            }
        }

        // Initialze the mover system. It requires the vector field entitys to be intied first
        _MoverSystem.Initialize(Size);

        // Init movers
        for (int i = 0; i < _MoverCount; i++)
        {
            SpawnMoverEntity();
        }
    }

    #region SPAWN METHODS
    void SpawnVectorfieldCellEntity(float3 pos)
    {
        // Create entity
        Entity vecCellEntity = _EntityManager.CreateEntity
        (
            typeof(VectorfieldCell),
            typeof(PosHash)
        );

        // Set force to float zero if in center to avoid NaNs
        float3 force = math.normalize(_VectorfieldCenter - pos);
        if (pos.Equals(_VectorfieldCenter))
            force = new float3(0, 0, 0);

        // Set component data
        _EntityManager.SetComponentData(vecCellEntity,
            new VectorfieldCell { _Force = force, _Pos = pos }
            );

        _EntityManager.SetComponentData(vecCellEntity,
           new PosHash { _Hash = _MoverSystem.GetVectorfieldHashMapKey(pos) }
           );
    }

    void SpawnMoverEntity()
    {
        Entity moverEntity = _EntityManager.CreateEntity
        (
           typeof(Translation),
           typeof(LocalToWorld),
           typeof(RenderMesh),
           typeof(Scale),
           typeof(Mover),
           typeof(MoverAvoid)
        );

        SetEntityComponentData( moverEntity, RandomPosInVectorfield(), _MoverMesh, _MoverMat, .2f );
        _EntityManager.SetComponentData( moverEntity, new Mover { _Velocity = float3.zero, _Drag = .2f } );
        _EntityManager.SetComponentData(moverEntity, new MoverAvoid { _AvoidDist = 1, _AvoidStrength = 9f });
    }

    void SetEntityComponentData(Entity entity, float3 pos, Mesh mesh, Material mat, float scale)
    {
        _EntityManager.SetSharedComponentData<RenderMesh>
        (
            entity,
            new RenderMesh
            {
                material = mat,
                mesh = mesh,
            }
        );

        _EntityManager.SetComponentData
        (
            entity,
            new Translation
            {
                Value = pos
            }
        );

        _EntityManager.SetComponentData(entity, new Scale { Value = scale });
    }

    #endregion

    #region HELPER METHODS
    float3 RandomPosInVectorfield()
    {
        return new float3(UnityEngine.Random.Range(0 + (Size *.2f), Size - (Size * .2f)), UnityEngine.Random.Range(0 + (Size * .2f), Size - (Size * .2f)), UnityEngine.Random.Range(0 + (Size * .2f), Size - (Size * .2f)));
    }
    #endregion
}

// A system that sets the velocity of all movers from the vector field
public class MoverSystem : ComponentSystem
{
    // Bounds for the vectorfield   
    float _VectorfeildResolution = 100;

    // Hashmap of all the vector cells
    public static NativeHashMap<int, VectorfieldCell> _VectorfieldCellHashMap;   
    
    // position hash as key 
    public static NativeMultiHashMap<int, EntityInVoxelData> _EnitityInVoxelsMultiHash;

    // Hash multiplyers - Offset each axis so we get a hash without and duplicates
    public const int _YHashMultiplier = 10000;
    public const int _ZHashMultiplier = 1000000;

    protected override void OnCreate()
    {
        _VectorfieldCellHashMap = new NativeHashMap<int, VectorfieldCell>(0, Allocator.Persistent);
        _EnitityInVoxelsMultiHash = new NativeMultiHashMap<int, EntityInVoxelData>(0, Allocator.Persistent);
        base.OnCreate();
    }

    public void Initialize(float vectorfieldResolution)
    {
        _VectorfeildResolution = vectorfieldResolution;
        InitVectorfieldCellHashMap();
    }

    void InitVectorfieldCellHashMap()
    {
        // Get all entitys with a VectorfieldCell component 
        EntityQuery entityQuery = GetEntityQuery(typeof(VectorfieldCell), typeof(PosHash));
        NativeArray<VectorfieldCell> vectorCellForces = entityQuery.ToComponentDataArray<VectorfieldCell>(Allocator.TempJob);
        NativeArray<PosHash> posHashes = entityQuery.ToComponentDataArray<PosHash>(Allocator.TempJob);

        for (int i = 0; i < vectorCellForces.Length; i++)
        {
            // Get hash and check if it is aready contained in the hashmap
            int hash = posHashes[i]._Hash;

            if (_VectorfieldCellHashMap.ContainsKey(hash))
                Debug.LogError("HASH MAP - Already contains : " + hash);

            // Add the vector cell to the hashmap
            _VectorfieldCellHashMap.TryAdd(hash, vectorCellForces[i]);
        }

        // Dispose temporary arrays
        vectorCellForces.Dispose();
        posHashes.Dispose();

        Debug.Log("HASH MAP - Initialized with hash count: " + _VectorfieldCellHashMap.Length);
    }

    protected override void OnUpdate()
    {
        // Build query for entitys we want to work on
        EntityQuery moverQuery = GetEntityQuery(typeof(Mover), typeof(Translation));

        // Clear the hash map each update
        _EnitityInVoxelsMultiHash.Clear();

        // Expand capacity of the quadrant multi hash map if there are more entitys in teh query than capacity
        if (moverQuery.CalculateEntityCount() > _EnitityInVoxelsMultiHash.Capacity)
        {
            _EnitityInVoxelsMultiHash.Capacity = moverQuery.CalculateEntityCount();
        }

        // Fill entity in voxel hash map
        Entities.ForEach((Entity entity, ref Mover mover, ref Translation translation) =>
        {
            int hashMapKey = GetVectorfieldHashMapKey(translation.Value);

            _EnitityInVoxelsMultiHash.Add
            (
                hashMapKey,
                new EntityInVoxelData
                {
                    _Entity = entity,
                    _Mover = mover,
                    _EntityTranslation = translation
                }
            );

            //Debug.DrawLine(translation.Value, _VectorfieldCellHashMap[hashMapKey]._Pos);
        });



        // Movers accumulate acceleration from vector field
        Entities.ForEach((Entity entity, ref Translation translation, ref Mover mover) =>
        {
            // Get hash from position
            int hash = GetVectorfieldHashMapKey(translation.Value);

            float3 acceleration = float3.zero;

            #region Search adjascent cells

            acceleration = _VectorfieldCellHashMap[hash]._Force;

            // Search edge cells
            acceleration += _VectorfieldCellHashMap[hash + 1]._Force;   // Right
            acceleration += _VectorfieldCellHashMap[hash - 1]._Force;   // Left
            acceleration += _VectorfieldCellHashMap[hash + _YHashMultiplier]._Force; // Top
            acceleration += _VectorfieldCellHashMap[hash - _YHashMultiplier]._Force; // Bottom

            // Search corner cells
            acceleration += _VectorfieldCellHashMap[hash + 1 + _YHashMultiplier]._Force; // Top Right
            acceleration += _VectorfieldCellHashMap[hash + 1 - _YHashMultiplier]._Force; // Bottom Right
            acceleration += _VectorfieldCellHashMap[hash - 1 - _YHashMultiplier]._Force; // Bottom Left
            acceleration += _VectorfieldCellHashMap[hash - 1 + _YHashMultiplier]._Force; // Bottom Left


            // FRONT ALONG Z
            // Search edge cells
            acceleration += _VectorfieldCellHashMap[hash + 1 + _ZHashMultiplier]._Force;   // Right
            acceleration += _VectorfieldCellHashMap[hash - 1 + _ZHashMultiplier]._Force;   // Left
            acceleration += _VectorfieldCellHashMap[hash + _YHashMultiplier + _ZHashMultiplier]._Force; // Top
            acceleration += _VectorfieldCellHashMap[hash - _YHashMultiplier + _ZHashMultiplier]._Force; // Bottom

            // Search corner cells
            acceleration += _VectorfieldCellHashMap[hash + 1 + _YHashMultiplier + _ZHashMultiplier]._Force; // Top Right
            acceleration += _VectorfieldCellHashMap[hash + 1 - _YHashMultiplier + _ZHashMultiplier]._Force; // Bottom Right
            acceleration += _VectorfieldCellHashMap[hash - 1 - _YHashMultiplier + _ZHashMultiplier]._Force; // Bottom Left
            acceleration += _VectorfieldCellHashMap[hash - 1 + _YHashMultiplier + _ZHashMultiplier]._Force; // Bottom Left


            // REAR ALONG Z
            // Search edge cells
            acceleration += _VectorfieldCellHashMap[hash + 1 - _ZHashMultiplier]._Force;   // Right
            acceleration += _VectorfieldCellHashMap[hash - 1 - _ZHashMultiplier]._Force;   // Left
            acceleration += _VectorfieldCellHashMap[hash + _YHashMultiplier - _ZHashMultiplier]._Force; // Top
            acceleration += _VectorfieldCellHashMap[hash - _YHashMultiplier - _ZHashMultiplier]._Force; // Bottom

            // Search corner cells
            acceleration += _VectorfieldCellHashMap[hash + 1 + _YHashMultiplier - _ZHashMultiplier]._Force; // Top Right
            acceleration += _VectorfieldCellHashMap[hash + 1 - _YHashMultiplier - _ZHashMultiplier]._Force; // Bottom Right
            acceleration += _VectorfieldCellHashMap[hash - 1 - _YHashMultiplier - _ZHashMultiplier]._Force; // Bottom Left
            acceleration += _VectorfieldCellHashMap[hash - 1 + _YHashMultiplier - _ZHashMultiplier]._Force; // Bottom Left

            acceleration /= 27.0f;

            #endregion

            mover._Acc = acceleration;
        });


        // Avoid entities nearby by applying force
        Entities.ForEach((Entity entity, ref Translation translation, ref Mover mover, ref MoverAvoid avoid) =>
        {
            // Get hash from position
            int hash = GetVectorfieldHashMapKey(translation.Value);

            float3 acceleration;
            acceleration = GetAvoidStrengthAtHash(hash, ref translation, ref avoid) * Time.deltaTime;

            // Search edge cells
            acceleration += GetAvoidStrengthAtHash(hash + 1, ref translation, ref avoid);   // Right
            acceleration += GetAvoidStrengthAtHash(hash - 1, ref translation, ref avoid);   // Left
            acceleration += GetAvoidStrengthAtHash(hash + _YHashMultiplier, ref translation, ref avoid); // Top
            acceleration += GetAvoidStrengthAtHash(hash - _YHashMultiplier, ref translation, ref avoid); // Bottom

            // Search corner cells
            acceleration += GetAvoidStrengthAtHash(hash + 1 + _YHashMultiplier, ref translation, ref avoid); // Top Right
            acceleration += GetAvoidStrengthAtHash(hash + 1 - _YHashMultiplier, ref translation, ref avoid); // Bottom Right
            acceleration += GetAvoidStrengthAtHash(hash - 1 - _YHashMultiplier, ref translation, ref avoid); // Bottom Left
            acceleration += GetAvoidStrengthAtHash(hash - 1 + _YHashMultiplier, ref translation, ref avoid); // Bottom Left


            // FRONT ALONG Z
            // Search edge cells
            acceleration += GetAvoidStrengthAtHash(hash + 1 + _ZHashMultiplier, ref translation, ref avoid);   // Right
            acceleration += GetAvoidStrengthAtHash(hash - 1 + _ZHashMultiplier, ref translation, ref avoid);   // Left
            acceleration += GetAvoidStrengthAtHash(hash + _YHashMultiplier + _ZHashMultiplier, ref translation, ref avoid); // Top
            acceleration += GetAvoidStrengthAtHash(hash - _YHashMultiplier + _ZHashMultiplier, ref translation, ref avoid); // Bottom

            // Search corner cells
            acceleration += GetAvoidStrengthAtHash(hash + 1 + _YHashMultiplier + _ZHashMultiplier, ref translation, ref avoid); // Top Right
            acceleration += GetAvoidStrengthAtHash(hash + 1 - _YHashMultiplier + _ZHashMultiplier, ref translation, ref avoid); // Bottom Right
            acceleration += GetAvoidStrengthAtHash(hash - 1 - _YHashMultiplier + _ZHashMultiplier, ref translation, ref avoid); // Bottom Left
            acceleration += GetAvoidStrengthAtHash(hash - 1 + _YHashMultiplier + _ZHashMultiplier, ref translation, ref avoid); // Bottom Left


            // REAR ALONG Z
            // Search edge cells
            acceleration += GetAvoidStrengthAtHash(hash + 1 - _ZHashMultiplier, ref translation, ref avoid);   // Right
            acceleration += GetAvoidStrengthAtHash(hash - 1 - _ZHashMultiplier, ref translation, ref avoid);   // Left
            acceleration += GetAvoidStrengthAtHash(hash + _YHashMultiplier - _ZHashMultiplier, ref translation, ref avoid); // Top
            acceleration += GetAvoidStrengthAtHash(hash - _YHashMultiplier - _ZHashMultiplier, ref translation, ref avoid); // Bottom

            // Search corner cells
            acceleration += GetAvoidStrengthAtHash(hash + 1 + _YHashMultiplier - _ZHashMultiplier, ref translation, ref avoid); // Top Right
            acceleration += GetAvoidStrengthAtHash(hash + 1 - _YHashMultiplier - _ZHashMultiplier, ref translation, ref avoid); // Bottom Right
            acceleration += GetAvoidStrengthAtHash(hash - 1 - _YHashMultiplier - _ZHashMultiplier, ref translation, ref avoid); // Bottom Left
            acceleration += GetAvoidStrengthAtHash(hash - 1 + _YHashMultiplier - _ZHashMultiplier, ref translation, ref avoid); // Bottom Left

            acceleration /= 27f;

            mover._Acc += acceleration;
        });

        // Update mover velocities
        Entities.ForEach((Entity entity, ref Translation translation, ref Mover mover) =>
        {
            // add acceerlation to velocity and apply drag
            mover._Velocity += mover._Acc * Time.deltaTime;
            mover._Velocity *= 1 - (mover._Drag * Time.deltaTime);

            // add velocity to position
            translation.Value += mover._Velocity * Time.deltaTime;

            // reset acceleration
            mover._Acc = float3.zero;
        });
    }

    float3 GetAvoidStrengthAtHash(int hash, ref Translation translation, ref MoverAvoid avoid)
    {
        EntityInVoxelData voxelData;
        NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
        float3 accel = 0;

        if (_EnitityInVoxelsMultiHash.TryGetFirstValue(hash, out voxelData, out nativeMultiHashMapIterator))
        {
            do
            {
                float dist = math.distance(translation.Value, voxelData._EntityTranslation.Value);
                float3 directionAwayFromEntity = translation.Value - voxelData._EntityTranslation.Value;

                float normDist = 1 - math.clamp((dist / avoid._AvoidDist), 0, 1);
                normDist = math.sqrt(normDist);

                accel = directionAwayFromEntity * avoid._AvoidStrength * normDist;
            }
            while (_EnitityInVoxelsMultiHash.TryGetNextValue(out voxelData, ref nativeMultiHashMapIterator));
        }

        return accel;
    }

    protected override void OnDestroy()
    {
        _VectorfieldCellHashMap.Dispose();
        _EnitityInVoxelsMultiHash.Dispose();
        base.OnDestroy();
    }

    // returns a hash based on a position
    public int GetVectorfieldHashMapKey(float3 pos)
    {
        float x = math.clamp(math.round(pos.x), 0, _VectorfeildResolution);
        float y = math.clamp(math.round(pos.y), 0, _VectorfeildResolution);
        float z = math.clamp(math.round(pos.z), 0, _VectorfeildResolution);
        int hash = (int) ( x + (y * _YHashMultiplier) + (z * _ZHashMultiplier));   
        
        return hash;
    }
}


public class VectorfieldDebugSystem : ComponentSystem
{
    protected override void OnUpdate()
    {        
        Entities.ForEach
        (
            (Entity entity, ref VectorfieldCell vectorCell) =>
            {
               // Debug.DrawLine(vectorCell._Pos, vectorCell._Pos + vectorCell._Force * .4f);              
            }
        );        
    }
}
