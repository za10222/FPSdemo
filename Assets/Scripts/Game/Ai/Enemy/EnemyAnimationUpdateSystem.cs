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
                var meshobject = Entity.Null;
                foreach(var i in children)
                {
                    if (HasComponent<SampleAnimationController>(i.Value))
                    {
                        meshobject = i.Value;
                        break;
                    }
                }
                if (meshobject == Entity.Null)
                    return;
                var sampleAnimationController = GetComponent<SampleAnimationController>(meshobject);
                switch (enemy.state){
                    case Enemy.EnemyState.walk:
                        sampleAnimationController.currentState = SampleAnimationController.state.walk;
                        SetComponent<SampleAnimationController>(meshobject, sampleAnimationController);
                        break;
                    case Enemy.EnemyState.idle:
                        sampleAnimationController.currentState = SampleAnimationController.state.idle;
                        SetComponent<SampleAnimationController>(meshobject, sampleAnimationController);
                        break;
                    case Enemy.EnemyState.attack:
                        sampleAnimationController.currentState = SampleAnimationController.state.attack;
                        SetComponent<SampleAnimationController>(meshobject, sampleAnimationController);
                        break;
                    case Enemy.EnemyState.dieing:
                        sampleAnimationController.currentState = SampleAnimationController.state.dieing;
                        SetComponent<SampleAnimationController>(meshobject, sampleAnimationController);
                        break;
                }
 
            }).Schedule();
        }
    }
}

