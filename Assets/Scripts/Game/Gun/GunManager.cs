using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Windows;

namespace FPSdemo
{
    public enum Gunstate
    {
        normal,
        shoot,
        changegun
    }
    public static class GunManager
    {

        [InternalBufferCapacity(8)]
        public struct GunDataBufferElement : IBufferElementData
        {
            public GunData gunData;
        }

  

        public struct GunData
        {
            public GunBaseData gunBaseData;
            public GunRenderData gunRenderData;
        }

        public struct GunBaseData : IComponentData
        {
            public float shootgap;
            public int gunTypeIndex;
        }

        [GhostComponent(PrefabType = GhostPrefabType.Client)]
        public struct GunRenderData : IComponentData
        {
            //model�йص����� 
            public Entity GunModelEntity;
            public Entity MuzzleEntity;
            public Entity ProjectileEntity;
        }

        //��ɫӵ�е��������� ��ǰ�洢�������� ������������ ������ǿ
        public struct PlayerGunData : IComponentData
        {
            [GhostField]
            public int gunTypeIndex;

            [GhostField]
            public float changeGunGap;

            [GhostField]
            public Gunstate gunstate;
        }

        //��ǹ֧״̬�йص�����
        [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
        public struct PlayerGunInternalData : IComponentData
        {
            [GhostField]
            public bool changeGun;
            [GhostField]
            public bool shoot;
            [GhostField]
            public float lastChangeDeltaTime;
            [GhostField]
            public float lastShootDeltaTime;

            public bool hasinput;
        }

        public struct ShootEventData : IComponentData
        {
            //�ĸ�ǹ֧ʵ�������
            [GhostField]
            public GunBaseData gunBaseData;

            [GhostField]
            public int owner;

            [GhostField]
            public Translation translation;

   
            [GhostField]
            public Rotation rotation;
        }

        //������ͻ���ʵ��������û�г�ʼ�� 
        [GhostComponent(PrefabType = GhostPrefabType.Client)]
        public struct ShootRenderData : IComponentData
        {
            public bool isRender;
            //�����洢���ж���Ч�Ƿ�����
            public Entity ProjectilePrefab;
            public float ProjectilePrefabLifetime;
        }

        //[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
        //[UpdateInGroup(typeof(GhostPredictionSystemGroup))]
        //public class test : SystemBase
        //{
        //    protected override void OnCreate()
        //    {

        //        var m_GunManagerEntity = EntityManager.CreateEntity(typeof(GunDataBufferElement));
        //        var m_gunPrefabs = Resources.LoadAll("Prefabs/GunData/");
        //        Debug.Log("��ȡ��������" + m_gunPrefabs.Length);
        //        for (int i = 0; i < m_gunPrefabs.Length; i++)
        //        {
        //            var gunPrefab = m_gunPrefabs[i];
        //            var m_gunDataEntity = PrefabAssetManager.GetOrCreateEntityPrefab(World, (GameObject)gunPrefab);
        //            DynamicBuffer<GunDataBufferElement> dynamicBuffer
        //           = EntityManager.GetBuffer<GunDataBufferElement>(m_GunManagerEntity);
        //            GunData gunData = default;
        //            gunData.gunBaseData = GetComponent<GunBaseData>(m_gunDataEntity);
        //            gunData.gunRenderData = GetComponent<GunRenderData>(m_gunDataEntity);

        //            dynamicBuffer.Add(new GunDataBufferElement
        //            {
        //                gunData = gunData
        //            });
        //        }
        //    }
        //    protected override void OnUpdate()
        //    {
        //    }
        //}



        //[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
        [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
        [UpdateAfter(typeof(PlayGunUserInputUpdateSystem))]
        public class HandlePlayerGunSystem : SystemBase
        {
            protected override void OnCreate()
            {
                m_Barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();


                m_GunManagerEntity = EntityManager.CreateEntity(typeof(GunDataBufferElement));
                var m_gunPrefabs = Resources.LoadAll("Prefabs/GunData/");
                Debug.Log("��ȡ��������" + m_gunPrefabs.Length);
                for (int i = 0; i < m_gunPrefabs.Length; i++)
                {
                    var gunPrefab = m_gunPrefabs[i];
                    var m_gunDataEntity = PrefabAssetManager.GetOrCreateEntityPrefab(World, (GameObject)gunPrefab);
                    DynamicBuffer<GunDataBufferElement> dynamicBuffer
                   = EntityManager.GetBuffer<GunDataBufferElement>(m_GunManagerEntity);
                    GunData gunData = default;
                    gunData.gunBaseData = GetComponent<GunBaseData>(m_gunDataEntity);
                    gunData.gunRenderData = GetComponent<GunRenderData>(m_gunDataEntity);

                    dynamicBuffer.Add(new GunDataBufferElement
                    {
                        gunData = gunData
                    });
                }


                RequireSingletonForUpdate<GunDataBufferElement>();
            }

            protected override void OnUpdate()
            {
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

                var df = Time.DeltaTime;


                DynamicBuffer<GunDataBufferElement> dynamicBuffer
                 = EntityManager.GetBuffer<GunDataBufferElement>(m_GunManagerEntity);


                var m_ShootEventPrefab2 = m_ShootEventPrefab;
                var commandBuffer = m_Barrier.CreateCommandBuffer().AsParallelWriter();

                //�鿴�Ƿ������� ���˾Ͱ�prefab��GunBaseData���ƹ�ȥ
                Entities
               .WithName("SwitchPlayerGunJobClientJob")
               .WithReadOnly(dynamicBuffer)
               .ForEach((Entity ent,int nativeThreadIndex, ref PlayerGunData playerGunData, ref PlayerGunInternalData playerGunInternalData, ref GunBaseData gunBase,in LocalToParent ltp,in Parent pa) =>
               {
                   if (playerGunInternalData.hasinput == false)
                       return;


                   playerGunInternalData.lastChangeDeltaTime += df;
                   playerGunInternalData.lastShootDeltaTime += df;

                   switch (playerGunData.gunstate)
                   {
                       case Gunstate.normal:
                           if ((playerGunInternalData.changeGun && playerGunInternalData.lastChangeDeltaTime > playerGunData.changeGunGap) || playerGunInternalData.lastChangeDeltaTime < -1)
                           {
                               playerGunData.gunstate = Gunstate.changegun;
                               playerGunInternalData.lastChangeDeltaTime = 0;
                               playerGunData.gunTypeIndex = (playerGunData.gunTypeIndex + 1) % dynamicBuffer.Length;
                               gunBase = dynamicBuffer[playerGunData.gunTypeIndex].gunData.gunBaseData;
                               if (HasComponent<GunRenderData>(ent))
                               {
                                   SetComponent<GunRenderData>(ent, dynamicBuffer[playerGunData.gunTypeIndex].gunData.gunRenderData);
                               }

                               break;
                           }

                           if ((playerGunInternalData.lastChangeDeltaTime > 0.5) && playerGunInternalData.shoot && playerGunInternalData.lastShootDeltaTime > gunBase.shootgap)
                           {
                               playerGunInternalData.lastShootDeltaTime = 0;
                               //���ǹ֧����¼�
                               if (m_ShootEventPrefab2 != Entity.Null)
                               {

                                   var e = commandBuffer.Instantiate(nativeThreadIndex, m_ShootEventPrefab2);
                                   
                                    var tran = GetComponent<Translation>(pa.Value);
                                    var rotation = GetComponent<Rotation>(pa.Value);
    
                                    var parent_localtoworld = new RigidTransform(rotation.Value, tran.Value);
      

                                   var ltw2 = math.mul(parent_localtoworld, new RigidTransform(ltp.Value));
                           
                                   commandBuffer.SetComponent(nativeThreadIndex, e,
                                     new ShootEventData { gunBaseData = gunBase, owner = GetComponent<GhostOwnerComponent>(pa.Value).NetworkId,
                                         translation=new Translation { Value=ltw2.pos}
                                     ,
                                         rotation=new Rotation { Value=ltw2.rot} });
                                    
                                   commandBuffer.SetComponent(nativeThreadIndex, e,
                                     new GhostOwnerComponent { NetworkId =  GetComponent<GhostOwnerComponent>(pa.Value).NetworkId });
                                   
                               }
                               playerGunData.gunstate = Gunstate.shoot;
                           }
                           break;

                       case Gunstate.changegun:
                           if (playerGunInternalData.lastChangeDeltaTime > playerGunData.changeGunGap)
                           {
                               playerGunData.gunstate = Gunstate.normal;
                           }

                           break;
                       case Gunstate.shoot:
                           if (playerGunInternalData.lastShootDeltaTime > gunBase.shootgap)
                           {
                               playerGunData.gunstate = Gunstate.normal;
                           }
                           break;
                   }


               }).Schedule();
                m_Barrier.AddJobHandleForProducer(Dependency);
            }


            public Entity m_GunManagerEntity;
            private Entity m_ShootEventPrefab;
            private BeginSimulationEntityCommandBufferSystem m_Barrier;
        }
    }
}