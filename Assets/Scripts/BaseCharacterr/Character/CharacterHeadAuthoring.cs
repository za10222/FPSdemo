using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;

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
    public Vector2 MaxminAngle=new Vector2(65, -65);
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
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(CharacterControllerSystem))]
public class CharacterControllerHeadSystem : SystemBase
{

    protected override void OnUpdate()
    {
        var input = GetSingleton<CharacterControllerInput>();
        float dt = Time.DeltaTime;

        Entities
            .WithName("CharacterControllerHeadJob")
            .WithoutBurst()
            .ForEach((ref Rotation headRotation,ref CharacterHead head) =>
            {
                // Handle input
                float a = -input.Looking.y* head.VerticalRotationSpeed*dt;
                head.Vertical+=a;
                head.Vertical = math.clamp(head.Vertical, head.MaxminAngle.y, head.MaxminAngle.x);

                headRotation.Value =  quaternion.Euler(math.radians(head.Vertical), 0, 0);
              
            }).Run();
    }
}
