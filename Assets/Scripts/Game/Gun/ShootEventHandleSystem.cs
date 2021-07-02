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
    [UpdateAfter(typeof(HandlePlayerGunSystem))]
    public class ShootEventHandleSystem : SystemBase
    {
        private BuildPhysicsWorld physicsWorldSystem;
        private EntityQuery m_bufferdateQuery;
        private EntityQuery m_FPCQuery;
        private Entity m_ShootEventPrefab;
        private BeginSimulationEntityCommandBufferSystem m_Barrier;

        protected override void OnCreate()
        {
            RequireSingletonForUpdate<GunDataBufferElement>();
            m_bufferdateQuery = GetEntityQuery(typeof(GunDataBufferElement));
            m_FPCQuery = GetEntityQuery(typeof(GhostOwnerComponent),typeof(CharacterControllerComponentData),typeof(PhysicsCollider));
            physicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();
            m_Barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var w = m_bufferdateQuery.ToEntityArray(Unity.Collections.Allocator.TempJob);
            var FPCs= m_FPCQuery.ToEntityArray(Unity.Collections.Allocator.TempJob);
            var t = EntityManager.GetBuffer<GunDataBufferElement>(w[0]);
 
            var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;
          
         
            int worldname = string.Equals(World.Name, "clientworld123")?10:5;

            var commandBuffer = m_Barrier.CreateCommandBuffer().AsParallelWriter();
            
            if (m_ShootEventPrefab == Entity.Null)
            {
                var prefabEntity = GetSingletonEntity<GhostPrefabCollectionComponent>();
                var prefabs = EntityManager.GetBuffer<GhostPrefabBuffer>(prefabEntity);
                var foundPrefab = Entity.Null;
                for (int i = 0; i < prefabs.Length; ++i)
                {
                    if (EntityManager.HasComponent<ShootEventData>(prefabs[i].Value))
                        foundPrefab = prefabs[i].Value;
                }
                if (foundPrefab != Entity.Null)
                    m_ShootEventPrefab = GhostCollectionSystem.CreatePredictedSpawnPrefab(EntityManager, foundPrefab);
            }
            var m_ShootEventPrefab2 = m_ShootEventPrefab;
            if (m_ShootEventPrefab2 == Entity.Null)
                return;
            Entities
            .WithName("ShootEventHandleJob")
            .WithNone<Prefab>()
            .ForEach((Entity entity, int nativeThreadIndex, ref GunManager.ShootBeginData shootBeginData) =>
            {
                if (shootBeginData.tobedelete == true)
                    return;
                var ghostOwnerComponentFromEntity = GetComponentDataFromEntity<GhostOwnerComponent>(true);
                var physicsColliderFromEntity = GetComponentDataFromEntity<PhysicsCollider>(true);
                var HealthEventBuffer = GetBufferFromEntity<HealthEventBufferElement>(false);
                shootHitRaycastWithoutShooterBody(ref shootBeginData, in collisionWorld, in FPCs, in physicsColliderFromEntity,
                        in ghostOwnerComponentFromEntity,in HealthEventBuffer,in m_ShootEventPrefab2, in commandBuffer, nativeThreadIndex, worldname,entity.Index);
                commandBuffer.DestroyEntity(nativeThreadIndex, entity);
                //shootEventData.gunBaseData.

            }).Schedule();
            Dependency.Complete();
            m_Barrier.AddJobHandleForProducer(Dependency);
         
        }
     
        static public void shootHitRaycastWithoutShooterBody(ref GunManager.ShootBeginData shootBeginData,
            in CollisionWorld collisionWorld,in NativeArray<Entity> FPCs,in ComponentDataFromEntity<PhysicsCollider> physicsColliderFromEntity,
            in ComponentDataFromEntity<GhostOwnerComponent> ghostOwnerComponentFromEntity,
            in BufferFromEntity<HealthEventBufferElement> healthEventBufferFromEntity,in Entity m_ShootEventPrefab,in EntityCommandBuffer.ParallelWriter commandBuffer,int  nativeThreadIndex, int worldname, int index)
        {
        
            //寻找发射的FPC
            PhysicsCollider shooter=default;
            bool isfind = false;
            foreach (var i in FPCs)
            {
                if(ghostOwnerComponentFromEntity[i].NetworkId== shootBeginData.owner)
                {
                    shooter = physicsColliderFromEntity[i];
                    isfind = true;
                    break;
                }
            }
            if (!isfind)
                return ;

            var e = commandBuffer.Instantiate(nativeThreadIndex, m_ShootEventPrefab);
            var shootEventData = new ShootEventData
            {
                gunBaseData = shootBeginData.gunBaseData,
                owner = shootBeginData.owner,
                translation = shootBeginData.translation,
                rotation = shootBeginData.rotation,
                spawntick = shootBeginData.spawntick,
                muzzleTran = shootBeginData.muzzleTran
            };


            commandBuffer.SetComponent(nativeThreadIndex, e,
              new GhostOwnerComponent { NetworkId = shootBeginData.owner });

            //保存旧的配置
            var oldFilter =shooter.Value.Value.Filter;
            var tempFilter=shooter.Value.Value.Filter ;
            tempFilter.GroupIndex = -5;
            shooter.Value.Value.Filter = tempFilter;

            //或许会有同步问题？ 因为并行多个body的groupid可能被设置为-5 
            float3 gunPos = shootEventData.translation.Value;
            float3 target = math.forward(shootEventData.rotation.Value) * 50f + gunPos;
            Unity.Physics.RaycastHit hit = new Unity.Physics.RaycastHit();
            var ishit = CommonUtilities.Raycast(gunPos, target, in collisionWorld, out hit);
            var  bt=collisionWorld.Bodies[hit.RigidBodyIndex].Collider.Value.Filter.BelongsTo;
  
            var cw = collisionWorld.Bodies[hit.RigidBodyIndex].Collider.Value.Filter.CollidesWith;

            //还原！
            shooter.Value.Value.Filter = oldFilter;
            if (ishit)
            {

                var hitPointPos = hit.Position;
                var dis = math.distance(hitPointPos, gunPos);
                float time = math.mul(dis - 2f, 1 / shootEventData.gunBaseData.ballisticVelocity);
                if (time < 0)
                    time = 0.01f;
                shootEventData.lifetime = time;
                shootEventData.hitPosition = hitPointPos;
                shootEventData.hitSurfaceNormal = hit.SurfaceNormal;    //hit.SurfaceNormal
                if(healthEventBufferFromEntity.HasComponent(hit.Entity))
                {

                    //Debug.Log(string.Format("命中+{0}+{1}+{2}+{3}",worldname,hit.Entity.Index,bt,cw));
                    healthEventBufferFromEntity[hit.Entity].Add(new HealthEventBufferElement { healthChange = -10 });
                }
            }
            else
            {
                //Debug.Log("没有");
                shootEventData.lifetime = -1;
            }
            shootBeginData.tobedelete = true;
            commandBuffer.SetComponent(nativeThreadIndex, e,shootEventData);
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
                if (SequenceHelpers.IsNewer(currentTick, shootEventData.spawntick+30))
                {
                    commandBuffer.DestroyEntity(entityInQueryIndex,ent);
                }
            }).ScheduleParallel();
            m_barrier.AddJobHandleForProducer(Dependency);
        }
    }

}