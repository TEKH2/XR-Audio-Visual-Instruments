using UnityEngine;
using Unity.Entities;

/// <summary>
/// ComponentSystem allows for this code to be executed on its own,
/// without being attached to any entities or game objects in Unity
/// </summary>
public class LevelSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        // Iterate through all entities containing a LevelComponent
        Entities.ForEach((ref LevelComponent levelComponent) =>
        {
            // Increment level by 1 per second
            levelComponent.level += 1f * Time.deltaTime;
        });
    }
}