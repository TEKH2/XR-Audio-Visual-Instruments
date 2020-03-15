using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

// Converts prefabs into entities for later instantiating via a static reference
public class PrefabEntities_V2 : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public static Entity _PrefabEntity;
    public static Entity _PrefabEntity2;
    public static Entity _PrefabEntity3;

    public GameObject _PrefabEntityGameObject;
    public GameObject _PrefabEntityGameObject2;
    public GameObject _PrefabEntityGameObject3;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        Entity prefabEntity = conversionSystem.GetPrimaryEntity(_PrefabEntityGameObject);
        _PrefabEntity = prefabEntity;

        Entity prefabEntity2 = conversionSystem.GetPrimaryEntity(_PrefabEntityGameObject2);
        _PrefabEntity2 = prefabEntity2;

        Entity prefabEntity3 = conversionSystem.GetPrimaryEntity(_PrefabEntityGameObject3);
        _PrefabEntity3 = prefabEntity3;
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(_PrefabEntityGameObject);
        referencedPrefabs.Add(_PrefabEntityGameObject2);
        referencedPrefabs.Add(_PrefabEntityGameObject3);
    }
}
