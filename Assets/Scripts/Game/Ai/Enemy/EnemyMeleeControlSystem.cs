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
                                Debug.Log("ɾ��");
                            }
                        }
                        else
                        {
                            enemyMeleeInternalData.dietime = time;
                        }

                        return;
                    }
                }

                //�����ɫ�ڼ䣬�������б���
                if (enemy.inhit)
                {
                    if (time > enemyMeleeInternalData.lasthittime + enemyMelee.hitDuration)
                    {  //��ԭ
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
                    //������ڹ���������
                    if (enemy.state == Enemy.EnemyState.attack)
                    {
                        //�ж�ʱ���Ƿ񹥻��ж�
                        var t = time - enemyMeleeInternalData.lastattacktime;

                        if (t > 5d)
                        {
                            //���й����ж� ״̬��Ϊidle
                            Debug.Log(string.Format("hit player {0}", enemyMeleeInternalData.hitEntity));
                            enemy.state = Enemy.EnemyState.idle;
                            //commandBuffer.SetComponent(entityInQueryIndex, enemyMelee.entitynode, new URPMaterialPropertyBaseColor { Value = white4 });
                        }   //�ȴ� ɶ������
                    }
                    else
                    {
                        //ֱ�Ӽ���nav stop ͣ�������й�������,ͬʱ���ù�����ʼʱ��
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
                        //�Ƿ�ʱ
                        var t = time - enemyMeleeInternalData.lastattacktime;
                        if (t > 5d)
                        {
                             //��ԭ 
                            enemy.state = Enemy.EnemyState.idle;
                        }
                    }
                    else
                    {
                        //������
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