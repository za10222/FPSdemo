using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Reese.Nav;
using UnityEngine;
using Unity.Physics.Systems;
using Unity.Physics;
using Unity.NetCode;
//[UpdateInGroup()]
namespace FPSdemo
{


public class FindAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Find() );
    }
}
    [GhostComponent(PrefabType = GhostPrefabType.Server)]
    public struct Find:IComponentData
{
}


    [UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
    public class FindSystem : SystemBase
{
    EntityQuery m_FindGroup;
    private BuildPhysicsWorld physicsWorldSystem;
    NavSystem navSystem;
        override protected void  OnCreate()
    {
        EntityQueryDesc FindQuery = new EntityQueryDesc
        {
            All = new ComponentType[]
                   {
                typeof(Find),
                typeof(Translation),
                typeof(Rotation)
                   }

        };
        m_FindGroup = GetEntityQuery(FindQuery);
        physicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();
        navSystem = World.GetOrCreateSystem<NavSystem>();
    }
         protected override void OnUpdate()
    {
            var df = Time.DeltaTime;

            EntityCommandBuffer eb=new EntityCommandBuffer(Allocator.TempJob);

            var translations = m_FindGroup.ToComponentDataArray<Translation>(Allocator.TempJob);

            var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;
            var settings = navSystem.Settings;

            Entities
            .WithName("FindSystemJob")
            .WithAll<Enemy>()
            .ForEach((Entity entity, ref Enemy enemy,ref Rotation rotation, in LocalToWorld ltw,in NavAgent nav) =>
            {
                enemy.FindUpdateTime += df;
                if (enemy.FindUpdateTime<0.05d)
                {
                    return;
                }

                enemy.FindUpdateTime = 0d;

                if (enemy.state == Enemy.EnemyState.attack|| enemy.state == Enemy.EnemyState.dieing)
                    return;
                for (int i = 0; i < translations.Length; ++i)
                {
                    if (math.distancesq(ltw.Position, translations[i].Value) > enemy.distance)
                    {
                        break;
                    }


                    //先转过来 再做别的

                    var temp = translations[i].Value;
                    temp.y = ltw.Position.y;
                    var lookRotation = quaternion.LookRotationSafe(temp - ltw.Position, math.up());

                    var angle = Quaternion.Angle(lookRotation, rotation.Value);
                    if (angle > 1)
                    {

                        rotation.Value = math.slerp(rotation.Value, lookRotation, df / 0.1f);
                        return;
                    }
             

                    if (math.distancesq(translations[i].Value, ltw.Position) <= 1)
                    {
                        return;
                    }

                    var start = translations[i].Value;
                    var end = translations[i].Value;




                    start.y = 100;
                    end.y = -100;
                    Unity.Physics.RaycastHit hit = new Unity.Physics.RaycastHit();

                    RaycastInput input = new RaycastInput()
                    {
                        Filter = new CollisionFilter()
                        {
                            BelongsTo = NavUtil.ToBitMask(settings.ColliderLayer),
                            CollidesWith = NavUtil.ToBitMask(settings.SurfaceLayer),
                        }
                    };

                    var ishit = CommonUtilities.Raycast(start, end,  in collisionWorld, ref input, out hit);

                    if(!ishit)
                    {
                        continue;
                    }else
                    {
                     


                        eb.AddComponent<NavDestination>(entity);
                        eb.SetComponent<NavDestination>(entity, new NavDestination { WorldPoint = hit.Position });

                        return;
                        //Debug.Log(string.Format("hit:{0}", hit.Position));

                    }
                  
                }
            }).Schedule();

            Entities
           .WithName("BossFindSystemJob")
           .WithAll<EnemyBoss>()
           .ForEach((Entity entity, ref EnemyBoss enemyboss, ref Rotation rotation, in LocalToWorld ltw) =>
           {
               enemyboss.FindUpdateTime += df;
               if (enemyboss.FindUpdateTime < 0.05d)
               {
                   return;
               }

               enemyboss.FindUpdateTime = 0d;

               if (!(enemyboss.state == EnemyBoss.EnemyBossState.idle))
                   return;
               for (int i = 0; i < translations.Length; ++i)
               {
                   if (math.distance(ltw.Position, translations[i].Value) > enemyboss.findDistance)
                   {
                       continue;
                   }


                 //先转过来 再做别的

                 var temp = translations[i].Value;
                   temp.y = ltw.Position.y;
                   var lookRotation = quaternion.LookRotationSafe(temp - ltw.Position, math.up());

                   var angle = Quaternion.Angle(lookRotation, rotation.Value);
                   if (angle > 1)
                   {

                       rotation.Value = math.slerp(rotation.Value, lookRotation, df / 0.1f);
                       return;
                   }
               }
           }).Schedule();

            Dependency.Complete();
        eb.Playback(EntityManager);
            translations.Dispose();
            eb.Dispose();
    }
}
}
