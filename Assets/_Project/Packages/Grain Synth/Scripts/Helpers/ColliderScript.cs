using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderScript : MonoBehaviour
{
    public bool _HostDummyEmittersOnCollision = false;
    public GrainSpeakerAuthoring _Speaker;
    public List<BaseEmitterClass> _LocalEmitters;
    public List<BaseEmitterClass> _DummyEmitters;
    public List<BaseEmitterClass> _TempGrainEmitters;
    public InteractionBase[] _Interactions;
    public int _CollidingCount = 0;
    public List<GameObject> _CollidingObjects;
    public GameObject _ThisGameObject;

    private void Start()
    {
        _ThisGameObject = gameObject;
        
        if (_Speaker == null)
            _Speaker = GetComponentInChildren<GrainSpeakerAuthoring>();



        BaseEmitterClass[] emitters = GetComponentsInChildren<BaseEmitterClass>();

        foreach (var emitter in emitters)
        {
            if (emitter._EmitterSetup == BaseEmitterClass.EmitterSetup.Local)
                _LocalEmitters.Add(emitter);
            else if (emitter._EmitterSetup == BaseEmitterClass.EmitterSetup.Dummy)
                _DummyEmitters.Add(emitter);
        }

        _Interactions = GetComponentsInChildren<InteractionBase>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        foreach (var interaction in _Interactions)
        {
            interaction.SetCollisionData(collision);
        }

        ColliderScript remoteColliderScript = collision.collider.GetComponent<ColliderScript>();
        
        if (remoteColliderScript != null)
        {
            if (!_CollidingObjects.Contains(remoteColliderScript.gameObject))
            {
                _CollidingObjects.Add(remoteColliderScript.gameObject);

                if (_HostDummyEmittersOnCollision)
                    foreach (var remoteDummyEmitter in remoteColliderScript._DummyEmitters)
                    {
                        GameObject newTempEmitter = Instantiate(remoteDummyEmitter.gameObject, gameObject.transform);
                        newTempEmitter.GetComponent<BaseEmitterClass>().SetupTempEmitter(collision, _Speaker);

                        if (newTempEmitter.GetComponent<GrainEmitterAuthoring>() != null)
                            _TempGrainEmitters.Add(newTempEmitter.GetComponent<BaseEmitterClass>());
                    }
            }
        }

        foreach (var emitter in _LocalEmitters)
        {
            if (emitter._EmitterType == BaseEmitterClass.EmitterType.Burst)
                emitter.NewCollision(collision);
        }

        _CollidingCount++;
    }

    private void OnCollisionStay(Collision collision)
    {
        foreach (var interaction in _Interactions)
        {
            interaction.SetColliding(true, collision.collider.material);
        }

        foreach (var emitter in _LocalEmitters)
        {
            if (emitter._EmitterType == BaseEmitterClass.EmitterType.Grain)
                emitter.UpdateCurrentCollisionStatus(collision);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        _CollidingCount--;

        foreach (var emitter in _LocalEmitters)
        {
            if (_CollidingCount == 0)
            {
                if (emitter._EmitterType == BaseEmitterClass.EmitterType.Grain)
                    emitter.UpdateCurrentCollisionStatus(null);
            } 
        }

        foreach (var interaction in _Interactions)
        {
            interaction.SetColliding(false, collision.collider.material);
        }

        for (int i = _TempGrainEmitters.Count - 1; i >= 0; i--)
        {
            if (_TempGrainEmitters[i]._ColldingObject == collision.collider.gameObject)
            {
                Destroy(_TempGrainEmitters[i].gameObject);
                _TempGrainEmitters.RemoveAt(i);
            }
        }

        _CollidingObjects.Remove(collision.collider.gameObject);
    }
}
