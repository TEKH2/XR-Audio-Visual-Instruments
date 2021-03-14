using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerParticlesForce
{
    private GameObject _ForcePrefab;
    private GameObject _ForceObject;
    private Transform _Parent;
    private bool _State;
    private float _Amount;


    public ControllerParticlesForce(GameObject prefab, Transform parent)
    {
        _ForcePrefab = prefab;
        _Parent = parent;
        _State = false;

        _ForceObject = GameObject.Instantiate(prefab);
        _ForceObject.transform.parent = parent;
        _ForceObject.transform.localPosition = Vector3.zero;
    }

    public void UpdateState(bool state)
    {
        _State = state;
    }
}
