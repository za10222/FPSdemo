using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport.Utilities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using static FPSdemo.GunManager;

namespace FPSdemo
{


    //[DisableAutoCreation]
    //[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
    [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
    [UpdateAfter(typeof(PlayGunUserInputUpdateSystem))]
    public class ShootEventHandleSystem : SystemBase
    {
        private BuildPhysicsWorld physicsWorldSystem;
        private EntityQuery m_bufferdateQuery;
        private EntityQuery m_FPCQuery;
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<GunDataBufferElement>();
            m_bufferdateQuery = GetEntityQuery(typeof(GunDataBufferElement));
            m_FPCQuery = GetEntityQuery(typeof(GhostOwnerComponent),typeof(CharacterControllerComponentData),typeof(PhysicsCollider));
            physicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();
            
        }

        protected override void OnUpdate()
        {
            var w = m_bufferdateQuery.ToEntityArray(Unity.Collections.Allocator.TempJob);
            var FPCs= m_FPCQuery.ToEntityArray(Unity.Collections.Allocator.TempJob);
            var t = EntityManager.GetBuffer<GunDataBufferElement>(w[0]);
 
            var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;
            var physicsColliderFromEntity = GetComponentDataFromEntity<PhysicsCollider>(true);
            var ghostOwnerComponentFromEntity = GetComponentDataFromEntity<GhostOwnerComponent>(true);

            Entities
            .WithName("ShootEventHandleJob")
            .WithAll<GunManager.ShootEventData>()
            .WithReadOnly(physicsColliderFromEntity)
            .WithReadOnly(ghostOwnerComponentFromEntity)
            .WithNone<Prefab>()
            .ForEach((ref GunManager.ShootEventData shootEventData) =>
            {
                
                if (shootEventData.ishandle==false)
                {
                    shootHitRaycastWithoutShooterBody(ref shootEventData, in collisionWorld, in FPCs, in physicsColliderFromEntity,
                        in ghostOwnerComponentFromEntity);
                }
                //shootEventData.gunBaseData.
            }).Schedule();
        }
     
        static public void shootHitRaycastWithoutShooterBody(ref GunManager.ShootEventData  shootEventData,
            in CollisionWorld collisionWorld,in NativeArray<Entity> FPCs,in ComponentDataFromEntity<PhysicsCollider> physicsColliderFromEntity,
            in ComponentDataFromEntity<GhostOwnerComponent> ghostOwnerComponentFromEntity)
        {
            //寻找发射的FPC
             PhysicsCollider shooter=default;
            bool isfind = false;
            foreach (var i in FPCs)
            {
                if(ghostOwnerComponentFromEntity[i].NetworkId== shootEventData.owner)
                {
                    shooter = physicsColliderFromEntity[i];
                    isfind = true;
                    break;
                }
            }
            if (!isfind)
                return ;

            //保存旧的配置
            var oldFilter=shooter.Value.Value.Filter;
            var tempFilter=shooter.Value.Value.Filter ;
            tempFilter.GroupIndex = -5;
            shooter.Value.Value.Filter = tempFilter;

            //或许会有同步问题？ 因为并行多个body的groupid可能被设置为-5 
            float3 gunPos = shootEventData.translation.Value;
            float3 target = math.forward(shootEventData.rotation.Value) * 50f + gunPos;
            Unity.Physics.RaycastHit hit = new Unity.Physics.RaycastHit();
            var ishit = CommonUtilities.Raycast(gunPos, target, in collisionWorld, out hit);

            //还原！
            shooter.Value.Value.Filter = oldFilter;
            if (ishit)
            {

                //Debug.Log("命中");
                var hitPointPos = hit.Position;
                var dis = math.distance(hitPointPos, gunPos);
                float time = math.mul(dis - 2f, 1 / shootEventData.gunBaseData.ballisticVelocity);
                if (time < 0)
                    time = 0.01f;
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

     
    }

    [UpdateInGroup(typeof(ServerSimulationSystemGroup))]
    public class ShootEventCleanSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem m_barrier;
        private ServerSimulationSystemGroup m_ServerSimulationSystemGroup;

        protected override void OnCreate()
        {
            RequireSingletonForUpdate<GunDataBufferElement>();
            m_barrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            m_ServerSimulationSystemGroup = World.GetExistingSystem<ServerSimulationSystemGroup>();
        }

        protected override void OnUpdate()
        {
            var currentTick = m_ServerSimulationSystemGroup.ServerTick;
            var commandBuffer = m_barrier.CreateCommandBuffer().AsParallelWriter();
            Entities
            .WithName("ShootEventCleanJob")
            .WithAll<GunManager.ShootEventData>()
            .ForEach((Entity ent, int entityInQueryIndex, ref GunManager.ShootEventData shootEventData) =>
            {
                if (shootEventData.ishandle == true && SequenceHelpers.IsNewer(currentTick, shootEventData.spawntick+30))
                {
                    commandBuffer.DestroyEntity(entityInQueryIndex,ent);
                }
            }).ScheduleParallel();
            m_barrier.AddJobHandleForProducer(Dependency);
        }
    }

}