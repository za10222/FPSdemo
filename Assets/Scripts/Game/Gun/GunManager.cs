using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport.Utilities;
using Unity.Transforms;
using UnityEngine;

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
            public float ballisticVelocity;
        }

        [GhostComponent(PrefabType = GhostPrefabType.Client)]
        public struct GunRenderData : IComponentData
        {
            //model有关的数据 
            public Entity GunModelEntity;
            public Entity MuzzleEntity;
            public Entity ProjectileEntity;
        }

        //角色拥有的武器数据 当前存储武器类型 基础武器参数 参数增强
        public struct PlayerGunData : IComponentData
        {
            [GhostField]
            public int gunTypeIndex;

            [GhostField]
            public float changeGunGap;

            [GhostField]
            public Gunstate gunstate;

            [GhostField]
            public Rotation rotation;
        }

        //和枪支状态有关的数据
        [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
        public struct PlayerGunInternalData : IComponentData
        {
            [GhostField]
            public bool changeGun;
            [GhostField]
            public bool shoot;

            [GhostField]
            public uint lastChangeTick;
            //[GhostField]
            public uint lastShootTick;

            public quaternion rotation;
            //public bool
            public bool hasinput;
        }

        public struct ShootEventData : IComponentData
        {
            //哪个枪支实体射击的
            [GhostField]
            public GunBaseData gunBaseData;

            [GhostField]
            public int owner;

            [GhostField]
            public Translation translation;

            [GhostField]
            public Rotation rotation;

            [GhostField]
            public bool ishandle;

            [GhostField]
            public float lifetime;
        }

        //这个被客户端实例化，但没有初始化 
        [GhostComponent(PrefabType = GhostPrefabType.Client)]
        public struct ShootRenderData : IComponentData
        {
            public bool isRender;
            //用来存储和判断特效是否生成
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
        //        Debug.Log("读取武器数据" + m_gunPrefabs.Length);
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


        [UpdateInGroup(typeof(GhostSimulationSystemGroup))]
        [UpdateAfter(typeof(GhostPredictionSystemGroup))]
        public class UpdatePlayerGunSystem : SystemBase
        {
            protected override void OnUpdate()
            {
                Entities
            .WithName("UpdatePlayerGunSystemJob")
            .ForEach((Entity ent, int nativeThreadIndex, ref PlayerGunData playerGunData, ref Rotation rotation) =>
            {
                rotation = playerGunData.rotation;
            }).Schedule();
            }
        }

        //[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
        [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
        [UpdateAfter(typeof(PlayGunUserInputUpdateSystem))]
        public class HandlePlayerGunSystem : SystemBase
        {

            private GhostPredictionSystemGroup m_PredictionGroup;

            protected override void OnCreate()
            {
                m_Barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
                m_PredictionGroup = World.GetOrCreateSystem<GhostPredictionSystemGroup>();

                m_GunManagerEntity = EntityManager.CreateEntity(typeof(GunDataBufferElement));
                var m_gunPrefabs = Resources.LoadAll("Prefabs/GunData/");
                Debug.Log("读取武器数据" + m_gunPrefabs.Length);
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
                RequireSingletonForUpdate<GhostPrefabCollectionComponent>();

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

                var currentTick = m_PredictionGroup.PredictingTick;

                var m_ShootEventPrefab2 = m_ShootEventPrefab;
                var commandBuffer = m_Barrier.CreateCommandBuffer().AsParallelWriter();

                //string server = World.Name;
                //Debug.Log(server);
                //查看是否换了武器 换了就把prefab的GunBaseData复制过去
                Entities
               .WithName("SwitchPlayerGunJobClientJob")
               .WithReadOnly(dynamicBuffer)
               .ForEach((Entity ent, int nativeThreadIndex, ref PlayerGunData playerGunData, ref PlayerGunInternalData playerGunInternalData, ref GunBaseData gunBase, in LocalToParent ltp, in Parent pa) =>
               {
                   if (playerGunInternalData.hasinput == false)
                       return;


                   //调整枪支y轴方向
                   Quaternion qa = playerGunInternalData.rotation;
                   //Debug.Log(qa.eulerAngles.x);
                   quaternion q = quaternion.Euler(math.radians(qa.eulerAngles.x), 0, 0);
                   //Debug.Log(string.Format("{0}", v));
                   //SetComponent<Rotation>(ent, new Rotation { Value = q });
                   playerGunData.rotation.Value = q;

                   switch (playerGunData.gunstate)
                   {
                       case Gunstate.normal:
                           if (playerGunInternalData.lastChangeTick==0
                           ||(playerGunInternalData.changeGun && (SequenceHelpers.IsNewer(currentTick, playerGunInternalData.lastChangeTick + (uint)(playerGunData.changeGunGap * 60)))
                           ))
                           {
                               playerGunData.gunstate = Gunstate.changegun;
                               playerGunData.gunTypeIndex = (playerGunData.gunTypeIndex + 1) % dynamicBuffer.Length;
                               gunBase = dynamicBuffer[playerGunData.gunTypeIndex].gunData.gunBaseData;
                               //if (HasComponent<GunRenderData>(ent))
                               //{
                               //    SetComponent<GunRenderData>(ent, dynamicBuffer[playerGunData.gunTypeIndex].gunData.gunRenderData);
                               //}
                               playerGunInternalData.lastChangeTick = currentTick;
                               break;
                           }
                          

                           if (playerGunInternalData.shoot && 
                           (SequenceHelpers.IsNewer(currentTick, playerGunInternalData.lastShootTick + (uint)(gunBase.shootgap * 60)))
                           )
                           {
                               //Debug.Log(string.Format("ct={0},lt={1}", currentTick, playerGunInternalData.lastShootTick));
                               //添加枪支射击事件
                               if (m_ShootEventPrefab2 != Entity.Null)
                               {
                                   var e = commandBuffer.Instantiate(nativeThreadIndex, m_ShootEventPrefab2);

                                   var tran = GetComponent<Translation>(pa.Value);
                                   var rotation2 = GetComponent<Rotation>(pa.Value);

                                   var parent_localtoworld = new RigidTransform(rotation2.Value, tran.Value);


                                   var ltw2 = math.mul(parent_localtoworld, new RigidTransform(ltp.Value));

                                   commandBuffer.SetComponent(nativeThreadIndex, e,
                                     new ShootEventData
                                     {
                                         gunBaseData = gunBase,
                                         owner = GetComponent<GhostOwnerComponent>(pa.Value).NetworkId,
                                         translation = new Translation { Value = ltw2.pos }
                                     ,
                                         rotation =new Rotation { Value = playerGunInternalData.rotation }
                                     });

                                   commandBuffer.SetComponent(nativeThreadIndex, e,
                                     new GhostOwnerComponent { NetworkId = GetComponent<GhostOwnerComponent>(pa.Value).NetworkId });

                               }
                               playerGunData.gunstate = Gunstate.shoot;
                               playerGunInternalData.lastShootTick = currentTick;
                           }
                           break;

                       case Gunstate.changegun:
                           if (SequenceHelpers.IsNewer(currentTick, playerGunInternalData.lastChangeTick + (uint)(playerGunData.changeGunGap * 60)))
                           {
                               playerGunData.gunstate = Gunstate.normal;
                           }

                           break;
                       case Gunstate.shoot:
                           if (SequenceHelpers.IsNewer(currentTick, playerGunInternalData.lastShootTick + (uint)(gunBase.shootgap * 60)))
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