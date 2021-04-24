using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace FPSdemo
{
    public struct GoInGameRequest : IRpcCommand
    {
    }

    [DisableAutoCreation]
    [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
    [AlwaysSynchronizeSystem]
    public class GoInGameClientSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate(GetEntityQuery(ComponentType.ReadOnly<NetworkIdComponent>(), ComponentType.Exclude<NetworkStreamInGame>()));
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            Entities.WithNone<NetworkStreamInGame>().ForEach((Entity ent, in NetworkIdComponent id) =>
            {
                commandBuffer.AddComponent<NetworkStreamInGame>(ent);
                var req = commandBuffer.CreateEntity();
                commandBuffer.AddComponent<GoInGameRequest>(req);
                commandBuffer.AddComponent(req, new SendRpcCommandRequestComponent { TargetConnection = ent });
            }).Run();
            commandBuffer.Playback(EntityManager);
        }
    }

    [DisableAutoCreation]
    [UpdateInGroup(typeof(ServerSimulationSystemGroup))]
    [AlwaysSynchronizeSystem]
    public class GoInGameServerSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate(GetEntityQuery(ComponentType.ReadOnly<GoInGameRequest>(), ComponentType.ReadOnly<ReceiveRpcCommandRequestComponent>()));
        }

        protected override void OnUpdate()
        {
            var ghostCollection = GetSingletonEntity<GhostPrefabCollectionComponent>();
            var prefab = Entity.Null;
            var prefabs = EntityManager.GetBuffer<GhostPrefabBuffer>(ghostCollection);
            for (int ghostId = 0; ghostId < prefabs.Length; ++ghostId)
            {
                if (EntityManager.HasComponent<CharacterControllerInternalData>(prefabs[ghostId].Value))
                    prefab = prefabs[ghostId].Value;
            }
            if(prefab== Entity.Null)
            {
                UnityEngine.Debug.Log("can not find prefab" );
                return;
            }
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            var networkIdFromEntity = GetComponentDataFromEntity<NetworkIdComponent>(true);
            Entities.WithReadOnly(networkIdFromEntity).ForEach((Entity reqEnt, in GoInGameRequest req, in ReceiveRpcCommandRequestComponent reqSrc) =>
            {

                commandBuffer.AddComponent<NetworkStreamInGame>(reqSrc.SourceConnection);
                UnityEngine.Debug.Log(String.Format("Server setting connection {0} to in game", networkIdFromEntity[reqSrc.SourceConnection].Value));

                var player = commandBuffer.Instantiate(prefab);
                commandBuffer.SetComponent<Translation>(player, new Translation { Value = new float3() { x=4,y=4,z=0.23f} });
                commandBuffer.SetComponent(player, new GhostOwnerComponent { NetworkId = networkIdFromEntity[reqSrc.SourceConnection].Value });
                commandBuffer.SetComponent(reqSrc.SourceConnection, new CommandTargetComponent { targetEntity = player });
                commandBuffer.DestroyEntity(reqEnt);
            }).Run();
            commandBuffer.Playback(EntityManager);
        }
    }

}

