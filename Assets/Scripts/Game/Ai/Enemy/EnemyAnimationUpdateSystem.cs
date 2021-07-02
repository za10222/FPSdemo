using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace FPSdemo
{
    public class EnemyAnimationUpdateSystem : SystemBase
    {
        protected override void OnUpdate()
        {

            Entities.WithName("EnemyAnimationUpdateSystem").WithAll<Enemy>()
            .ForEach((Entity entity,in DynamicBuffer<Child> children,in Enemy enemy) => {

                if (!HasComponent<SampleAnimationController>(children[0].Value))
                {
                    return;
                }
                var sampleAnimationController = GetComponent<SampleAnimationController>(children[0].Value);
                switch (enemy.state){
                    case Enemy.EnemyState.walk:
                        sampleAnimationController.currentState = SampleAnimationController.state.walk;
                        SetComponent<SampleAnimationController>(children[0].Value, sampleAnimationController);
                        break;
                    case Enemy.EnemyState.idle:
                        sampleAnimationController.currentState = SampleAnimationController.state.idle;
                        SetComponent<SampleAnimationController>(children[0].Value, sampleAnimationController);
                        break;
                    case Enemy.EnemyState.attack:
                        sampleAnimationController.currentState = SampleAnimationController.state.attack;
                        SetComponent<SampleAnimationController>(children[0].Value, sampleAnimationController);
                        break;
                    case Enemy.EnemyState.dieing:
                        sampleAnimationController.currentState = SampleAnimationController.state.dieing;
                        SetComponent<SampleAnimationController>(children[0].Value, sampleAnimationController);
                        break;
                }
 
            }).Schedule();
        }
    }
}

