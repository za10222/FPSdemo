using Unity.Entities;
using UnityEngine;

namespace FPSdemo
{

    public class PlayerCameraControlAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new PlayerCameraControl.State() { isEnabled=1, fieldOfView =60});
        }
    }

}