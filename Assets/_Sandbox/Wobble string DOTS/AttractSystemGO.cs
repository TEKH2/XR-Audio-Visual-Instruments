using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;
using UnityEngine;

public class AttractSystemGO : ComponentSystem
{
    public float3 _Pos;
    public float _MaxDistSqred;
    public float _Strength;

    protected override unsafe void OnUpdate()
    {
        Entities.ForEach((ref PhysicsVelocity velocity, ref Translation position, ref Rotation rotation) =>
        {
            float3 vectorToPos = _Pos - position.Value;
            float distSqrd = math.lengthsq(vectorToPos);

            if (distSqrd < _MaxDistSqred)
            {
                // Alter linear velocity
                velocity.Linear += _Strength * (vectorToPos / math.sqrt(distSqrd));
            }
        });
    }
};
