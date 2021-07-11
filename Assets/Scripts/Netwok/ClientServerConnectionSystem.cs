using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;
using Unity.NetCode;
using UnityEngine;
#if UNITY_EDITOR
using Unity.NetCode.Editor;
#endif


//[DisableAutoCreation]
[UpdateInWorld(UpdateInWorld.TargetWorld.Default)]
public class ClientServerConnectionSystem : SystemBase
{
    private const ushort networkPort = 50001;

    private struct InitializeClientServer : IComponentData
    {
    }

    protected override void OnCreate()
    {
        RequireSingletonForUpdate<InitializeClientServer>();
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Game")
            return;
        var initEntity = EntityManager.CreateEntity(typeof(InitializeClientServer));
    }

    protected override void OnUpdate()
    {
        if (!UiManager.needtoconnect)
            return;
        Debug.Log(ClientServerBootstrap.RequestedPlayType);
        EntityManager.DestroyEntity(GetSingletonEntity<InitializeClientServer>());
        foreach (var world in World.All)
        {
#if !UNITY_CLIENT || UNITY_SERVER || UNITY_EDITOR
            // Bind the server and start listening for connections
            if (world.GetExistingSystem<ServerSimulationSystemGroup>() != null)
            {
                NetworkEndPoint ep = NetworkEndPoint.AnyIpv4;
                ep.Port = networkPort;
                world.GetExistingSystem<NetworkStreamReceiveSystem>().Listen(ep);
                Debug.Log("Server listen");
            }

#endif
#if !UNITY_SERVER
          
            // Auto connect all clients to the server
            if (world.GetExistingSystem<ClientSimulationSystemGroup>() != null)
            {
                NetworkEndPoint ep = NetworkEndPoint.LoopbackIpv4;
                ep.Port = networkPort;
                world.GetExistingSystem<NetworkStreamReceiveSystem>().Connect(ep);
                Debug.Log("Clinet connect");
            }
#endif
        }
    }
}
