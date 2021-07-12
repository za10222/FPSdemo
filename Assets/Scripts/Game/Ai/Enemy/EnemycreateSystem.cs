using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using static FPSdemo.GunManager;
using Unity.Physics.Systems;

namespace FPSdemo
{
    [UpdateInGroup(typeof(ServerSimulationSystemGroup))]
    public class EnemycreateSystem : SystemBase
    {
        private float bosstime;
        private float ememytime;
        private EntityQuery bossquery;
        private EntityQuery meleequery; 
        private EntityQuery rangequery;
        private Entity bossprefab;
        private Entity meleeprefab;
        private Entity rangeprefab;
         
        EntityCommandBufferSystem barrier => World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        protected override void OnCreate()
        {
            bosstime =0;
            ememytime = 0;
            RequireSingletonForUpdate<GunDataBufferElement>();
            RequireSingletonForUpdate<NetworkStreamInGame>();
            bossquery = GetEntityQuery(typeof(EnemyBoss));
            meleequery = GetEntityQuery(typeof(EnemyMelee));
            rangequery = GetEntityQuery(typeof(EnemyRange ));
        }
        protected override void OnUpdate()
        {
            if (bossprefab == Entity.Null)
            {
                var prefabEntity = GetSingletonEntity<GhostPrefabCollectionComponent>();
                var prefabs = EntityManager.GetBuffer<GhostPrefabBuffer>(prefabEntity);
                var foundPrefab = Entity.Null;
                for (int i = 0; i < prefabs.Length; ++i)
                {
                    if (EntityManager.HasComponent<EnemyBoss>(prefabs[i].Value))
                        foundPrefab = prefabs[i].Value;
                }
                if (foundPrefab != Entity.Null)
                    bossprefab = foundPrefab;
            }
            if (meleeprefab == Entity.Null)
            {
                var prefabEntity = GetSingletonEntity<GhostPrefabCollectionComponent>();
                var prefabs = EntityManager.GetBuffer<GhostPrefabBuffer>(prefabEntity);
                var foundPrefab = Entity.Null;
                for (int i = 0; i < prefabs.Length; ++i)
                {
                    if (EntityManager.HasComponent<EnemyMelee>(prefabs[i].Value))
                        foundPrefab = prefabs[i].Value;
                }
                if (foundPrefab != Entity.Null)
                    meleeprefab = foundPrefab;
            }
            if (rangeprefab == Entity.Null)
            {
                var prefabEntity = GetSingletonEntity<GhostPrefabCollectionComponent>();
                var prefabs = EntityManager.GetBuffer<GhostPrefabBuffer>(prefabEntity);
                var foundPrefab = Entity.Null;
                for (int i = 0; i < prefabs.Length; ++i)
                {
                    if (EntityManager.HasComponent<EnemyRange>(prefabs[i].Value))
                        foundPrefab = prefabs[i].Value;
                }
                if (foundPrefab != Entity.Null)
                    rangeprefab = foundPrefab;
            }

            var df = Time.DeltaTime;
            var commandBuffer = barrier.CreateCommandBuffer();
            if (bossquery.CalculateEntityCount()==0)
            {
                bosstime +=df;
           
            }
            ememytime += df;
            if (bosstime > 5f)
            {
                bosstime = 0;
                var e = commandBuffer.Instantiate(bossprefab);
                commandBuffer.SetComponent(e, new Translation { Value = new float3(47, 0, 45) });
            }

            if (ememytime > 5f)
            {
                var random = new Unity.Mathematics.Random((uint)Time.ElapsedTime * 100 + 1);
                var t1= random.NextUInt()%10-5;
                var t2 = random.NextUInt() %10-5;
                ememytime = 0;
                if(meleequery.CalculateEntityCount()<=10)
                {
                    var e = commandBuffer.Instantiate(meleeprefab);
                    commandBuffer.SetComponent(e, new Translation { Value = new float3(37+t1, 0, 45+t2) });
                }
                if (rangequery.CalculateEntityCount() <= 10)
                {
                    var e = commandBuffer.Instantiate(rangeprefab);
                    commandBuffer.SetComponent(e, new Translation { Value = new float3(54+t2, 13, 30+t1) });
                }
            }
        }
    }
}

