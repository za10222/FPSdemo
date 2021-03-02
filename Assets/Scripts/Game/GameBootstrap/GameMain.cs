using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace FPSdemo
{ 
[DefaultExecutionOrder(-1000)]
public class GameMain: MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
            
        if (GameBootstrap.clientworld != null)
        {
                var w = GameBootstrap.clientworld.GetExistingSystem<InitializationSystemGroup>();
                w.AddSystemToUpdateList(GameBootstrap.clientworld.CreateSystem<PlayerCameraControl.HandlePlayerCameraControlSpawn>());
                w.AddSystemToUpdateList(GameBootstrap.clientworld.CreateSystem<PlayerCameraControl.UpdatePlayerCameras>());
                w.AddSystemToUpdateList(GameBootstrap.clientworld.CreateSystem<PlayerCameraControl.CleanPlayerCameras>());
                w.AddSystemToUpdateList(GameBootstrap.clientworld.CreateSystem<UserInput.UserInputUpdateSystem>());

                var c = GameBootstrap.clientworld.GetExistingSystem<ClientSimulationSystemGroup>();
                c.AddSystemToUpdateList(GameBootstrap.clientworld.CreateSystem<PlayerInputCommandSystem>());
                c.AddSystemToUpdateList(GameBootstrap.clientworld.CreateSystem<GoInGameClientSystem>());
                var F = GameBootstrap.clientworld.GetExistingSystem<FixedStepSimulationSystemGroup>();
                //F.AddSystemToUpdateList(GameBootstrap.clientworld.CreateSystem<CharacterControllerHeadSystem>());
                F.AddSystemToUpdateList(GameBootstrap.clientworld.CreateSystem<BufferInterpolatedCharacterControllerMotion>());
                F.AddSystemToUpdateList(GameBootstrap.clientworld.CreateSystem<CharacterControllerSystem>());
                F.AddSystemToUpdateList(GameBootstrap.clientworld.CreateSystem<PlayerCameraControl.PlayCameraUserInputUpdateSystem>());

                var p= GameBootstrap.clientworld.GetExistingSystem<GhostPredictionSystemGroup>();
                p.AddSystemToUpdateList(GameBootstrap.clientworld.CreateSystem<UpdateCharacterControllerInternalDataSystem>());
            }
            if (GameBootstrap.serverworld!=null)
            {
                var w = GameBootstrap.serverworld.GetExistingSystem<InitializationSystemGroup>();
 

                var c = GameBootstrap.serverworld.GetExistingSystem<ServerSimulationSystemGroup>();
                c.AddSystemToUpdateList(GameBootstrap.serverworld.CreateSystem<GoInGameServerSystem>());
                var F = GameBootstrap.serverworld.GetExistingSystem<FixedStepSimulationSystemGroup>();
                //F.AddSystemToUpdateList(GameBootstrap.serverworld.CreateSystem<CharacterControllerHeadSystem>());
                F.AddSystemToUpdateList(GameBootstrap.serverworld.CreateSystem<BufferInterpolatedCharacterControllerMotion>());
                F.AddSystemToUpdateList(GameBootstrap.serverworld.CreateSystem<CharacterControllerSystem>());

                var p = GameBootstrap.serverworld.GetExistingSystem<GhostPredictionSystemGroup>();
                p.AddSystemToUpdateList(GameBootstrap.serverworld.CreateSystem<UpdateCharacterControllerInternalDataSystem>());
            }
        }

}
public class GameBootstrap : ClientServerBootstrap
{
    public static World clientworld;
    public static World serverworld;
    public override bool Initialize(string defaultWorldName)
    {
        var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default);
        GenerateSystemLists(systems);

        var world = new World(defaultWorldName);
        World.DefaultGameObjectInjectionWorld = world;

        DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, ExplicitDefaultWorldSystems);
#if !UNITY_SERVER
#if UNITY_EDITOR
        if (ClientServerBootstrap.RequestedPlayType != ClientServerBootstrap.PlayType.Server)

#endif
        clientworld = ClientServerBootstrap.CreateClientWorld(world, "clientworld123");
#endif
#if !UNITY_CLIENT || UNITY_SERVER || UNITY_EDITOR
    #if UNITY_EDITOR
       if (ClientServerBootstrap.RequestedPlayType != ClientServerBootstrap.PlayType.Client)
    #endif
          serverworld = ClientServerBootstrap.CreateServerWorld(world, "severworld123");
#endif
        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);
        return true;
    }

}
}
