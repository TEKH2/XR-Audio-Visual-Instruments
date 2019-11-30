using UnityEngine;
using Unity.Entities;

public class LevelTesting : MonoBehaviour
{
    private void Start()
    {
        EntityManager entityManager = World.Active.EntityManager;

        Entity entity = entityManager.CreateEntity(typeof(LevelComponent));

        entityManager.SetComponentData(entity, new LevelComponent { level = 10 });
    }
}