//using Unity.Entities;
//using Unity.Jobs;
//using Unity.Mathematics;
//using Unity.Rendering;
//using Unity.Transforms;
//using UnityEngine;

//[UpdateInGroup(typeof(SimulationSystemGroup))]
//public class CubeGameSystem : JobComponentSystem
//{
//    protected override void OnCreate()
//    {
//        base.OnCreate();
//        var myCube = EntityManager.CreateEntity(
//            ComponentType.ReadOnly<LocalToWorld>(),
//            ComponentType.ReadOnly<RenderMesh>(),
//            ComponentType.ReadOnly<RenderBounds>()
//        );
//        EntityManager.SetComponentData(myCube, new LocalToWorld
//        {
//            Value = new float4x4(rotation: quaternion.identity, translation: new float3(1, 2, 3))
//        });
//        var ah = Resources.Load<GameObject>("Prefabs\\123").GetComponent<AssetHolder2>();
//        EntityManager.SetSharedComponentData(myCube, new RenderMesh
//        {
//            mesh = ah.myMesh,
//            material = ah.myMaterial
//        });
//        EntityManager.SetName(myCube, "cube1234");
//        //World.DefaultGameObjectInjectionWorld = w;
//        Debug.Log(123);
//    }

//    protected override JobHandle OnUpdate(JobHandle inputDeps)
//    {
//        return default;
//    }
//}