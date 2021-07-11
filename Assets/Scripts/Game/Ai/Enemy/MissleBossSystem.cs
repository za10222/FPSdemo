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
    public class MissleBossSystem : SystemBase
    {
        private double lastUpdateTime;
        private NavSystem navSystem;
        private BuildPhysicsWorld physicsWorldSystem;
        private StepPhysicsWorld stepPhysicsWorld;
        EntityCommandBufferSystem barrier => World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        override protected void OnCreate()
        {
            navSystem = World.GetOrCreateSystem<NavSystem>();
            physicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();
            stepPhysicsWorld = World.GetExistingSystem<StepPhysicsWorld>();
            RequireSingletonForUpdate<GunDataBufferElement>();
        }

        protected override void OnUpdate()
        {
             
            var df = Time.DeltaTime;
            var events = ((Simulation)stepPhysicsWorld.Simulation).TriggerEvents;

            foreach (var i in events)
            {
                Entity missle = Entity.Null;
                Entity play = Entity.Null;
                if ((HasComponent<MissleBoss>(i.EntityA) && HasComponent<Find>(i.EntityB)))
                {
                    missle = i.EntityA;
                    play = i.EntityB;
                    var missleBoss = GetComponent<MissleBoss>(missle);
                    missleBoss.hitFind = true;
                    missleBoss.hitEntity = play;
                    SetComponent(missle, missleBoss);
                    continue;
                }
                if ((HasComponent<MissleBoss>(i.EntityB) && HasComponent<Find>(i.EntityA)))
                {
                    missle = i.EntityB;
                    play = i.EntityA;
                    var missleBoss = GetComponent<MissleBoss>(missle);
                    missleBoss.hitFind = true;
                    missleBoss.hitEntity = play;
                    SetComponent(missle, missleBoss);
                    continue;
                }
            } 
            
            var tranFromEntity = GetComponentDataFromEntity<Translation>();
           
            var commandBuffer = barrier.CreateCommandBuffer().AsParallelWriter();
            var setting = navSystem.Settings;
            var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;
            var bufferFromEntity = GetBufferFromEntity<HealthEventBufferElement>();
            var time = Time.ElapsedTime;
            Entities
                .WithNativeDisableParallelForRestriction(bufferFromEntity)
                .WithReadOnly(collisionWorld)
                .WithReadOnly(tranFromEntity)
               .ForEach((Entity entity, int entityInQueryIndex,ref MissleBoss missleBoss) =>
               {
                   var health = GetComponent<HealthData>(entity);
                   if (health.currentHp == 0)
                   {
                          commandBuffer.DestroyEntity(entityInQueryIndex, entity);

                       return;
                   }


                   if (missleBoss.hitFind)
                   {
                       bufferFromEntity[missleBoss.hitEntity].Add(new HealthEventBufferElement { healthChange=-20});
                       commandBuffer.DestroyEntity(entityInQueryIndex,entity);
                   }
                   else 
                   {
                       if (missleBoss.inhit)
                       {
                           if (time > missleBoss.lasthittime + missleBoss.hitDuration)
                           {  //»¹Ô­
                              //commandBuffer.SetComponent(entityInQueryIndex, enemyMelee.entitynode, new URPMaterialPropertyBaseColor
                              //{
                              //    Value = white4
                              //});
                               missleBoss.inhit = false;
                           }

                       }
                       else
                       {
                           if ((time > missleBoss.lasthittime + missleBoss.hitDuration + missleBoss.recoverDuration) &&
                           bufferFromEntity.HasComponent(entity) && bufferFromEntity[entity].Length > 0)
                           {
                               //commandBuffer.SetComponent(entityInQueryIndex, enemyMelee.entitynode, new URPMaterialPropertyBaseColor
                               //{
                               //    Value = red4
                               //});
                               missleBoss.inhit = true;
                               missleBoss.lasthittime = time;
                           }
                       }

                       missleBoss.navupdatedftime+=df;
                       if (missleBoss.navupdatedftime<0.5)
                       {
                           return;
                       }
                       missleBoss.navupdatedftime = 0;
                       var start = tranFromEntity[missleBoss.find].Value;
                       var end = tranFromEntity[missleBoss.find].Value;
                   


                       start.y = 100;
                       end.y = -100;
                       Unity.Physics.RaycastHit hit = new Unity.Physics.RaycastHit();

                       RaycastInput input = new RaycastInput()
                       {
                           Filter = new CollisionFilter()
                           {
                               BelongsTo = NavUtil.ToBitMask(setting.ColliderLayer),
                               CollidesWith = NavUtil.ToBitMask(setting.SurfaceLayer),
                           }
                       };

                       var ishit = CommonUtilities.Raycast(start, end, in collisionWorld, ref input, out hit);
                       if(ishit)
                       {
                           commandBuffer.AddComponent(entityInQueryIndex, entity, new NavDestination { WorldPoint = hit.Position });
                          
                       }

                   }
               }).ScheduleParallel();
            barrier.AddJobHandleForProducer(Dependency);
        }
    }
}