using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
namespace FPSdemo
{


    public struct HealthData: IComponentData
    {
        public float maxHp;
        public float currentHp;
    }

    [DisallowMultipleComponent]
    public class HealthAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {

        public float maxHp;
        public float currentHp;


        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new HealthData { maxHp = maxHp , currentHp = currentHp });
            dstManager.AddBuffer<HealthEventBufferElement>(entity);
        }
    }
}