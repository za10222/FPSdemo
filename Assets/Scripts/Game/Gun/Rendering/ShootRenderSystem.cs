﻿using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static FPSdemo.GunManager;

namespace FPSdemo
{
    public struct ShootisRenderTag : IComponentData
    {
    }
    public struct VXFEntityTag : IComponentData
    {
    }
    //    //[DisableAutoCreation]
    public struct RenderLifeTime : IComponentData
    {
        public float lifetime;
    }
    public struct ProjectileLifetime : IComponentData
    {
        public float lifetime;
    }

    public struct ProjectileData : IComponentData
    {
       public Translation startTranslation;
       public Rotation rotation;
       public Entity VFXPrefab;
       public float VFXLifeTime;
       public float3 hitPosition;
       public float3 hitSurfaceNormal;
       public bool ishit;
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
                        shootRenderData.ProjectilePrefab = gunrenderdata.ProjectileEntity;
                        shootRenderData.ProjectileLifetime = 3 ;
                    }
                    if (shootRenderData.VFXPrefab == Entity.Null)
                    {
                        var gunrenderdata = t[shootEventData.gunBaseData.gunTypeIndex].gunData.gunRenderData;
                        shootRenderData.VFXPrefab = gunrenderdata.VFXEntity;
                        shootRenderData.VFXLifetime = 0.2f;
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
                .WithNone<ShootisRenderTag>()
                .ForEach((Entity entity, int entityInQueryIndex, ref ShootRenderData shootRenderData, in ShootEventData shootEventData) =>
                {
                    if (shootRenderData.ProjectilePrefab != Entity.Null)
                    {
                        if (shootEventData.lifetime > 0f && shootEventData.lifetime < 0.015f)
                        {
                            var t = ecb.Instantiate(entityInQueryIndex, shootRenderData.VFXPrefab);
                            ecb.AddComponent(entityInQueryIndex, t, new RenderLifeTime { lifetime = shootRenderData.VFXLifetime });
                            ecb.AddComponent(entityInQueryIndex, t, new VXFEntityTag { });

                            ecb.SetComponent<Rotation>(entityInQueryIndex, t, new Rotation
                            {
                                Value = quaternion.Euler(shootEventData.hitSurfaceNormal)
                            });
                            ecb.SetComponent<Translation>(entityInQueryIndex, t, new Translation
                            {
                                Value = shootEventData.hitPosition
                            });
                            ecb.AddComponent(entityInQueryIndex, entity, new ShootisRenderTag());
                            return;
                        }
                        Debug.Log(string.Format("产生轨迹，{0}",entity.Index));
                        var e = ecb.Instantiate(entityInQueryIndex, shootRenderData.ProjectilePrefab);

                        if (shootEventData.lifetime < 0)
                        {
                           ecb.AddComponent(entityInQueryIndex, e, new ProjectileLifetime { lifetime = shootRenderData.ProjectileLifetime });
                            //ecb.AddComponent(entityInQueryIndex, e, new Entity {  = shootRenderData.ProjectileLifetime });
                        }
                        else
                        {
                            ecb.AddComponent(entityInQueryIndex, e, new ProjectileLifetime { lifetime = shootEventData.lifetime });
                            //ecb.AddComponent(entityInQueryIndex, e, new ProjectileLifetime { lifetime = 10 });

                            //ecb.AddComponent(entityInQueryIndex, e, new ProjectileLifetime { lifetime = shootRenderData.ProjectileLifetime });
                        }

                        ecb.AddComponent(entityInQueryIndex, e, new ProjectileData
                        {
                            startTranslation = shootEventData.translation,
                            rotation = shootEventData.rotation,
                            VFXPrefab = shootRenderData.VFXPrefab,
                            VFXLifeTime = shootRenderData.VFXLifetime,
                            hitPosition = shootEventData.hitPosition,
                            hitSurfaceNormal=shootEventData.hitSurfaceNormal,
                            ishit= shootEventData.lifetime >0
                        }) ;


                        if (shootEventData.lifetime < 0)
                        {
       

                            ecb.SetComponent<Rotation>(entityInQueryIndex, e, new Rotation
                            {
                                Value = shootEventData.rotation.Value
                            });
                            ecb.SetComponent<Translation>(entityInQueryIndex, e, new Translation
                            {
                                Value = shootEventData.muzzleTran
                            });
                            ecb.SetComponent<PhysicsVelocity>(entityInQueryIndex, e, new PhysicsVelocity
                            {
                                //Angular= hootEventData.rotation.Value,
                                //Linear = math.forward(shootEventData.rotation.Value) * shootEventData.gunBaseData.ballisticVelocity
                                Linear = math.forward(shootEventData.rotation.Value) * shootEventData.gunBaseData.ballisticVelocity
                            });
                        }
                        else
                        {
                            var start = shootEventData.muzzleTran;
                            var end = shootEventData.hitPosition;

                            var shootnormal = math.normalize(end - start);

                            ecb.SetComponent<Rotation>(entityInQueryIndex, e, new Rotation
                            {
                                Value = quaternion.LookRotation(shootnormal, math.up())
                            });
                            ecb.SetComponent<Translation>(entityInQueryIndex, e, new Translation
                            {
                                Value = shootEventData.muzzleTran
                            });
                            ecb.SetComponent<PhysicsVelocity>(entityInQueryIndex, e, new PhysicsVelocity
                            {
                                //Angular= hootEventData.rotation.Value,
                                //Linear = math.forward(shootEventData.rotation.Value) * shootEventData.gunBaseData.ballisticVelocity
                                Linear = shootnormal * shootEventData.gunBaseData.ballisticVelocity
                            });
                        }
                        //获得枪口位置
                        //重新计算方向
                        //如果没命中就不重新计算
                        ecb.AddComponent(entityInQueryIndex, entity, new ShootisRenderTag());
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

            Entities
            .WithName("ShootRenderLifetimeJob")
            .ForEach((Entity ent, int entityInQueryIndex, ref ProjectileLifetime projectileLifetime, in ProjectileData projectileData) =>
            {
                if (projectileLifetime.lifetime > 0)
                {   
                    projectileLifetime.lifetime -= df;
                }
                else
                {
                    ecb.DestroyEntity(entityInQueryIndex, ent);
                    if (projectileData.ishit == false)
                        return;
                    var t=ecb.Instantiate(entityInQueryIndex, projectileData.VFXPrefab);
                    ecb.AddComponent(entityInQueryIndex, t,new RenderLifeTime { lifetime = projectileData.VFXLifeTime });
                    ecb.AddComponent(entityInQueryIndex, t, new VXFEntityTag { });

                    ecb.SetComponent<Rotation>(entityInQueryIndex, t, new Rotation
                    {
                        Value = quaternion.Euler(projectileData.hitSurfaceNormal)
                    });
                    ecb.SetComponent<Translation>(entityInQueryIndex, t, new Translation
                    {
                        Value = projectileData.hitPosition
                    });
                }
            }).ScheduleParallel();

            m_Barrier.AddJobHandleForProducer(Dependency);


            Entities
            .WithName("VFXplayJob")
            .WithoutBurst()
            .ForEach((Entity ent,in VXFEntityTag vxfEntityTag) =>
            {
                var ps = EntityManager.GetComponentObject<ParticleSystem>(ent);
                if(!ps.isPlaying)
                    ps.Play();
            }).Run();
        }
    }
    [UpdateInGroup(typeof(ClientPresentationSystemGroup))]
    //[UpdateAfter(typeof(UpdateShootRenderSystem))]
    public class CleanRenderSystem : SystemBase
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

            Entities
            .WithName("RenderLifetimeJob")
            .ForEach((Entity ent, int entityInQueryIndex,ref RenderLifeTime renderLifeTime) =>
            {
                if (renderLifeTime.lifetime > 0)
                {
                    renderLifeTime.lifetime -= df;
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