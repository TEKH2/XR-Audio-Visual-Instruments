using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Rendering;
using Unity.Entities;
using Unity.Transforms;


public struct VectorfieldCell : IComponentData
{
    public float3 _Force;
    public float3 _Pos;
}

public struct Mover : IComponentData
{
    public float3 _Acc;
    public float3 _Velocity;
    public float _Drag;
}

public class DOTS_VectorfieldSystem : MonoBehaviour
{
    private static EntityManager _EntityManager;

    public int _VoxelCount = 50;
    public float _VoxelSize = 1;
    public float _VectorLength = 1;

    [SerializeField] private Material _Mat;
    [SerializeField] private Mesh _Mesh;

    public int _MoverCount = 10;

    float3 _Center;
    float Size { get { return _VoxelCount * _VoxelSize; } }

    MoverSystem _MoverSystem;



    private void Awake()
    {
        _EntityManager = World.Active.EntityManager;

        _MoverSystem = World.Active.GetExistingSystem(typeof(MoverSystem)) as MoverSystem;
        _MoverSystem._HashMin = 0;
        _MoverSystem._HashMax = Size;

        _Center = new float3(((_VoxelSize * _VoxelCount) * .5f) - (_VoxelSize * .5f));// (_VoxelSize/2.0f)*(_VoxelCount/2.0f), (_VoxelSize / 2.0f) * (_VoxelCount / 2.0f), (_VoxelSize / 2.0f) * (_VoxelCount / 2.0f));

        for (int x = 0; x < _VoxelCount; x++)
        {
            for (int y = 0; y < _VoxelCount; y++)
            {
                for (int z = 0; z < _VoxelCount; z++)
                {
                    float3 pos = new float3(x * _VoxelSize, y * _VoxelSize, z * _VoxelSize);
                    //Debug.Log("Cell pos: " + pos);
                    SpawnVectorfieldCellEntity(pos);
                }
            }
        }

        for (int i = 0; i < _MoverCount; i++)
        {
            SpawnMoverEntity();
        }
    }


    void SpawnVectorfieldCellEntity(float3 pos)
    {
        // Create entity
        Entity vecCellEntity = _EntityManager.CreateEntity
        (
           typeof(VectorfieldCell)
        );

        // Set component data
        _EntityManager.SetComponentData(vecCellEntity, new VectorfieldCell { _Force =  math.normalize(_Center - pos), _Pos = pos } );    
    }

    void SpawnMoverEntity()
    {
        Entity moverEntity = _EntityManager.CreateEntity
        (
           typeof(Translation),
           typeof(LocalToWorld),
           typeof(RenderMesh),
           typeof(Scale),
           typeof(Mover)
        );

        SetEntityComponentData( moverEntity, RandomPosInVectorfield(), _Mesh, _Mat, .4f );
        _EntityManager.SetComponentData( moverEntity, new Mover { _Velocity = new float3(1, 1, 1), _Drag = .2f } );
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

    float3 Noise(float3 pos)
    {
        float perlinScalar = .009f;
        pos += new float3(UnityEngine.Random.Range(10, 100), UnityEngine.Random.Range(10, 100), UnityEngine.Random.Range(10, 100));
        float noiseX = Mathf.PerlinNoise(pos.x * perlinScalar, (pos.y + pos.z) * perlinScalar);
        float noiseY = 0;// Mathf.PerlinNoise(pos.y * perlinScalar, (pos.z + pos.x) * perlinScalar);
        float noiseZ = 0;// Mathf.PerlinNoise(pos.z * perlinScalar, (pos.x + pos.y) * perlinScalar);

        float3 noise = new float3(noiseX, noiseY, noiseZ);
        noise -= new float3(.5f, .5f, .5f);
        noise *= _VectorLength;

        //Debug.Log(pos + "    " + noise);

        return noise;
    }

    float3 RandomPosInVectorfield()
    {
        return new float3(UnityEngine.Random.Range(0 + (Size *.2f), Size - (Size * .2f)), UnityEngine.Random.Range(0 + (Size * .2f), Size - (Size * .2f)), UnityEngine.Random.Range(0 + (Size * .2f), Size - (Size * .2f)));
    }

}

public class MoverSystem : ComponentSystem
{
    public static NativeHashMap<int, VectorfieldCell> _CellHashMap;
    public float _HashMin = 0;
    public float _HashMax = 100;
    
