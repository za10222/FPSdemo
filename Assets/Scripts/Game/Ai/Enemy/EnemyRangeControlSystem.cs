using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Systems;
using static FPSdemo.GunManager;
using UnityEngine;
using Reese.Nav;
using Unity.Rendering;
using System.Collections.Generic;
using Unity.NetCode;

namespace FPSdemo
{
    //[DisableAutoCreation]
    [UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
    //[UpdateBefore(typeof(EnemyAnimationUpdateSystem))]
    public class EnemyRangeControlSystem : SystemBase
    {
        private BuildPhysicsWorld physicsWorldSystem;
        private Entity m_misslePrefab;
        private EntityQuery m_FPCQuery;
        EntityCommandBufferSystem barrier => World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<GunDataBufferElement>();
            physicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();
            m_FPCQuery = GetEntityQuery(typeof(GhostOwnerComponent), typeof(CharacterControllerComponentData), typeof(PhysicsCollider));
        }

        protected override void OnUpdate()
        {
            var commandBuffer = barrier.CreateCommandBuffer().AsParallelWriter();

            var PointFromEntity = GetComponentDataFromEntity<Point>();
            var time = Time.ElapsedTime;
            var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;
            var FPCs = m_FPCQuery.ToEntityArray(Unity.Collections.Allocator.TempJob);

            if (m_misslePrefab == Entity.Null)
            {
                var prefabEntity = GetSingletonEntity<GhostPrefabCollectionComponent>();
                var prefabs = EntityManager.GetBuffer<GhostPrefabBuffer>(prefabEntity);
                var foundPrefab = Entity.Null;
                for (int i = 0; i < prefabs.Length; ++i)
                {
                    if (EntityManager.HasComponent<Missile>(prefabs[i].Value))
                        foundPrefab = prefabs[i].Value;
                }
                if (foundPrefab != Entity.Null)
                    m_misslePrefab = foundPrefab;
            }

            var bufferFromEntity = GetBufferFromEntity<HealthEventBufferElement>();
            var m_misslePrefab2 = m_misslePrefab;

            var ltwFromEntity = GetComponentDataFromEntity<LocalToWorld>();

            Entities
                .WithReadOnly(ltwFromEntity)
                .WithReadOnly(collisionWorld)
                .WithReadOnly(bufferFromEntity)
                .WithReadOnly(FPCs)
                .WithReadOnly(PointFromEntity)
                .ForEach((Entity entity, int entityInQueryIndex, ref EnemyRangeInternalData enemyRangeInternalData, ref Enemy enemy, in EnemyRange enemyRange) =>
            {
   
                if (HasComponent<HealthData>(entity))
                {
                    var health = GetComponent<HealthData>(entity);
                    if (health.currentHp == 0)
                    {
                        if (HasComponent<NavWalking>(entity))
                        {
                            commandBuffer.AddComponent<NavStop>(entityInQueryIndex, entity);
                        }

                        enemy.state = Enemy.EnemyState.dieing;
                        enemy.inhit = false;
                        if (enemyRangeInternalData.dietime > 0.1)
                        {
                            if (time - enemyRangeInternalData.dietime > 2)
                            {
                                commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                                bool isfind = false;
                                Entity shooter = Entity.Null;
                                foreach (var i in FPCs)
                                {
                                    if (GetComponent<GhostOwnerComponent>(i).NetworkId == health.lasthit)
                                    {
                                        shooter = i;
                                        isfind = true;
                                        break;
                                    }
                                }
                                if (isfind)
                                {
                                    var point = PointFromEntity[shooter];
                                    point.point += 10;
                                    commandBuffer.SetComponent<Point>(entityInQueryIndex, shooter, point);
                                    Debug.Log(string.Format("加分{0},分数{1}", shooter.Index, point.point));
                                }
                            }
                        }
                        else
                        {
                            enemyRangeInternalData.dietime = time;
                        }

                        return;
                    }
                }
                //collisionWorld
                var ltw = ltwFromEntity[enemyRange.bulletspawn];

              


                //如果变色期间，无视所有被打
                if (enemy.inhit)
                {
                    if (time > enemyRangeInternalData.lasthittime + enemyRange.hitDuration)
                    {  //还原
                        //commandBuffer.SetComponent(entityInQueryIndex, enemyMelee.entitynode, new URPMaterialPropertyBaseColor
                        //{
                        //    Value = white4
                        //});
                        enemy.inhit = false;
                    }

                }
                else
                {
                    if ((time > enemyRangeInternalData.lasthittime + enemyRange.hitDuration + enemyRange.recoverDuration) &&
                    bufferFromEntity.HasComponent(entity) && bufferFromEntity[entity].Length > 0)
                    {
                        //commandBuffer.SetComponent(entityInQueryIndex, enemyMelee.entitynode, new URPMaterialPropertyBaseColor
                        //{
                        //    Value = red4
                        //});
                        enemy.inhit = true;
                        enemyRangeInternalData.lasthittime = time;
                    }
                }


                //
                if (enemy.state == Enemy.EnemyState.attack)
                {
                    //判断
                    var t = time - enemyRangeInternalData.lastattacktime;

                    if (t > 1d && t < 1.5d)
                    {
                        if(!enemyRangeInternalData.missileCreated)
                        {
                            var e = commandBuffer.Instantiate(entityInQueryIndex, m_misslePrefab2);
                            commandBuffer.SetComponent<Translation>(entityInQueryIndex, e, new Translation {Value= ltw.Position });
                            commandBuffer.SetComponent<Rotation>(entityInQueryIndex, e, new Rotation { Value = ltw.Rotation });
                            commandBuffer.SetComponent<PhysicsVelocity>(entityInQueryIndex, e, 
                                new PhysicsVelocity { Linear = math.normalize(math.forward(ltw.Rotation))*3 });
                            enemyRangeInternalData.missileCreated = true;
                        }
                      

                        //进行攻击判断 状态变为idle
                        //Debug.Log(string.Format("find player {0}", hitresult.Entity));
                        //enemy.state = Enemy.EnemyState.idle;
                        //commandBuffer.SetComponent(entityInQueryIndex, enemyMelee.entitynode, new URPMaterialPropertyBaseColor { Value = white4 });
                    }   //等待 啥都不做
                    else if (t > 1.5d)
                    {
                        enemy.state = Enemy.EnemyState.idle;
                        //状态变为idle
                        //Debug.Log(string.Format("find player {0}", hitresult.Entity));
                        //enemy.state = Enemy.EnemyState.idle;
                        //commandBuffer.SetComponent(entityInQueryIndex, enemyMelee.entitynode, new URPMaterialPropertyBaseColor { Value = white4 });
  
                    }
                }
                else
                {
                    var forward = math.normalize(math.forward(ltw.Rotation));
                    var startpos = ltw.Position;
                    var endpos = ltw.Position + forward * enemyRange.attackRange;
                    var hitresult = new ColliderCastHit();
                    var ishit = CommonUtilities.SphereCollidercast(ltwFromEntity[enemyRange.bulletspawn].Position, endpos, 0.5f, in collisionWorld, out hitresult);

                    if (ishit)
                    {
                        commandBuffer.AddComponent<NavStop>(entityInQueryIndex, entity);
                        enemy.state = Enemy.EnemyState.attack;
                        enemyRangeInternalData.lastattacktime = time;
                        enemyRangeInternalData.missileCreated = false;
                        Debug.Log(string.Format("find player {0}", hitresult.Entity));
                    }
                    else
                    {
                        //能跑吗
                        if (HasComponent<NavWalking>(entity))
                        {
                            enemy.state = Enemy.EnemyState.walk;
                        }
                        else
                        {
                            enemy.state = Enemy.EnemyState.idle;
                        }
                    }

                }

            }).Schedule();
            Dependency.Complete();
            barrier.AddJobHandleForProducer(Dependency);
          
            var df = Time.DeltaTime;
            Entities.ForEach((Entity entity, int entityInQueryIndex, ref Missile missile,in HealthData healthData) =>
            {
                missile.lifetime -= df;
                if (missile.lifetime <= 0|| healthData.currentHp<=0)
                {
                    commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                }
            }).ScheduleParallel();
            barrier.AddJobHandleForProducer(Dependency);
            FPCs.Dispose();

        }
    }
}