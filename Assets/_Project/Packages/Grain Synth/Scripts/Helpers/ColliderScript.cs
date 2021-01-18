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
        ColliderScript newColliderScript = collision.collider.GetComponent<ColliderScript>();
        
        if (newColliderScript != null)
        {
            if (!_CollidingObjects.Contains(newColliderScript.gameObject))
            {
                _CollidingObjects.Add(newColliderScript.gameObject);

                if (_HostDummyEmittersOnCollision)
                    foreach (var remoteDummyEmitter in newColliderScript._DummyEmitters)
                    {
                        GameObject newTempEmitter = Instantiate(remoteDummyEmitter.gameObject, gameObject.transform);
                        newTempEmitter.GetComponent<BaseEmitterClass>().SetupTempEmitter(collision.collider.gameObject, _Speaker);
                        if (newTempEmitter.GetComponent<GrainEmitterAuthoring>() != null)
                            _TempGrainEmitters.Add(newTempEmitter.GetComponent<BaseEmitterClass>());

                        Debug.Log("Created new dummy emitter: " + newTempEmitter.gameObject.name);
                    }
            }
        }

        foreach (var emitter in _LocalEmitters)
        {
            if (emitter._EmitterType == BaseEmitterClass.EmitterType.Burst)
                emitter.NewCollision(collision);
        }

        foreach (var interaction in _Interactions)
        {
            interaction.SetCollisionData(collision);
        }

        _CollidingCount++;
    }

    private void OnCollisionStay(Collision collision)
    {
        foreach (var emitter in _LocalEmitters)
        {
            if (emitter._EmitterType == BaseEmitterClass.EmitterType.Grain)
                emitter.UpdateCurrentCollisionStatus(collision);
        }

        foreach (var interaction in _Interactions)
        {
            interaction.SetColliding(true, collision.collider.material);
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
