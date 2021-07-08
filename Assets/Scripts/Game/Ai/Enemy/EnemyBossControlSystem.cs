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
    //[UpdateBefore(typeof(EnemyAnimationUpdateSystem))]
    [UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
    public class EnemyBossControlSystem : SystemBase
    {
        private BuildPhysicsWorld physicsWorldSystem;
        EntityCommandBufferSystem barrier => World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        private Entity m_misslePrefab;

        protected override void OnCreate()
        {
            physicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();
            RequireSingletonForUpdate<GunDataBufferElement>();
        }

        protected override void OnUpdate()
        {
            var commandBuffer = barrier.CreateCommandBuffer().AsParallelWriter();
          
            var bufferFromEntity= GetBufferFromEntity<HealthEventBufferElement>();


            var time = Time.ElapsedTime;


            var ltwFromEntity = GetComponentDataFromEntity<LocalToWorld>();

            var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;

            if (m_misslePrefab == Entity.Null)
            {
                var prefabEntity = GetSingletonEntity<GhostPrefabCollectionComponent>();
                var prefabs = EntityManager.GetBuffer<GhostPrefabBuffer>(prefabEntity);
                var foundPrefab = Entity.Null;
                for (int i = 0; i < prefabs.Length; ++i)
                {
                    if (EntityManager.HasComponent<MissleBoss>(prefabs[i].Value))
                        foundPrefab = prefabs[i].Value;
                }
                if (foundPrefab != Entity.Null)
                    m_misslePrefab = foundPrefab;
            }
            var m_misslePrefab2 = m_misslePrefab;

            Entities
                .WithReadOnly(bufferFromEntity)
                .WithReadOnly(ltwFromEntity)
                .WithReadOnly(collisionWorld)
                .ForEach((Entity entity, int entityInQueryIndex, ref EnemyBossInternalData enemyBossInternalData, ref EnemyBoss enemyBoss) =>
            {
                if(HasComponent<HealthData>(entity))
                {
                    var health = GetComponent<HealthData>(entity);
                    if(health.currentHp==0)
                    {
                        if(HasComponent<NavWalking>(entity))
                        {
                            commandBuffer.AddComponent<NavStop>(entityInQueryIndex, entity);
                        }

                        enemyBoss.state = EnemyBoss.EnemyBossState.dieing;
                        enemyBoss.inhit = false;
                        if (enemyBossInternalData.dietime>0.1)
                        {
                            if(time - enemyBossInternalData.dietime > 2.5)
                            {
                                commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                            }
                        }
                        else
                        {
                            enemyBossInternalData.dietime = time;
                        }

                        return;
                    }
                }

                //如果变色期间，无视所有被打
                if (enemyBoss.inhit)
                {
                    if (time > enemyBossInternalData.lasthittime + enemyBoss.hitDuration)
                    {  //还原
                        //commandBuffer.SetComponent(entityInQueryIndex, enemyMelee.entitynode, new URPMaterialPropertyBaseColor
                        //{
                        //    Value = white4
                        //});
                        enemyBoss.inhit = false;
                    }
                 
                }
                else
                {
                    if ((time > enemyBossInternalData.lasthittime + enemyBoss.hitDuration+ enemyBoss.recoverDuration)&&
                    bufferFromEntity.HasComponent(entity) && bufferFromEntity[entity].Length > 0)
                    {
                        enemyBoss.inhit = true;
                        enemyBossInternalData.lasthittime = time;
                    }
                }
                var ltw_left = ltwFromEntity[enemyBoss.LeftRock];
                var ltw_right = ltwFromEntity[enemyBoss.RightRock];
                var ltw = ltwFromEntity[entity];

                var ltw_left_temp =new RigidTransform(ltw_left.Value);
                ltw_left_temp.pos.y = ltw.Position.y;
                var ltw_right_temp = new RigidTransform(ltw_right.Value);
                ltw_right_temp.pos.y = ltw.Position.y;
                if (enemyBoss.state == EnemyBoss.EnemyBossState.shoot)
                {
                    //判断
                    var t = time - enemyBossInternalData.lastattacktime;

                    if (t > 1d && t < 1.5d)
                    {
                     
                        if (!enemyBossInternalData.shootcreated)
                        {
                            var e_left = commandBuffer.Instantiate(entityInQueryIndex, m_misslePrefab2);

                            var missBossData = GetComponent<MissleBoss>(m_misslePrefab2);
                                missBossData.lifetime = 10;
                                missBossData.find = enemyBossInternalData.find;
                            commandBuffer.SetComponent<Translation>(entityInQueryIndex, e_left, new Translation { Value = ltw_left_temp.pos });
                            commandBuffer.SetComponent<Rotation>(entityInQueryIndex, e_left, new Rotation { Value = ltw.Rotation });
 
                            commandBuffer.SetComponent<MissleBoss>(entityInQueryIndex, e_left, missBossData);

                            var e_right = commandBuffer.Instantiate(entityInQueryIndex, m_misslePrefab2);
                            commandBuffer.SetComponent<Translation>(entityInQueryIndex, e_right, new Translation { Value = ltw_right_temp.pos });
                            commandBuffer.SetComponent<Rotation>(entityInQueryIndex, e_right, new Rotation { Value = ltw.Rotation });
                            commandBuffer.SetComponent<MissleBoss>(entityInQueryIndex, e_right, missBossData);

                            enemyBossInternalData.shootcreated = true;
                        }


                        //进行攻击判断 状态变为idle
                        //Debug.Log(string.Format("find player {0}", hitresult.Entity));
                        //enemy.state = Enemy.EnemyState.idle;
                        //commandBuffer.SetComponent(entityInQueryIndex, enemyMelee.entitynode, new URPMaterialPropertyBaseColor { Value = white4 });
                    }   //等待 啥都不做
                    else if (t > 1.5d)
                    {
                        enemyBoss.state = EnemyBoss.EnemyBossState.idle;
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
                    var endpos = ltw.Position + forward * enemyBoss.findDistance;
                    var hitresult = new ColliderCastHit();
                    var ishit = CommonUtilities.SphereCollidercast(ltwFromEntity[entity].Position, endpos, 0.5f, in collisionWorld, out hitresult);

                    if (ishit)
                    {
                        commandBuffer.AddComponent<NavStop>(entityInQueryIndex, entity);
                        enemyBoss.state = EnemyBoss.EnemyBossState.shoot;
                        enemyBossInternalData.lastattacktime = time;
                        enemyBossInternalData.shootcreated = false;
                        enemyBossInternalData.find =hitresult.Entity;
                    }
                    else
                    {
                        //能跑吗

                        enemyBoss.state = EnemyBoss.EnemyBossState.idle;
                    }

                }



            }).ScheduleParallel();
            barrier.AddJobHandleForProducer(Dependency);
            var df = Time.DeltaTime;
            Entities.ForEach((Entity entity, int entityInQueryIndex, ref MissleBoss missile) =>
            {
                missile.lifetime -= df;
                if (missile.lifetime <= 0)
                {
                    commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                }
            }).ScheduleParallel();
            barrier.AddJobHandleForProducer(Dependency);
        }
    }
}