    protected override void OnCreate()
    {
        _CellHashMap = new NativeHashMap<int, VectorfieldCell>(0, Allocator.Persistent);

        base.OnCreate();
    }

    void InitHashMap()
    {
        // fill cell hashmap
        EntityQuery entityQuery = GetEntityQuery(typeof(VectorfieldCell));
        NativeArray<VectorfieldCell> vectorCellForces = entityQuery.ToComponentDataArray<VectorfieldCell>(Allocator.TempJob);

       

        for (int i = 0; i < vectorCellForces.Length; i++)
        {
            //Debug.Log("Query lenght: " + vectorCellForces.Length + "    " + vectorCellForces[i]._Pos );
            int hash = GetVectorfieldHashMapKey(vectorCellForces[i]._Pos);           
            _CellHashMap.TryAdd(hash, vectorCellForces[i]);
        }

        vectorCellForces.Dispose();
    }

    protected override void OnDestroy()
    {
        _CellHashMap.Dispose();
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        if (_CellHashMap.Length == 0)
            InitHashMap();

        // Foreach mover
        Entities.ForEach((Entity entity, ref Translation translation, ref Mover mover) =>
        {
            VectorfieldCell cell = _CellHashMap[GetVectorfieldHashMapKey(translation.Value)];

            mover._Acc = cell._Force;

            // add acceerlation to velocity and apply drag
            mover._Velocity += mover._Acc * Time.deltaTime;
            mover._Velocity *= 1 - (mover._Drag * Time.deltaTime);

            // add velocity to position
            translation.Value += mover._Velocity * Time.deltaTime;

            Debug.DrawLine(translation.Value, translation.Value + (mover._Velocity * 2));
            Debug.DrawLine(translation.Value, translation.Value + (cell._Force * 4));


            // reset acceleration
            // mover._Acc = float3.zero;
        });
    }

    /*
    protected override void OnUpdate()
    {
        EntityQuery entityQuery = GetEntityQuery(typeof(Translation), typeof(VectorfieldCell));
        NativeArray<Translation> vectorCellTranslations = entityQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        NativeArray<VectorfieldCell> vectorCellForces = entityQuery.ToComponentDataArray<VectorfieldCell>(Allocator.TempJob);

        // Foreach mover
        Entities.ForEach((Entity entity, ref Translation translation, ref Mover mover) =>
        {
            float closestDist = float.MaxValue;
            int closestIndex = 0;

            for (int i = 0; i < vectorCellTranslations.Length; i++)
            {
                float dist = math.distancesq(translation.Value, vectorCellTranslations[i].Value);
                if (dist < closestDist * closestDist)
                {
                    closestDist = dist;
                    closestIndex = i;
                }
            }

            mover._Acc = vectorCellForces[closestIndex]._Force;

            // add acceerlation to velocity and apply drag
            mover._Velocity += mover._Acc * Time.deltaTime;
            mover._Velocity *= 1 - (mover._Drag * Time.deltaTime);

            // add velocity to position
            translation.Value += mover._Velocity * Time.deltaTime;
            Debug.Log("here   " + mover._Velocity);
            Debug.DrawLine(translation.Value, translation.Value + (mover._Velocity*10));     

            // reset acceleration
            // mover._Acc = float3.zero;
        });

        vectorCellTranslations.Dispose();
        vectorCellForces.Dispose();
    }
    */

    public int GetVectorfieldHashMapKey(float3 pos)
    {
        float x = math.clamp(math.floor(pos.x), _HashMin, _HashMax);
        float y = math.clamp(math.floor(pos.y), _HashMin, _HashMax);
        float z = math.clamp(math.floor(pos.z), _HashMin, _HashMax);

        int hash = (int) ( x + (y*1000) + (z*10000));
        //Debug.Log(pos + "    " + hash);
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
                //Debug.DrawLine(vectorCell._Pos, vectorCell._Pos + vectorCell._Force * .4f);              
            }
        );        
    }
}
