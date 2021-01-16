using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;
using Random = UnityEngine.Random;




[DisallowMultipleComponent]
[RequiresEntityConversion]
[RequireComponent(typeof(ConvertToEntity))]
public class BaseEmitterClass : MonoBehaviour, IConvertGameObjectToEntity
{
    public enum EmitterType
    {
        Grain,
        Burst
    }

    public bool _TakePropertiesFromCollidingObject = false;

    [Range(0.1f, 50f)]
    public float _MaxAudibleDistance = 10f;

    [Header("DEBUG")]
    public float _CurrentDistance = 0;
    public float _DistanceVolume = 0;
    public bool _WithinEarshot = true;

    protected bool _Initialized = false;
    protected bool _StaticallyPaired = false;
    protected bool _InRangeTemp = false;
    protected bool _CollisionTriggered = false;
    public bool _Colliding = false;
    public string _ColldingObjectName = "";

    private bool _StaticSurface = false;
    public bool _PingPongAtEndOfClip = true;
    public bool _MultiplyVolumeByColliderRigidity = false;
    public float _VolumeMultiply = 1;

    public bool _AttachedToSpeaker = false;
    public int _AttachedSpeakerIndex;
    public GrainSpeakerAuthoring _PairedSpeaker;
    public Transform _HeadPosition;

    protected Entity _EmitterEntity;
    protected EntityManager _EntityManager;
    protected float[] _PerlinSeedArray;

    public EmitterType _EmitterType;

    public DSPBase[] _DSPChainParams;

    void Start()
    {
        _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _HeadPosition = FindObjectOfType<Camera>().transform;

        _PerlinSeedArray = new float[10];
        for (int i = 0; i < _PerlinSeedArray.Length; i++)
        {
            float offset = Random.Range(0, 1000);
            _PerlinSeedArray[i] = Mathf.PerlinNoise(offset, offset * 0.5f);
        }
    }

    public void Awake()
    {
        GetComponent<ConvertToEntity>().ConversionMode = ConvertToEntity.Mode.ConvertAndInjectGameObject;
        Initialise();
    }

    public void DestroyEntity()
    {
        //print("Emitter Entity Destroyed");
        if (_EmitterEntity != null)
            _EntityManager.DestroyEntity(_EmitterEntity);
    }

    private void OnDestroy()
    {
        DestroyEntity();
    }

    public void IsStaticSurface(bool staticSurface)
    {
        _StaticSurface = staticSurface;
    }

    public void NewCollision(Collision collision)
    {
        _CollisionTriggered = true;

        //Debug.Log("New collision of  " + name + "  with  " + collision.collider.name);

        if (!_MultiplyVolumeByColliderRigidity)
            _VolumeMultiply = 1;
        else if (collision.collider.GetComponent<SurfaceParameters>() != null)
            _VolumeMultiply = collision.collider.GetComponent<SurfaceParameters>()._Rigidity;
    }


    // TODO -=- This seems inefficient to set everything OnCollisionStay tick. Doing this to avoid extra collisions
    // from breaking the roll sounds
    public void UpdateCurrentCollisionStatus(Collision collision)
    {
        if (collision == null)
        {
            _Colliding = false;
            _VolumeMultiply = 1;
        }
        else
        {
            if (_TakePropertiesFromCollidingObject)
            {
                if (_EmitterType == EmitterType.Grain)
                    SetRemoteGrainEmitter(collision.collider.GetComponentInChildren<DummyGrainEmitter>());
                else if (_EmitterType == EmitterType.Burst)
                    SetRemoteBurstEmitter(collision.collider.GetComponentInChildren<DummyBurstEmitter>());
            }

            if (_MultiplyVolumeByColliderRigidity && collision.collider.GetComponent<SurfaceParameters>() != null)
                _VolumeMultiply = collision.collider.GetComponent<SurfaceParameters>()._Rigidity;
            _Colliding = true;
        }
    }

    public virtual void SetRemoteGrainEmitter(DummyGrainEmitter dummyEmitter) { }

    public virtual void SetRemoteBurstEmitter(DummyBurstEmitter dummyEmitter) { }

    public virtual void Initialise() { }

    public GrainSpeakerAuthoring DynamicallyAttachedSpeaker { get { return GrainSynth.Instance._GrainSpeakers[_AttachedSpeakerIndex]; } }

    protected void UpdateDSPBuffer(bool clear = true)
    {
        //--- TODO not sure if clearing and adding again is the best way to do this
        DynamicBuffer<DSPParametersElement> dspBuffer = _EntityManager.GetBuffer<DSPParametersElement>(_EmitterEntity);

        if (clear) dspBuffer.Clear();

        for (int i = 0; i < _DSPChainParams.Length; i++)
        {
            dspBuffer.Add(_DSPChainParams[i].GetDSPBufferElement());
        }
    }

    public float GeneratePerlinForParameter(int parameterIndex)
    {
        return Mathf.PerlinNoise(Time.time + _PerlinSeedArray[parameterIndex], (Time.time + _PerlinSeedArray[parameterIndex]) * 0.5f);
    }

    protected void OnDrawGizmos()
    {
        Gizmos.color = _InRangeTemp ? Color.yellow : Color.blue;
        Gizmos.DrawSphere(transform.position, .1f);
    }

    public virtual void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) { }

    protected virtual void UpdateCollisionNumbers(int currentCollisionCount) { }
}
