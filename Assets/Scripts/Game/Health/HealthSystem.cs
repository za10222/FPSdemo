using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace FPSdemo
{
    public struct HealthEventBufferElement : IBufferElementData
    {
       public float healthChange;
       public int owner;
    }

    [UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
    [UpdateAfter(typeof(EnemyMeleeControlSystem))]
    [UpdateAfter(typeof(EnemyRangeControlSystem))]
    public class HealthSystem : SystemBase
    {


        protected override void OnUpdate()
        {

            Entities.ForEach((Entity entity, ref HealthData healthData,ref DynamicBuffer<HealthEventBufferElement> healthEventBuffers ) =>
            {
                for (int i = 0; i < healthEventBuffers.Length; i++)
                {
                    healthData.currentHp += healthEventBuffers[i].healthChange;
                    healthData.currentHp = math.clamp(healthData.currentHp, 0, healthData.maxHp);
                    healthData.lasthit = healthEventBuffers[i].owner;
                }
                healthEventBuffers.Clear();
                // Implement the work to perform for each entity here.

            }).Schedule();
        }
    }
}