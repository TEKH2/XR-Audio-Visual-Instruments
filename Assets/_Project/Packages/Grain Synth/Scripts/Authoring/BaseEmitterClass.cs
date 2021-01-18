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
    public enum EmitterType {Grain, Burst}
    public enum EmitterSetup {Local, Dummy, Temp}

    [Header("Emitter Config")]
    public EmitterType _EmitterType;
    public EmitterSetup _EmitterSetup;
    protected bool _Initialized = false;
    protected bool _StaticallyPaired = false;
    public bool _AttachedToSpeaker = false;
    public int _AttachedSpeakerIndex;
    public GrainSpeakerAuthoring _PairedSpeaker;
    protected Entity _EmitterEntity;
    protected EntityManager _EntityManager;
    protected float[] _PerlinSeedArray;
    protected Transform _HeadPosition;

    [Header("Playback Config")]
    [Range(0.1f, 50f)]
    public float _MaxAudibleDistance = 10f;
    public bool _MultiplyVolumeByColliderRigidity = false;
    protected float _VolumeMultiply = 1;
    public bool _PingPongAtEndOfClip = true;

    [Header("Runtime Activity")]
    protected bool _InRangeTemp = false;
    protected bool _CollisionTriggered = false;
    public bool _Colliding = false;
    public GameObject _ColldingObject;

    public float _CurrentDistance = 0;
    public float _DistanceVolume = 0;
    public bool _WithinEarshot = true;

    [SerializeField]
    protected GameObject _CollidingDummyEmitterGameObject;
    protected List<GameObject> _RemoteInteractions;


    [Header("Sound Config")]
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
        if (_EmitterEntity != null)
            _EntityManager.DestroyEntity(_EmitterEntity);
    }

    private void OnDestroy()
    {
        DestroyEntity();
    }

    // Only for burst emitter types
    public void NewCollision(Collision collision)
    {
        _CollisionTriggered = true;

        if (!_MultiplyVolumeByColliderRigidity)
            _VolumeMultiply = 1;
        else if (collision.collider.GetComponent<SurfaceParameters>() != null)
            _VolumeMultiply = collision.collider.GetComponent<SurfaceParameters>()._Rigidity;

        // Copy dummy emitter if this is a remote emitter
        //if (_EmitterSetup == EmitterSetup.Temp)
        //{
        //    BurstEmitterAuthoring colliderDummyEmitter = collision.collider.GetComponentInChildren<BurstEmitterAuthoring>();
        //    if (colliderDummyEmitter != null && colliderDummyEmitter._EmitterSetup == EmitterSetup.Dummy)
        //        SetRemoteBurstEmitter(collision.collider.GetComponentInChildren<DummyBurstEmitter>());
        //    else
        //        _CollisionTriggered = false;
        //}       
    }

    // Only for grain emitter types
    public void UpdateCurrentCollisionStatus(Collision collision)
    {
        if (collision == null)
        {
            _Colliding = false;
            _VolumeMultiply = 1;

            // Clear emitter if it's set to remote
            //if (_EmitterSetup == EmitterSetup.Temp)
            //{
            //    if (_CollidingDummyEmitterGameObject != null)
            //    {
            //        Destroy(_CollidingDummyEmitterGameObject);
            //        _CollidingDummyEmitterGameObject = null;
            //    }
                    
            //    SetRemoteGrainEmitter(null, null);
            //}
        }
        else
        {
            _Colliding = true;

            if (!_MultiplyVolumeByColliderRigidity)
                _VolumeMultiply = 1;
            else if (collision.collider.GetComponent<SurfaceParameters>() != null)
                _VolumeMultiply = collision.collider.GetComponent<SurfaceParameters>()._Rigidity;

            //if (_EmitterSetup == EmitterSetup.Temp && _CollidingDummyEmitterGameObject != collision.collider.gameObject)
            //{
            //    _CollidingDummyEmitterGameObject = collision.collider.gameObject;

            //    if (collision.collider.GetComponentInChildren<DummyGrainEmitter>() != null)
            //    {
            //        SetRemoteGrainEmitter(collision.collider.GetComponentInChildren<DummyGrainEmitter>(), collision.collider.GetComponentsInChildren<InteractionBase>());
            //    }
            //    else
            //    {
            //        _Colliding = false;
            //    }
                    
            //}
        }
    }

    //public virtual void SetRemoteGrainEmitter(DummyGrainEmitter dummyEmitter, InteractionBase[] remoteInteractions) { }

    //public virtual void SetRemoteBurstEmitter(DummyBurstEmitter dummyEmitter) { }

    public virtual void SetupTempEmitter(GameObject collidingGameObject, GrainSpeakerAuthoring speaker) { }

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
