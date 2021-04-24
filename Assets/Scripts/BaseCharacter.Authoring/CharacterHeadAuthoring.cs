using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;

namespace FPSdemo
{


    public struct CharacterHead : IComponentData
    {
        public float VerticalRotationSpeed;
        public Vector2 MaxminAngle;
        public float Vertical;
    }


    public class CharacterHeadAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {

        // Referenced prefabs have to be declared so that the conversion system knows about them ahead of time
        public float VerticalRotationSpeed = 20;
        public Vector2 MaxminAngle = new Vector2(65, -65);
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(
                entity,
                new CharacterHead
                {
                    VerticalRotationSpeed = VerticalRotationSpeed,
                    MaxminAngle = MaxminAngle,
                    Vertical = 0
                }); ;
        }
    }

    // Update before physics gets going so that we don't have hazard warnings.
    // This assumes that all gun are being controlled from the same single input system
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(CharacterControllerSystem))]
    public class CharacterControllerHeadSystem : SystemBase
    {

        protected override void OnCreate()
        {
            //EntityQuery m_UserInputdateQuery = GetEntityQuery(typeof(UserInput.UserInputdate));
            //RequireForUpdate(eq);
        }
        protected override void OnUpdate()
        {
            //EntityQuery m_UserInputdateQuery = GetEntityQuery(typeof(UserInput.UserInputdate));
            //if (m_UserInputdateQuery.CalculateEntityCount() == 0)
            //    return;
            //var input = m_UserInputdateQuery.GetSingleton<UserInput.UserInputdate>().userinput;
            //float dt = Time.DeltaTime;

            //Entities
            //    .WithName("CharacterControllerHeadJob")
            //    .WithoutBurst()
            //    .ForEach((ref Rotation headRotation, ref CharacterHead head) =>
            //    {
            //    // Handle input
            //        float a = -input.Looking.y * head.VerticalRotationSpeed * dt;
            //        head.Vertical += a;
            //        head.Vertical = math.clamp(head.Vertical, head.MaxminAngle.y, head.MaxminAngle.x);
            //        Debug.Log("head="+head.Vertical);
            //        headRotation.Value = quaternion.Euler(math.radians(head.Vertical), 0, 0);
            //    }).Run();
        }
    }
}