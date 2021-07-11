using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
namespace FPSdemo
{


    public struct HealthData: IComponentData
    {
        [GhostField]
        public float maxHp;
        [GhostField]
        public float currentHp;

        [GhostField]
        public int lasthit;
    }

    [DisallowMultipleComponent]
    public class HealthAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {

        public float maxHp;
        public float currentHp;


        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new HealthData { maxHp = maxHp , currentHp = currentHp , lasthit =-1});
            dstManager.AddBuffer<HealthEventBufferElement>(entity);
        }
    }
}