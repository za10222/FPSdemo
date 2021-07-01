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
    }
    [UpdateAfter(typeof(EnemyMeleeControlSystem))]
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
                  
                }
                healthEventBuffers.Clear();
                // Implement the work to perform for each entity here.

            }).Schedule();
        }
    }
}