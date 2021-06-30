using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace FPSdemo
{
    public class EnemyAnimationSystem : SystemBase
    {
        protected override void OnUpdate()
        {

            Entities.WithName("EnemyAnimationUpdateSystem").WithAll<Enemy>()
            .ForEach((Entity entity, DynamicBuffer<Child> children) => {

                if (!HasComponent<SampleAnimationController>(children[0].Value))
                {
                    return;
                }
                var sampleAnimationController = GetComponent<SampleAnimationController>(children[0].Value);
                if (HasComponent<Reese.Nav.NavWalking>(entity))
                {

                    sampleAnimationController.currentState = SampleAnimationController.state.walk;
                    SetComponent<SampleAnimationController>(children[0].Value, sampleAnimationController);
                }
                else
                {
                    sampleAnimationController.currentState = SampleAnimationController.state.idle;
                    SetComponent<SampleAnimationController>(children[0].Value, sampleAnimationController);
                    
                }
            }).Schedule();
        }
    }
}

