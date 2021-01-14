using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;


namespace FPSdemo
{



    [DisallowMultipleComponent]
    public class CameraFollower : MonoBehaviour, IConvertGameObjectToEntity
    {
        public GameObject Target;


        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {

            //conversionSystem.AddHybridComponent(Target);
            dstManager.AddComponentData(entity, new CameraFollowSettings { Target = conversionSystem.GetPrimaryEntity(Target) });
        }
        void Start()
        {

        }
    }
   
    struct CameraFollowSettings : IComponentData
    {
        public Entity Target;
    }


    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    //[UpdateAfter(typeof(TransformSystemGroup))]
    class CameraFollowSystem : SystemBase
    {
        private BuildPhysicsWorld m_BuildPhysicsWorld;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_BuildPhysicsWorld = World.GetExistingSystem<BuildPhysicsWorld>();
         
        }
        protected override void OnUpdate()
        {
            PhysicsWorld world = m_BuildPhysicsWorld.PhysicsWorld;
            Entities
               .WithName("CameraFollowSystemJob")
               .WithoutBurst()
               .WithReadOnly(world)
               .ForEach((CameraFollower monoBehaviour, ref CameraFollowSettings camera) =>
               {
                   var worldtran=monoBehaviour.transform;
                   var newPosition = HasComponent<LocalToWorld>(camera.Target) ?
                   GetComponent<LocalToWorld>(camera.Target).Position
                   :(float3)worldtran.position;

                   quaternion newRotation = HasComponent<LocalToWorld>(camera.Target) ?
                 GetComponent<LocalToWorld>(camera.Target).Rotation
                : (quaternion)worldtran.rotation;
                   monoBehaviour.transform.SetPositionAndRotation(newPosition, newRotation);
               }).Run();
        }
    }
}