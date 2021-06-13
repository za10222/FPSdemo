using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics.Systems;
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
        private BuildPhysicsWorld physicsWorldSystem;
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<GunDataBufferElement>();
            m_bufferdateQuery = GetEntityQuery(typeof(GunDataBufferElement));
            physicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();
        }

        protected override void OnUpdate()
        {
            var w = m_bufferdateQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            var t = EntityManager.GetBuffer<GunDataBufferElement>(w[0]);
          
            var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;
            Entities
            .WithName("ShootEventHandleJob")
            .WithAll<GunManager.ShootEventData>()
            .ForEach((ref GunManager.ShootEventData shootEventData) =>
            {
                if(shootEventData.ishandle==false)
                {

       
                float3 gunPos =shootEventData.translation.Value;
                float3 target = math.forward(shootEventData.rotation.Value)*50f+ gunPos;
                Unity.Physics.RaycastHit hit= new Unity.Physics.RaycastHit();
                var ishit=CommonUtilities.Raycast(gunPos,target,in collisionWorld, out hit);
                if(ishit)
                {
                    //Debug.Log("命中");
                    var hitPointPos=hit.Position;
                    var dis=math.distance(hitPointPos, gunPos);
                    float time = math.mul(dis-1.3f, 1/shootEventData.gunBaseData.ballisticVelocity);
                    if (time < 0)
                        time = 0.1f;
                    shootEventData.lifetime = time;
                    shootEventData.hitPosition = hitPointPos;
                    shootEventData.hitSurfaceNormal = hit.SurfaceNormal;    //hit.SurfaceNormal
                }
                    else
                {
                    //Debug.Log("没有");
                    shootEventData.lifetime = -1;
                }
                shootEventData.ishandle = true;
                }
                //shootEventData.gunBaseData.
            }).Schedule();
        }
        EntityQuery m_bufferdateQuery;


     
    }


}