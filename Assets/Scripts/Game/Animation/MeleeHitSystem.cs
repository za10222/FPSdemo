using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Rendering;
using UnityEngine;

namespace FPSdemo
{
    [UpdateInGroup(typeof(ClientPresentationSystemGroup))]
    public class MeleeHitSystem : SystemBase
    {
        EntityCommandBufferSystem barrier => World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        protected override void OnUpdate()
        {

            Vector4 wet = Color.white;
            float4 white4 = wet;
            Vector4 red = Color.red;
            float4 red4 = red;
            var commandBuffer = barrier.CreateCommandBuffer().AsParallelWriter();
            Entities.ForEach((int entityInQueryIndex, in Enemy enemy) =>
            {
                if (enemy.inhit)
                {
                    commandBuffer.SetComponent(entityInQueryIndex, enemy.Bodynode, new URPMaterialPropertyBaseColor
                    {
                        Value = red4
                    });

                }
                else
                {
                    commandBuffer.SetComponent(entityInQueryIndex, enemy.Bodynode, new URPMaterialPropertyBaseColor
                    {
                        Value = white4
                    });
                }
           
            }).Schedule();
        }
    }
}