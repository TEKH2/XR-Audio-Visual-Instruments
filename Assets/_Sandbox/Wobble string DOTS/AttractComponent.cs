using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public struct AttractComponent : IComponentData
{
    public float _MaxDistanceSqrd;
    public float _Strength;
}
