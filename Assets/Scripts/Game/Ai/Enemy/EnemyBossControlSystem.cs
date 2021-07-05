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
          
            var bufferFromEntity= GetBufferFromEntity<HealthEventBufferElement>();


            var time = Time.ElapsedTime;


            Entities
                .WithReadOnly(bufferFromEntity)
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



            }).ScheduleParallel();
            barrier.AddJobHandleForProducer(Dependency);
        }
    }
}