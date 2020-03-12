using Unity.Entities;
using UnityEngine;


public class AttractComponentGO : MonoBehaviour
{
    public float _MaxDistance = 3;
    public float _Strength = 1;

    AttractSystemGO _AttractSystem;

    private void Start()
    {
        //_AttractSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<AttractSystem>();
    }

    void Update()
    {
        _AttractSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<AttractSystemGO>();
        _AttractSystem._Pos = transform.position;
        _AttractSystem._MaxDistSqred = _MaxDistance * _MaxDistance;
        _AttractSystem._Strength = _Strength;
    }
}
