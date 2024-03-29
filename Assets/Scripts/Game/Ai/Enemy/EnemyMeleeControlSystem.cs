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
    public class EnemyMeleeControlSystem : SystemBase
    {
        StepPhysicsWorld stepPhysicsWorld;
        EntityCommandBufferSystem barrier => World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        private EntityQuery m_FPCQuery;
        protected override void OnCreate()
        {
            stepPhysicsWorld = World.GetExistingSystem<StepPhysicsWorld>();
            RequireSingletonForUpdate<GunDataBufferElement>();
            m_FPCQuery = GetEntityQuery(typeof(GhostOwnerComponent), typeof(CharacterControllerComponentData), typeof(PhysicsCollider));
        }

        protected override void OnUpdate()
        {
            var commandBuffer = barrier.CreateCommandBuffer().AsParallelWriter();
    
            var bufferFromEntity= GetBufferFromEntity<HealthEventBufferElement>();
            var PointFromEntity = GetComponentDataFromEntity<Point>();

            var FPCs = m_FPCQuery.ToEntityArray(Unity.Collections.Allocator.TempJob);
            var time = Time.ElapsedTime;


            Entities
                .WithReadOnly(bufferFromEntity)
                .WithReadOnly(FPCs)
                .WithReadOnly(PointFromEntity)
                .ForEach((Entity entity, int entityInQueryIndex, ref EnemyMeleeInternalData enemyMeleeInternalData, ref Enemy enemy, in EnemyMelee enemyMelee) =>
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
                     
                        enemy.state = Enemy.EnemyState.dieing;
                        enemy.inhit = false;
                        if (enemyMeleeInternalData.dietime>0.1)
                        {
                            if(time - enemyMeleeInternalData.dietime > 2)
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
                            enemyMeleeInternalData.dietime = time;
                        }

                        return;
                    }
                }

                //如果变色期间，无视所有被打
                if (enemy.inhit)
                {
                    if (time > enemyMeleeInternalData.lasthittime + enemyMelee.hitDuration)
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
                    if ((time > enemyMeleeInternalData.lasthittime + enemyMelee.hitDuration+ enemyMelee.recoverDuration)&&
                    bufferFromEntity.HasComponent(entity) && bufferFromEntity[entity].Length > 0)
                    {
                        //commandBuffer.SetComponent(entityInQueryIndex, enemyMelee.entitynode, new URPMaterialPropertyBaseColor
                        //{
                        //    Value = red4
                        //});
                        enemy.inhit = true;
                        enemyMeleeInternalData.lasthittime = time;
                    }
                }

                if (enemyMeleeInternalData.hasFind)
                {
                    //如果还在攻击过程中
                    if (enemy.state == Enemy.EnemyState.attack)
                    {
                        //判断时间是否攻击判断
                        var t = time - enemyMeleeInternalData.lastattacktime;

                        if (t > 1d && enemyMeleeInternalData.isattack ==false)
                        {
                            //进行攻击判断 状态变为idle
                            enemyMeleeInternalData.isattack = true;
                            commandBuffer.AppendToBuffer(entityInQueryIndex, enemyMeleeInternalData.hitEntity, new HealthEventBufferElement { healthChange = -10 });
                            //commandBuffer.SetComponent(entityInQueryIndex, enemyMelee.entitynode, new URPMaterialPropertyBaseColor { Value = white4 });
                        }   //等待 啥都不做

                        if (t > 3d )
                        {
                            //进行攻击判断 状态变为idle
                            enemy.state = Enemy.EnemyState.idle;
                            //commandBuffer.SetComponent(entityInQueryIndex, enemyMelee.entitynode, new URPMaterialPropertyBaseColor { Value = white4 });
                        }   //等待 啥都不做
                    }
                    else 
                    {
                        //直接加入nav stop 停下来进行攻击动作,同时设置攻击开始时间
                        commandBuffer.AddComponent<NavStop>(entityInQueryIndex, entity);
                        enemy.state = Enemy.EnemyState.attack;
                        enemyMeleeInternalData.lastattacktime = time;
                        enemyMeleeInternalData.isattack = false;

                        //commandBuffer.SetComponent(entityInQueryIndex, enemyMelee.entitynode, new URPMaterialPropertyBaseColor { Value = red4 });
                    }
                }
                else
                {
                    if (enemy.state == Enemy.EnemyState.attack)
                    {
                        //是否超时
                        var t = time - enemyMeleeInternalData.lastattacktime;
                        if (t > 3d)
                        {
                             //还原 
                            enemy.state = Enemy.EnemyState.idle;
                        }
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
                enemyMeleeInternalData.hitEntity = Entity.Null;
                enemyMeleeInternalData.hasFind = false;
            }).ScheduleParallel();
            Dependency.Complete();
            barrier.AddJobHandleForProducer(Dependency);
            FPCs.Dispose();
        }
    }
}