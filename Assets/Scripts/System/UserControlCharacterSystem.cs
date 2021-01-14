using Unity.Entities;


namespace FPSdemo{ 
// This input system simply applies the same character input
// information to every character controller in the scene
[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(UserInputUpdateSystem))]
    public class UpdateInputSystem : SystemBase
{
        private EntityQuery m_CharacterControllerInputQuery;

        protected override void OnCreate()
        {
            m_CharacterControllerInputQuery = GetEntityQuery(typeof(CharacterControllerInput));
        }
        protected override void OnUpdate()
    {
        if (m_CharacterControllerInputQuery.CalculateEntityCount() == 0)
            return;
        var input = m_CharacterControllerInputQuery.GetSingleton<CharacterControllerInput>();
    
        // Read user input
        
        Entities
            .WithName("UpdateInputSystemJob")
            .WithAll<CharacterControllerInternalData,UserControlDate>()
            .WithBurst()
            .ForEach((ref CharacterControllerInternalData ccData) =>
            {
                ccData.Input.Movement = input.Movement;
                ccData.Input.Looking = input.Looking;
                // jump request may not be processed on this frame, so record it rather than matching input state
                if (input.Jumped != 0)
                    ccData.Input.Jumped = 1;
            }
            ).ScheduleParallel();
    }
}
}