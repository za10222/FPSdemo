
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using static FPSdemo.UserInput;

namespace FPSdemo
{

[DisableAutoCreation]
[UpdateInGroup(typeof(GhostInputSystemGroup))]
[UpdateAfter(typeof(UserInputUpdateSystem))]
    public class PlayerInputCommandSystem :SystemBase
{
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<NetworkIdComponent>();
            RequireSingletonForUpdate<UserInput.UserInputdate>();
            RequireSingletonForUpdate<NetworkStreamInGame>();
        }

        protected override void OnUpdate()
        {
            var localInput = GetSingleton<CommandTargetComponent>().targetEntity;
            if (localInput == Entity.Null)
            {
                Entities.WithoutBurst().ForEach((Entity ent, DynamicBuffer<UserCommand> data) =>
                {
                    SetSingleton(new CommandTargetComponent { targetEntity = ent });
                }).Run();
                return;
            }
            var input = default(UserCommand);
            var uesrinput = GetSingleton<UserInput.UserInputdate>();
            input = uesrinput.userinput;
            input.Tick = World.GetExistingSystem<ClientSimulationSystemGroup>().ServerTick;
            var inputBuffer = EntityManager.GetBuffer<UserCommand>(localInput);
            inputBuffer.AddCommandData(input);
        }
    }

}