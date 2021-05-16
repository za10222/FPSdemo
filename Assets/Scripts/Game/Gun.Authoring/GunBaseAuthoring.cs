using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace FPSdemo
{
    [DisallowMultipleComponent]
    public class GunBaseAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<GunManager.GunBaseData>(entity);
            dstManager.AddComponent<GunManager.GunRenderData>(entity);
        }

    }
}
