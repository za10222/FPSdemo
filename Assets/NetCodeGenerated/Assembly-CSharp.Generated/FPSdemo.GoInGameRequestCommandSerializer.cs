//THIS FILE IS AUTOGENERATED BY GHOSTCOMPILER. DON'T MODIFY OR ALTER.
using AOT;
using Unity.Burst;
using Unity.Networking.Transport;
using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;
using FPSdemo;


namespace Assembly_CSharp.Generated
{
    [BurstCompile]
    public struct FPSdemoGoInGameRequestSerializer : IComponentData, IRpcCommandSerializer<FPSdemo.GoInGameRequest>
    {
        public void Serialize(ref DataStreamWriter writer, in RpcSerializerState state, in FPSdemo.GoInGameRequest data)
        {
        }

        public void Deserialize(ref DataStreamReader reader, in RpcDeserializerState state,  ref FPSdemo.GoInGameRequest data)
        {
        }
        [BurstCompile]
        [MonoPInvokeCallback(typeof(RpcExecutor.ExecuteDelegate))]
        private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
        {
            RpcExecutor.ExecuteCreateRequestComponent<FPSdemoGoInGameRequestSerializer, FPSdemo.GoInGameRequest>(ref parameters);
        }

        static PortableFunctionPointer<RpcExecutor.ExecuteDelegate> InvokeExecuteFunctionPointer =
            new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);
        public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
        {
            return InvokeExecuteFunctionPointer;
        }
    }
    class FPSdemoGoInGameRequestRpcCommandRequestSystem : RpcCommandRequestSystem<FPSdemoGoInGameRequestSerializer, FPSdemo.GoInGameRequest>
    {
        [BurstCompile]
        protected struct SendRpc : IJobEntityBatch
        {
            public SendRpcData data;
            public void Execute(ArchetypeChunk chunk, int orderIndex)
            {
                data.Execute(chunk, orderIndex);
            }
        }
        protected override void OnUpdate()
        {
            var sendJob = new SendRpc{data = InitJobData()};
            ScheduleJobData(sendJob);
        }
    }
}