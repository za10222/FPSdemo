using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using static FPSdemo.GunManager;

namespace FPSdemo
{
    //[DisableAutoCreation]


    public struct GunSpawnState: ISystemStateComponentData
    {
        public Entity Gun;
        public int gunTypeIndex;
        public Entity Muzzle;
    }

    [UpdateInGroup(typeof(ClientPresentationSystemGroup))]
    public class GunUpdateSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<GunDataBufferElement>();
            m_bufferdateQuery = GetEntityQuery(typeof(GunDataBufferElement));
        }

        protected override void OnUpdate()
        {
            var w=m_bufferdateQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            var t = EntityManager.GetBuffer<GunDataBufferElement>(w[0]);
            Entities
                .WithName("GunDataUpdateJob")
                .WithReadOnly(t)
                .WithAll<PlayerGunData, GunBaseData, GunRenderData>()
                .ForEach((Entity entity, int entityInQueryIndex,ref GunBaseData gunBaseData, ref GunRenderData gunRenderData,in PlayerGunData playerGunData) =>
                {
                    gunBaseData = t[playerGunData.gunTypeIndex].gunData.gunBaseData;
                    gunRenderData = t[playerGunData.gunTypeIndex].gunData.gunRenderData;
 
                }).Schedule();
        }
        EntityQuery m_bufferdateQuery;
    }


    [UpdateInGroup(typeof(ClientPresentationSystemGroup))]
    [UpdateAfter(typeof(GunUpdateSystem))]
    public class GunRenderSystem : SystemBase
    {

        private BeginPresentationEntityCommandBufferSystem m_Barrier;

        protected override void OnCreate()
        {
            RequireSingletonForUpdate<GunDataBufferElement>();
            m_Barrier = World.GetOrCreateSystem<BeginPresentationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var ecb = m_Barrier.CreateCommandBuffer().AsParallelWriter();



            Entities
                .WithName("GunSpwanJob")
                .WithAll<PlayerGunData, GunBaseData, GunRenderData>()
                .WithNone<GunSpawnState>()
                .ForEach((Entity entity, int entityInQueryIndex,ref GunBaseData gunBaseData, ref GunRenderData gunRenderData, in LocalToWorld lot) =>
                {
                    if (gunRenderData.GunModelEntity == Entity.Null)
                    {
                        return;
                    }
                   var e= ecb.Instantiate(entityInQueryIndex, gunRenderData.GunModelEntity);
                   var m = ecb.Instantiate(entityInQueryIndex, gunRenderData.MuzzleEntity);
                    
                    ecb.AddComponent<GunSpawnState>(entityInQueryIndex, entity,
                       new GunSpawnState
                       {Gun= e, 
                       gunTypeIndex = gunBaseData .gunTypeIndex
                       ,
                           Muzzle = m
                       });
                    ecb.SetComponent<Translation>(entityInQueryIndex, e, new Translation { Value = lot.Position });
                    ecb.SetComponent<Rotation>(entityInQueryIndex, e, new Rotation { Value = lot.Rotation });

                }).ScheduleParallel();

            Entities
               .WithName("GunChangeRenderJob")
               .WithAll<PlayerGunData, GunRenderData, GunSpawnState>()
               .ForEach((Entity entity, int entityInQueryIndex, ref GunBaseData gunBaseData,ref GunRenderData gunRenderData, ref GunSpawnState gunSpawn, in LocalToWorld lot) =>
               {
                   if (gunSpawn.gunTypeIndex!= gunBaseData.gunTypeIndex)
                   {
                       ecb.DestroyEntity(entityInQueryIndex, gunSpawn.Gun);
                       ecb.DestroyEntity(entityInQueryIndex, gunSpawn.Muzzle);
                       var e = ecb.Instantiate(entityInQueryIndex, gunRenderData.GunModelEntity);
                       var m = ecb.Instantiate(entityInQueryIndex, gunRenderData.MuzzleEntity);
                       ecb.SetComponent<GunSpawnState>(entityInQueryIndex, entity, new GunSpawnState { Gun = e ,
                           gunTypeIndex = gunBaseData.gunTypeIndex
                           ,Muzzle=m
                       });
                       ecb.SetComponent<Translation>(entityInQueryIndex, e, new Translation { Value = lot.Position });
                       ecb.SetComponent<Rotation>(entityInQueryIndex, e, new Rotation { Value = lot.Rotation });
                   }
                  
               }).ScheduleParallel();
            m_Barrier.AddJobHandleForProducer(Dependency);
        }
        
    }

    [UpdateInGroup(typeof(ClientPresentationSystemGroup))]
    [UpdateAfter(typeof(GunRenderSystem))]
    public class UpdateGunSystem : SystemBase
    {
        private BeginPresentationEntityCommandBufferSystem m_Barrier;

        protected override void OnCreate()
        {
            m_Barrier = World.GetOrCreateSystem<BeginPresentationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {

            var bufferFromEntity = GetBufferFromEntity<LinkedEntityGroup>(true);

            var ecb = m_Barrier.CreateCommandBuffer().AsParallelWriter();
            //更新位置
            Entities
            .WithName("UpdateGunJob")
            .WithAll<PlayerGunData, GunSpawnState>()
            .WithReadOnly(bufferFromEntity)
            .ForEach((Entity ent, int entityInQueryIndex, in GunSpawnState gunEntity, in LocalToWorld ltw) =>
            {
                SetComponent<Translation>(gunEntity.Gun, new Translation { Value = ltw.Position });
                SetComponent<Rotation>(gunEntity.Gun, new Rotation { Value = ltw.Rotation });

                var child = bufferFromEntity[gunEntity.Gun];
                Entity find = Entity.Null;
                for (int i = 0; i < child.Length; i++)
                {
                    find = child[i].Value;
                    if (HasComponent<Muzzle>(find))
                        break;
                }
                if (find == Entity.Null)
                {
                    return;
                } 

                var ltp = GetComponent<LocalToParent>(find);
                RigidTransform localToWorld_parent = new RigidTransform(ltw.Value);
                RigidTransform localtoparent = new RigidTransform(ltp.Value);
                RigidTransform newltw = math.mul(localToWorld_parent, localtoparent);
                SetComponent<Translation>(gunEntity.Muzzle, new Translation { Value = newltw.pos });
                SetComponent<Rotation>(gunEntity.Muzzle, new Rotation { Value = newltw.rot });

            }).Schedule();

            Entities
            .WithName("CleanGunJob")
            .WithAll< GunSpawnState>()
            .WithNone<PlayerGunData>()
            .ForEach((Entity ent, int entityInQueryIndex, in GunSpawnState gunEntity) =>
            {
                ecb.DestroyEntity(entityInQueryIndex, gunEntity.Gun);
                ecb.DestroyEntity(entityInQueryIndex, gunEntity.Muzzle);
                ecb.RemoveComponent<PlayerGunData>(entityInQueryIndex, ent);
            }).ScheduleParallel();

            m_Barrier.AddJobHandleForProducer(Dependency);
        }
    }

    [UpdateInGroup(typeof(ClientPresentationSystemGroup))]
    [UpdateAfter(typeof(GunRenderSystem))]
    public class MuzzleRenderSystem : SystemBase
    {

        private BeginPresentationEntityCommandBufferSystem m_Barrier;

        protected override void OnCreate()
        {
            RequireSingletonForUpdate<GunDataBufferElement>();
            m_Barrier = World.GetOrCreateSystem<BeginPresentationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var ecb = m_Barrier.CreateCommandBuffer().AsParallelWriter();
            Entities
                .WithName("HandleMuzzleJob")
                .WithAll<PlayerGunData, GunRenderData, GunSpawnState>()
                .WithoutBurst()
                .ForEach((Entity entity, int entityInQueryIndex, ref GunSpawnState gunSpawnState, ref GunRenderData gunRenderData,in PlayerGunData playerGunData) =>
                {
                    if(gunRenderData.MuzzleEntity==Entity.Null|| gunSpawnState.Muzzle == Entity.Null)
                    {
                        return;
                    }
         
                    if (playerGunData.gunstate==Gunstate.shoot)
                    {
                       var t=EntityManager.GetBuffer<LinkedEntityGroup>(gunSpawnState.Muzzle);
                       var psroot = EntityManager.GetComponentObject<ParticleSystem>(gunSpawnState.Muzzle);
                     
                       if(!psroot.isPlaying)
                        {
                            Debug.Log("开火");
                        foreach (var w in t)
                        {
                            var ps=EntityManager.GetComponentObject<ParticleSystem>(w.Value);
                            ps.Play();
                        }
                        }
                    }
                    else
                    {
                        var t = EntityManager.GetBuffer<LinkedEntityGroup>(gunSpawnState.Muzzle);
                        foreach (var w in t)
                        {
                            var ps = EntityManager.GetComponentObject<ParticleSystem>(w.Value);
                                ps.Stop();
                        }
                    }
                    //ecb.AddComponent(entityInQueryIndex, e, new RenderLifetime {lifetime=2 });
                    //ecb.SetComponent();
                }).Run();
            m_Barrier.AddJobHandleForProducer(Dependency);
        }

    }



}