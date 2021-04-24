using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Windows;

namespace FPSdemo
{

    public static class GunManager
    {
        [InternalBufferCapacity(8)]
        public struct GunPrefabEntityBufferElement : IBufferElementData
        {
            public GunPrefabData Value;

            public static implicit operator GunPrefabData(GunPrefabEntityBufferElement e)
            {
                return e.Value;
            }

            public static implicit operator GunPrefabEntityBufferElement(GunPrefabData e)
            {
                return new GunPrefabEntityBufferElement { Value = e };
            }
        }
        public struct GunPrefabData
        {
            public Entity GunPrefabEntity;
            public int gunTypeIndex;
        }

        public struct GunBaseData : IComponentData
        {
            public float shootgap;
        }

        //角色拥有的武器数据 当前存储武器类型 基础武器参数 参数增强
        public struct PlayerGunData : IComponentData
        {
            public GunBaseData gunBaseData;
            public int gunTypeIndex;
            public bool changeGun;

            public double changeGunGap;
        }

        public struct PlayerGunInternalData : IComponentData
        {
            public double lastChangeDeltaTime;
            public double lastShootDeltaTime;
        }
        public struct PlayerGunSpawn : IComponentData
        {
        }
        public struct GunEntity : ISystemStateComponentData
        {
            public Entity Value;
        }

        //public class HandlePlayerGunSpawnSystem : SystemBase
        //{ 

        //}


        public class GunManagerSystem : SystemBase
        {

            protected override void OnCreate()
            {
                m_GunManagerEntity = EntityManager.CreateEntity(typeof(GunPrefabEntityBufferElement));
                var m_gunPrefabs = Resources.LoadAll("Prefabs/Gun/");
                Debug.Log("读取武器预制体" + m_gunPrefabs.Length);
                for (int i = 0; i < m_gunPrefabs.Length; i++)
                {
                    var gunPrefab = m_gunPrefabs[i];
                    var m_gunPrefabEntity = PrefabAssetManager.GetOrCreateEntityPrefab(World, (GameObject)gunPrefab);
                    DynamicBuffer<GunPrefabEntityBufferElement> dynamicBuffer
                 = EntityManager.GetBuffer<GunPrefabEntityBufferElement>(m_GunManagerEntity);
                    dynamicBuffer.Add(new GunPrefabData { GunPrefabEntity = m_gunPrefabEntity, gunTypeIndex = i });
                }
                RequireSingletonForUpdate<GunPrefabEntityBufferElement>();

            }

            protected override void OnUpdate()
            {
                //查看是否换了武器 换了就把prefan的GunBaseData复制过去
                Entities
                .WithName("SwitchPlayerGunJob")
                .WithStructuralChanges()
                .WithoutBurst()
                .ForEach((Entity ent, ref PlayerGunData playgundata) =>
                {
                    if (playgundata.changeGun)
                    {
                        if(HasComponent<GunEntity>(ent))
                        {
                            var gun=GetComponent<GunEntity>(ent);
                            EntityManager.DestroyEntity(gun.Value);
                            EntityManager.RemoveComponent<GunEntity>(ent);
                        }
                        var temp = playgundata.gunTypeIndex;
                        DynamicBuffer<GunPrefabEntityBufferElement> dynamicBuffer
                        = EntityManager.GetBuffer<GunPrefabEntityBufferElement>(m_GunManagerEntity);
                        var e = dynamicBuffer[playgundata.gunTypeIndex];
                        var gunbasedate = GetComponent<GunBaseData>(e.Value.GunPrefabEntity);
                        playgundata.gunBaseData = gunbasedate;
                        playgundata.changeGun = false;
                        playgundata.gunTypeIndex = temp;

                    }
                }).Run();
            }
            //protected override void OnUpdate()
            //{
            //    var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            //    //查看是否换了武器 换了就把prefan的GunBaseData复制过去
            //    Entities
            //    .WithName("UpdatePlayerGunJob")
            //    .WithoutBurst()
            //    .ForEach((Entity ent,in LocalToWorld ltw, in PlayerGunData playgundata) =>
            //    {
            //        //如果不存在，就实例化gun
            //        if (!EntityManager.Exists(playgundata.currentGun.GunEntity)
            //        || playgundata.currentGun.GunTypeIndex!= playgundata.GunTypeIndex)
            //        {
            //            DynamicBuffer<GunPrefabEntityBufferElement> dynamicBuffer
            //            = EntityManager.GetBuffer<GunPrefabEntityBufferElement>(m_GunManagerEntity);
            //            foreach (var i in dynamicBuffer)
            //            {
            //                if(i.Value.GunTypeIndex == playgundata.GunTypeIndex)
            //                {
            //                    var e= commandBuffer.Instantiate( i.Value.GunPrefab);
            //                    GunData gundata= default;
            //                    gundata.GunEntity = e;
            //                    gundata.GunTypeIndex = playgundata.GunTypeIndex;
            //                    commandBuffer.SetComponent<PlayerGunData>( ent,new PlayerGunData { currentGun = gundata, GunTypeIndex= playgundata.GunTypeIndex });
            //                    commandBuffer.SetComponent<Translation>(e, new Translation { Value = ltw.Position });
            //                    commandBuffer.SetComponent<Rotation>(e, new Rotation { Value = ltw.Rotation });

