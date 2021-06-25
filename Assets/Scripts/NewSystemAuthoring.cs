using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Reese.Nav;
using UnityEngine;
//[UpdateInGroup()]
public class NewSystemAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Testsd() );
    }
}

public struct Testsd:IComponentData
{

}


//[DisableAutoCreation]
public class NewSystem : SystemBase
{

    override protected void  OnCreate()
    {
        
    }
         protected override void OnUpdate()
    {
        EntityCommandBuffer eb=new EntityCommandBuffer(Allocator.Temp);
        Entities
            .WithName("test")
            .WithAll<Testsd>()
            .WithoutBurst()
            .ForEach((Entity entity) =>
            {
               
                eb.RemoveComponent<Testsd>(entity);
                eb.AddComponent<NavDestination>(entity);
                eb.SetComponent<NavDestination>(entity, new NavDestination { WorldPoint = new float3(20, 0, 42) });
            }).Run();
        eb.Playback(EntityManager);
    }
}
