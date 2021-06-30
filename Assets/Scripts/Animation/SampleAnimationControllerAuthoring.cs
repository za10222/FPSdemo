using Unity.Entities;
using UnityEngine;

namespace FPSdemo
{
    public class SampleAnimationControllerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public float switchgap;
        public float TransitionDuration;
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData<SampleAnimationController>(
                entity, new SampleAnimationController
                {
                    switchgap = switchgap,
                    TransitionDuration = TransitionDuration
                }
                ); ;
        }
    }
}