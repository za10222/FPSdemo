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
            var time = Time.ElapsedTime;
            Entities.ForEach((Entity entity, int entityInQueryIndex, ref EnemyMeleeInternalData enemyMeleeInternalData, ref Enemy enemy, in EnemyMelee enemyMelee) =>
            {
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

                        }   //等待 啥都不做
                    }
                    else
                    {
                        //直接加入nav stop 停下来进行攻击动作,同时设置攻击开始时间
                        commandBuffer.AddComponent<NavStop>(entityInQueryIndex, entity);
                        enemy.state = Enemy.EnemyState.attack;
                        enemyMeleeInternalData.lastattacktime = time;
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
                             //还原 大概率还是攻击状态
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