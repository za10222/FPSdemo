using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static FPSdemo.GunManager;

namespace FPSdemo
{
    //    //[DisableAutoCreation]

        public struct RenderLifetime : IComponentData
    {
        public float lifetime;
    }

    public struct ProjectileData : IComponentData
    {
       public Translation startTranslation;
       public Rotation rotation;
    }

    [UpdateInGroup(typeof(ClientPresentationSystemGroup))]
    [UpdateAfter(typeof(UpdateGunSystem))]

    public class ShootDataUpdateSystem : SystemBase
    {
        //private BeginPresentationEntityCommandBufferSystem m_Barrier;
        private EntityQuery m_bufferdateQuery;

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
                .WithName("ShootDataUpdateJob")
                .WithReadOnly(t)
                .WithAll<ShootEventData, ShootRenderData>()
                .ForEach((Entity entity, int entityInQueryIndex, ref ShootRenderData shootRenderData, in ShootEventData shootEventData ) =>
                {
                    if(shootRenderData.ProjectilePrefab==Entity.Null)
                    {
                        var gunrenderdata = t[shootEventData.gunBaseData.gunTypeIndex].gunData.gunRenderData;
                        shootRenderData = new ShootRenderData { ProjectilePrefab = gunrenderdata.ProjectileEntity, ProjectilePrefabLifetime = 3 };
                    }
                }).ScheduleParallel();
        }

    }


    [UpdateInGroup(typeof(ClientPresentationSystemGroup))]
    [UpdateAfter(typeof(ShootDataUpdateSystem))]
    public class ShootRenderSystem : SystemBase
    {
        private BeginPresentationEntityCommandBufferSystem m_Barrier;

        protected override void OnCreate()
        {
            RequireSingletonForUpdate<GunDataBufferElement>();
            m_Barrier = World.GetOrCreateSystem<BeginPresentationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var df = Time.DeltaTime;
            var ecb = m_Barrier.CreateCommandBuffer().AsParallelWriter();
             Entities
                .WithName("ShootRenderJob")
                .WithAll<ShootEventData, ShootRenderData>()
                .ForEach((Entity entity, int entityInQueryIndex, ref ShootRenderData shootRenderData, in ShootEventData shootEventData) =>
                {
                    if (shootRenderData.ProjectilePrefab != Entity.Null&& shootRenderData.isRender==false)
                    {
                        var e = ecb.Instantiate(entityInQueryIndex, shootRenderData.ProjectilePrefab);
                        ecb.AddComponent(entityInQueryIndex, e, new RenderLifetime { lifetime = shootRenderData.ProjectilePrefabLifetime });
                        ecb.AddComponent(entityInQueryIndex, e, new ProjectileData
                        {
                            startTranslation = shootEventData.translation,
                            rotation = shootEventData.rotation
                        });
                        ecb.SetComponent<Rotation>(entityInQueryIndex, e, new Rotation
                        {
                            Value = shootEventData.rotation.Value
                        }) ;
                        ecb.SetComponent<Translation>(entityInQueryIndex, e, new Translation
                        {
                            Value = shootEventData.translation.Value
                        });
                        ecb.SetComponent<PhysicsVelocity>(entityInQueryIndex, e, new PhysicsVelocity
                        {
                            //Angular= hootEventData.rotation.Value,
                            Linear = math.forward(shootEventData.rotation.Value) * 50
                        });

                        shootRenderData.isRender = true;
                    }
                }).ScheduleParallel();

            //Entities
            // .WithName("ProjectileUpdateJob")
            // .WithAll<RenderLifetime, ProjectileData>()
            // .ForEach((Entity entity, int entityInQueryIndex, ref ProjectileData projectileData, ref Translation translation,ref Rotation rotation) =>
            // {
            //     rotation = projectileData.rotation;
            //     translation = projectileData.startTranslation;
            //     //translation += math.mul(df, rotation);
            // }).ScheduleParallel();

            m_Barrier.AddJobHandleForProducer(Dependency);
        }

}



    [UpdateInGroup(typeof(ClientPresentationSystemGroup))]
    //[UpdateAfter(typeof(UpdateShootRenderSystem))]
    public class CleanShootRenderSystem : SystemBase
    {
        private BeginPresentationEntityCommandBufferSystem m_Barrier;

        protected override void OnCreate()
        {
            m_Barrier = World.GetOrCreateSystem<BeginPresentationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var df = Time.DeltaTime;
            var ecb = m_Barrier.CreateCommandBuffer().AsParallelWriter();
            //Entities
            //.WithName("CleanShootRenderJob")
            //.WithAll<ShootRenderSpawnState>()
            //.WithNone<ShootEventData>()
            //.ForEach((Entity ent, int entityInQueryIndex, in ShootRenderSpawnState shootRenderSpawnState) =>
            //{
            //    ecb.DestroyEntity(entityInQueryIndex, shootRenderSpawnState.MuzzleEntity);
            //    ecb.RemoveComponent<ShootRenderSpawnState>(entityInQueryIndex, ent);
            //}).ScheduleParallel();


            Entities
            .WithName("ShootRenderLifetimeJob")
            .ForEach((Entity ent, int entityInQueryIndex, ref RenderLifetime renderLifetime) =>
            {
                if (renderLifetime.lifetime > 0)
                {
                    renderLifetime.lifetime -= df;
                }
                else
                {
                    ecb.DestroyEntity(entityInQueryIndex, ent);
                }
            }).ScheduleParallel();

            m_Barrier.AddJobHandleForProducer(Dependency);
        }
    }
}