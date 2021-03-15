
using Unity.Entities;
using UnityEngine;

namespace FPSdemo
{
    public class UserCommandBufferAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddBuffer<UserCommand>(entity);
        }
    }

}
