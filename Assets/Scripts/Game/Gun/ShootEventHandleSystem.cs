using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using static FPSdemo.GunManager;

namespace FPSdemo
{
 

    //[DisableAutoCreation]
    [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
    [UpdateAfter(typeof(PlayGunUserInputUpdateSystem))]
    public class ShootEventHandleSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<GunDataBufferElement>();
            m_bufferdateQuery = GetEntityQuery(typeof(GunDataBufferElement));
        }

        protected override void OnUpdate()
        {
            var w = m_bufferdateQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            var t = EntityManager.GetBuffer<GunDataBufferElement>(w[0]);
            Entities
            .WithName("ShootEventHandleJob")
            .WithAll<GunManager.ShootEventData>()
            .ForEach((ref GunManager.ShootEventData shootEventData) =>
            {
                var physicsWorldSystem = World.GetExistingSystem<Unity.Physics.Systems.BuildPhysicsWorld>();
                //float3 gunPos=;

                CommonUtilities.Raycast(shootEventData.translation.Value,,physicsWorldSystem);
                //shootEventData.gunBaseData.
            }).Run();
        }
        EntityQuery m_bufferdateQuery;


     
    }


}