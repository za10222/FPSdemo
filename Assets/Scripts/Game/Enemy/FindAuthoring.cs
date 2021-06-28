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

public struct Find:IComponentData
{
}


    //[DisableAutoCreation]
    public class NewSystem : SystemBase
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
        EntityCommandBuffer eb=new EntityCommandBuffer(Allocator.Temp);

        var translations = m_FindGroup.ToComponentDataArray<Translation>(Allocator.Temp);

        var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;
        var settings = navSystem.Settings;
        var df = Time.DeltaTime;

        Entities
            .WithName("test")
            .WithAll<Enemy>()
            .WithoutBurst()
            .ForEach((Entity entity, ref Enemy enemy,ref Rotation rotation, in LocalToWorld ltw,in NavAgent nav) =>
            {

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
                    if (angle > 5)
                    {

                        rotation.Value = math.slerp(rotation.Value, lookRotation, df / 0.3f);
                        return;
                    }
                    //var disforward = translations[i].Value - ltw.Position;
                    //var disnormal = math.normalize(disforward );
                    //var offsetpos = disforward - disnormal*1.5f + ltw.Position;


                    //var start = offsetpos;
                    //var end = offsetpos;


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
            }).Run();
        eb.Playback(EntityManager);
    }
}
}
