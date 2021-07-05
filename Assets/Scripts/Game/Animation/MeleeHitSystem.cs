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
     
        protected override void OnUpdate()
        {
            EntityCommandBuffer commandBuffer =new EntityCommandBuffer(Allocator.TempJob);
            Vector4 wet = Color.white;
            float4 white4 = wet;
            Vector4 red = Color.red;
            float4 red4 = red;
            Entities.ForEach((in Enemy enemy) =>
            {
                if (enemy.inhit)
                {

                    commandBuffer.SetComponent( enemy.Bodynode, new URPMaterialPropertyBaseColor
                    {
                        Value = red4
                    });


                }
                else
                {
                    commandBuffer.SetComponent( enemy.Bodynode, new URPMaterialPropertyBaseColor
                    {
                        Value = white4
                    });
                }
           
            }).Schedule();
            Entities.ForEach((in EnemyBoss enemyboss) =>
            {
                if (enemyboss.inhit)
                {

                    commandBuffer.SetComponent(enemyboss.Bodyup, new URPMaterialPropertyBaseColor
                    {
                        Value =new float4(1f,0f,0f,0.5882f)
                    });
                    commandBuffer.SetComponent(enemyboss.Bodylow, new URPMaterialPropertyBaseColor
                    {
                        Value = new float4(1f, 0f, 0f, 0.5882f)
                    });
                }
                else
                {
                    commandBuffer.SetComponent(enemyboss.Bodyup, new URPMaterialPropertyBaseColor
                    {
                        Value = white4
                    });
                    commandBuffer.SetComponent(enemyboss.Bodylow, new URPMaterialPropertyBaseColor
                    {
                        Value = white4
                    });
                }

            }).Schedule();
            Dependency.Complete();

            commandBuffer.Playback(EntityManager);
        }
    }
}