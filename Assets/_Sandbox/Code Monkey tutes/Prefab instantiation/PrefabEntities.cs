using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class PrefabEntities : MonoBehaviour, IConvertGameObjectToEntity
{
    public static Entity _PrefabEntity;
    public GameObject _PrefabEntityGameObject;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        using (BlobAssetStore blobAssetStore = new BlobAssetStore())
        {
            Entity prefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy
            (
                _PrefabEntityGameObject,
                GameObjectConversionSettings.FromWorld(dstManager.World, blobAssetStore)
            );

            PrefabEntities._PrefabEntity = prefabEntity;
        }
    }
}
