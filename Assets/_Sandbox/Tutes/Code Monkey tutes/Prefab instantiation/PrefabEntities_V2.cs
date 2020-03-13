using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class PrefabEntities_V2 : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public static Entity _PrefabEntity;
    public GameObject _PrefabEntityGameObject;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        Entity prefabEntity = conversionSystem.GetPrimaryEntity(_PrefabEntityGameObject);
        _PrefabEntity = prefabEntity;
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(_PrefabEntityGameObject);
    }
}
