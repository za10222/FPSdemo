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

namespace FPSdemo
{
     
    [UpdateBefore(typeof(EnemyAnimationUpdateSystem))]
    public class EnemyMeleeControlSystem : SystemBase
    {
        StepPhysicsWorld stepPhysicsWorld;
        EntityCommandBufferSystem barrier => World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        protected override void OnCreate()
        {
            stepPhysicsWorld = World.GetExistingSystem<StepPhysicsWorld>();
            RequireSingletonForUpdate<GunDataBufferElement>();
        }

        protected override void OnUpdate()
        {
            var events = ((Simulation)stepPhysicsWorld.Simulation).TriggerEvents;
            var commandBuffer = barrier.CreateCommandBuffer().AsParallelWriter();
            foreach (var i in events)
            {
                Entity enemy = Entity.Null;
                Entity play = Entity.Null;
                if ((HasComponent<Find>(i.EntityA) && HasComponent<Enemy>(i.EntityB)))
                {
                    enemy = i.EntityB;
                    play = i.EntityA;
                    var enemyMeleeInternalData = GetComponent<EnemyMeleeInternalData>(enemy);
                    enemyMeleeInternalData.hasFind = true;
                    enemyMeleeInternalData.hitEntity = play;
                    SetComponent(enemy, enemyMeleeInternalData );
                    continue;
                }
                if ((HasComponent<Find>(i.EntityB) && HasComponent<Enemy>(i.EntityA)))
                {
                    enemy = i.EntityA;
                    play = i.EntityB;
                    var enemyMeleeInternalData = GetComponent<EnemyMeleeInternalData>(enemy);
                    enemyMeleeInternalData.hasFind = true;
                    enemyMeleeInternalData.hitEntity = play;
                    SetComponent(enemy, enemyMeleeInternalData);
                    continue;
                }
            }
            var bufferFromEntity= GetBufferFromEntity<HealthEventBufferElement>();


            var time = Time.ElapsedTime;


            Entities
                .WithReadOnly(bufferFromEntity)
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
                                Debug.Log("删除");
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

                        if (t > 5d)
                        {
                            //进行攻击判断 状态变为idle
                            Debug.Log(string.Format("hit player {0}", enemyMeleeInternalData.hitEntity));
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
                        //commandBuffer.SetComponent(entityInQueryIndex, enemyMelee.entitynode, new URPMaterialPropertyBaseColor { Value = red4 });
                    }
                }
                else
                {
                    if (enemy.state == Enemy.EnemyState.attack)
                    {
                        //是否超时
                        var t = time - enemyMeleeInternalData.lastattacktime;
                        if (t > 5d)
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
            barrier.AddJobHandleForProducer(Dependency);
        }
    }
}