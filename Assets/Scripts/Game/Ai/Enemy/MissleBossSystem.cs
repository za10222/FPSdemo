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
        EntityCommandBufferSystem barrier => World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        protected override void OnUpdate()
        {
            var time = Time.ElapsedTime;
            if(time-lastUpdateTime<0.5d)
            {
                return;
            }
            lastUpdateTime = time;
            var ltwFromEntity = GetComponentDataFromEntity<LocalToWorld>();
            var commandBuffer = barrier.CreateCommandBuffer().AsParallelWriter();
            Entities
                .WithReadOnly(ltwFromEntity)
               .ForEach((Entity entity, int entityInQueryIndex,in MissleBoss missleBoss) =>
               {
                   commandBuffer.AddComponent(entityInQueryIndex, entity, new NavDestination {WorldPoint=ltwFromEntity[missleBoss.find].Position});
               }).ScheduleParallel();
            barrier.AddJobHandleForProducer(Dependency);
        }
    }
}