            //                    break;
            //                }
            //            }
            //        }
            //        else
            //        {
            //            EntityManager.SetComponentData<Translation>(playgundata.currentGun.GunEntity,new Translation { Value= ltw.Position });
            //            EntityManager.SetComponentData<Rotation>(playgundata.currentGun.GunEntity, new Rotation { Value = ltw.Rotation });
            //        }
            //    }).Run();
            //    commandBuffer.Playback(EntityManager); 
            //}
            Entity m_GunManagerEntity;
        }
        public class HandlePlayerGunSpawn : SystemBase
        {

            EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;


            protected override void OnCreate()
            {


                RequireSingletonForUpdate<GunPrefabEntityBufferElement>();
                m_EndSimulationEcbSystem = World
                 .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            }

            protected override void OnUpdate()
            {
                var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter();
                var m_GunManagerEntity=GetSingletonEntity<GunPrefabEntityBufferElement>();

                DynamicBuffer<GunPrefabEntityBufferElement> dynamicBuffer
                    = EntityManager.GetBuffer<GunPrefabEntityBufferElement>(m_GunManagerEntity);
                //如果需要生成gunentity 但没有 就生成，不一样也生成
                Entities
                .WithName("HandlePlayerGunSpawnJob")
                .WithReadOnly(dynamicBuffer)
                .WithAll<PlayerGunSpawn, PlayerGunData>()
                .WithNone<GunEntity, Prefab>()
                .ForEach((Entity ent, int entityInQueryIndex, in PlayerGunData playgundata) =>
                {
                    var e= ecb.Instantiate(entityInQueryIndex, dynamicBuffer[playgundata.gunTypeIndex].Value.GunPrefabEntity);
                    ecb.AddComponent(entityInQueryIndex, ent, new GunEntity
                    {
                        Value = e,
                    });
                }).ScheduleParallel();
                m_EndSimulationEcbSystem.AddJobHandleForProducer(this.Dependency);
            }
        }
        public class UpdatePlayerGun : SystemBase
        {
            protected override void OnCreate()
            {

                RequireSingletonForUpdate<GunPrefabEntityBufferElement>();

            }
            protected override void OnUpdate()
            {

                var m_GunManagerEntity = GetSingletonEntity<GunPrefabEntityBufferElement>();
                //更新位置
                Entities
                .WithName("UpdatePlayerGunJob")
                .WithAll<PlayerGunSpawn, PlayerGunData, GunEntity>()
                .ForEach((Entity ent,in GunEntity gunEntity,in LocalToWorld ltw) =>
                {
                    SetComponent<Translation>(gunEntity.Value, new Translation { Value = ltw.Position });
                    SetComponent<Rotation>(gunEntity.Value, new Rotation { Value = ltw.Rotation });
                }).Schedule();
            }
        }

    }
}