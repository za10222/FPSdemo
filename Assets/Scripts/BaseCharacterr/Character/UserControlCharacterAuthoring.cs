using UnityEngine;
using UnityEditor;
using Unity.Entities;

namespace FPSdemo {
    public struct UserControlDate : IComponentData
    {

    }
    public class UserControlCharacterAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<UserControlDate>(entity);
        }
    }

}