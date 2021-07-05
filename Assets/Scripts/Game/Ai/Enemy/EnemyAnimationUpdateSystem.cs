using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;

namespace FPSdemo
{
    [UpdateInGroup(typeof(ClientPresentationSystemGroup))]
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

            Entities.WithName("EnemyBossAnimationUpdateSystem").WithAll<EnemyBoss>()
            .ForEach((Entity entity, in DynamicBuffer<Child> children, in EnemyBoss enemy) => {
                var meshobject = Entity.Null;
                foreach (var i in children)
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
                switch (enemy.state)
                {
                    case EnemyBoss.EnemyBossState.idle:
                        sampleAnimationController.currentState = SampleAnimationController.state.idle;
                        SetComponent<SampleAnimationController>(meshobject, sampleAnimationController);
                        break;
                    case EnemyBoss.EnemyBossState.shoot:
                        sampleAnimationController.currentState = SampleAnimationController.state.shoot;
                        SetComponent<SampleAnimationController>(meshobject, sampleAnimationController);
                        break;
                    case EnemyBoss.EnemyBossState.bigshoot:
                        sampleAnimationController.currentState = SampleAnimationController.state.bigShoot;
                        SetComponent<SampleAnimationController>(meshobject, sampleAnimationController);
                        break;
                    case EnemyBoss.EnemyBossState.taunt:
                        sampleAnimationController.currentState = SampleAnimationController.state.taunt;
                        SetComponent<SampleAnimationController>(meshobject, sampleAnimationController);
                        break;
                    case EnemyBoss.EnemyBossState.dieing:
                        sampleAnimationController.currentState = SampleAnimationController.state.dieing;
                        SetComponent<SampleAnimationController>(meshobject, sampleAnimationController);
                        break;
                }

            }).Schedule();
        }
    }
}

