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
    [UpdateBefore(typeof(EnemyBossControlSystem))]
    [UpdateBefore(typeof(EnemyMeleeControlSystem))]
    [UpdateBefore(typeof(EnemyRangeControlSystem))]

    public class TriggerEventHandleSystem : SystemBase
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
            var commandBuffer = barrier.CreateCommandBuffer();
            var time = Time.ElapsedTime;
            var events = ((Simulation)stepPhysicsWorld.Simulation).TriggerEvents;
            foreach (var i in events)
            {
                Entity enemy = Entity.Null;
                Entity play = Entity.Null;
                if ((HasComponent<Find>(i.EntityA) && HasComponent<EnemyMelee>(i.EntityB)))
                {
                    enemy = i.EntityB;
                    play = i.EntityA;
                    var enemyMeleeInternalData = GetComponent<EnemyMeleeInternalData>(enemy);
                    enemyMeleeInternalData.hasFind = true;
                    enemyMeleeInternalData.hitEntity = play;
                    SetComponent(enemy, enemyMeleeInternalData );
                    continue;
                }
                if ((HasComponent<Find>(i.EntityB) && HasComponent<EnemyMelee>(i.EntityA)))
                {
                    enemy = i.EntityA;
                    play = i.EntityB;
                    var enemyMeleeInternalData = GetComponent<EnemyMeleeInternalData>(enemy);
                    enemyMeleeInternalData.hasFind = true;
                    enemyMeleeInternalData.hitEntity = play;
                    SetComponent(enemy, enemyMeleeInternalData);
                    continue;
                }

                Entity enemyboss = Entity.Null;
                if ((HasComponent<Find>(i.EntityA) && HasComponent<EnemyBoss>(i.EntityB)))
                {
                    enemyboss = i.EntityB;
                    play = i.EntityA;

                    var intel = GetComponent<CharacterControllerInternalData>(play);
                    var rot = GetComponent<Translation>(play).Value;
                    var rot2 = GetComponent<LocalToWorld>(enemyboss).Position;

                    intel.addVelocity = -math.normalize(rot2 - rot) * 10;
                    intel.starttime = 0.3d;
                    var healthbuff = GetBuffer<HealthEventBufferElement>(play);
                    healthbuff.Add(new HealthEventBufferElement { healthChange = -20 });
                    SetComponent(play, intel);
                    continue; 
                }  
                if ((HasComponent<Find>(i.EntityB) && HasComponent<EnemyBoss>(i.EntityA)))
                {
                    enemyboss = i.EntityA;
                    play = i.EntityB;
                    var intel = GetComponent<CharacterControllerInternalData>(play);
                    var rot = GetComponent<Translation>(play).Value;
                    var rot2 = GetComponent<LocalToWorld>(enemyboss).Position;
                    intel.addVelocity = -math.normalize(rot2 - rot) * 10;
                    intel.starttime = 0.3d;
                    var healthbuff = GetBuffer<HealthEventBufferElement>(play);
                    healthbuff.Add(new HealthEventBufferElement { healthChange = -20 });
                    SetComponent(play, intel);

                    continue;
                }

                if ((HasComponent<Find>(i.EntityA) && HasComponent<Missile>(i.EntityB)))
                {
                    var missle = i.EntityB;
                    play = i.EntityA;


                    var healthbuff = GetBuffer<HealthEventBufferElement>(play);
                    healthbuff.Add(new HealthEventBufferElement { healthChange = -5 });
                    commandBuffer.DestroyEntity(missle);
                    continue;
                }
                if ((HasComponent<Find>(i.EntityB) && HasComponent<Missile>(i.EntityA)))
                {
                    var missle = i.EntityA;
                    play = i.EntityB;
                    var healthbuff = GetBuffer<HealthEventBufferElement>(play);
                    healthbuff.Add(new HealthEventBufferElement { healthChange=-5});

                    commandBuffer.DestroyEntity(missle);

                     
                    continue;
                }
            }
         
        }
    }
}