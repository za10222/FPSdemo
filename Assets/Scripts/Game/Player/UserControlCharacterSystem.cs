using Unity.Entities;


namespace FPSdemo{ 


[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(UserInput.UserInputUpdateSystem))]
    public class UpdateInputSystem : SystemBase
{
        private EntityQuery m_UserInputQuery;

        protected override void OnCreate()
        {
            m_UserInputQuery = GetEntityQuery(typeof(UserInput.UserInputdate));
        }
        protected override void OnUpdate()
    {
        if (m_UserInputQuery.CalculateEntityCount() == 0)
            return;
        var input = m_UserInputQuery.GetSingleton<UserInput.UserInputdate>();
        
        Entities
            .WithName("UpdateInputSystemJob")
            .WithAll<CharacterControllerInternalData,UserControlDate>()
            .WithBurst()
            .ForEach((ref CharacterControllerInternalData ccData) =>
            {
                ccData.Input.Movement = input.userinput.Movement;
                ccData.Input.Looking = input.userinput.Looking;
                if (input.userinput.buttons.IsSet(UserCommand.Button.Jump))
                    ccData.Input.Jumped = 1;
            }
            ).ScheduleParallel();
    }
}
}