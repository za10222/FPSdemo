//THIS FILE IS AUTOGENERATED BY GHOSTCOMPILER. DON'T MODIFY OR ALTER.
using System;
using AOT;
using Unity.Burst;
using Unity.Networking.Transport;
using Unity.NetCode.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;
using FPSdemo;

namespace Assembly_CSharp.Generated
{
    [BurstCompile]
    public struct FPSdemoEnemyGhostComponentSerializer
    {
        static GhostComponentSerializer.State GetState()
        {
            // This needs to be lazy initialized because otherwise there is a depenency on the static initialization order which breaks il2cpp builds due to TYpeManager not being initialized yet
            if (!s_StateInitialized)
            {
                s_State = new GhostComponentSerializer.State
                {
                    GhostFieldsHash = 11266163746584454747,
                    ExcludeFromComponentCollectionHash = 0,
                    ComponentType = ComponentType.ReadWrite<FPSdemo.Enemy>(),
                    ComponentSize = UnsafeUtility.SizeOf<FPSdemo.Enemy>(),
                    SnapshotSize = UnsafeUtility.SizeOf<Snapshot>(),
                    ChangeMaskBits = ChangeMaskBits,
                    SendMask = GhostComponentSerializer.SendMask.Interpolated | GhostComponentSerializer.SendMask.Predicted,
                    SendToOwner = SendToOwnerType.All,
                    SendForChildEntities = 1,
                    VariantHash = 0,
                    CopyToSnapshot =
                        new PortableFunctionPointer<GhostComponentSerializer.CopyToFromSnapshotDelegate>(CopyToSnapshot),
                    CopyFromSnapshot =
                        new PortableFunctionPointer<GhostComponentSerializer.CopyToFromSnapshotDelegate>(CopyFromSnapshot),
                    RestoreFromBackup =
                        new PortableFunctionPointer<GhostComponentSerializer.RestoreFromBackupDelegate>(RestoreFromBackup),
                    PredictDelta = new PortableFunctionPointer<GhostComponentSerializer.PredictDeltaDelegate>(PredictDelta),
                    CalculateChangeMask =
                        new PortableFunctionPointer<GhostComponentSerializer.CalculateChangeMaskDelegate>(
                            CalculateChangeMask),
                    Serialize = new PortableFunctionPointer<GhostComponentSerializer.SerializeDelegate>(Serialize),
                    Deserialize = new PortableFunctionPointer<GhostComponentSerializer.DeserializeDelegate>(Deserialize),
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    ReportPredictionErrors = new PortableFunctionPointer<GhostComponentSerializer.ReportPredictionErrorsDelegate>(ReportPredictionErrors),
                    #endif
                };
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                s_State.NumPredictionErrorNames = GetPredictionErrorNames(ref s_State.PredictionErrorNames);
                #endif
                s_StateInitialized = true;
            }
            return s_State;
        }
        private static bool s_StateInitialized;
        private static GhostComponentSerializer.State s_State;
        public static GhostComponentSerializer.State State => GetState();
        public struct Snapshot
        {
            public float distance;
            public int state;
            public uint inhit;
        }
        public const int ChangeMaskBits = 3;
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.CopyToFromSnapshotDelegate))]
        private static void CopyToSnapshot(IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, IntPtr componentData, int componentStride, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData, snapshotOffset + snapshotStride*i);
                ref var component = ref GhostComponentSerializer.TypeCast<FPSdemo.Enemy>(componentData, componentStride*i);
                ref var serializerState = ref GhostComponentSerializer.TypeCast<GhostSerializerState>(stateData, 0);
                snapshot.distance = component.distance;
                snapshot.state = (int) component.state;
                snapshot.inhit = component.inhit?1u:0;
            }
        }
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.CopyToFromSnapshotDelegate))]
        private static void CopyFromSnapshot(IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, IntPtr componentData, int componentStride, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                var deserializerState = GhostComponentSerializer.TypeCast<GhostDeserializerState>(stateData, 0);
                ref var snapshotInterpolationData = ref GhostComponentSerializer.TypeCast<SnapshotData.DataAtTick>(snapshotData, snapshotStride*i);
                ref var snapshotBefore = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotInterpolationData.SnapshotBefore, snapshotOffset);
                ref var snapshotAfter = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotInterpolationData.SnapshotAfter, snapshotOffset);
                //Compute the required owner mask for the components and buffers by retrievieng the ghost owner id from the data for the current tick.
                if (snapshotInterpolationData.GhostOwner > 0)
                {
                    var requiredOwnerMask = snapshotInterpolationData.GhostOwner == deserializerState.GhostOwner
                        ? SendToOwnerType.SendToOwner
                        : SendToOwnerType.SendToNonOwner;
                    if ((deserializerState.SendToOwner & requiredOwnerMask) == 0)
                        continue;
                }
                deserializerState.SnapshotTick = snapshotInterpolationData.Tick;
                float snapshotInterpolationFactorRaw = snapshotInterpolationData.InterpolationFactor;
                float snapshotInterpolationFactor = snapshotInterpolationFactorRaw;
                ref var component = ref GhostComponentSerializer.TypeCast<FPSdemo.Enemy>(componentData, componentStride*i);
                component.distance = snapshotBefore.distance;
                component.state = (FPSdemo.Enemy.EnemyState) snapshotBefore.state;
                component.inhit = snapshotBefore.inhit != 0;

            }
        }


        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.RestoreFromBackupDelegate))]
        private static void RestoreFromBackup(IntPtr componentData, IntPtr backupData)
        {
            ref var component = ref GhostComponentSerializer.TypeCast<FPSdemo.Enemy>(componentData, 0);
            ref var backup = ref GhostComponentSerializer.TypeCast<FPSdemo.Enemy>(backupData, 0);
            component.distance = backup.distance;
            component.state = backup.state;
            component.inhit = backup.inhit;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.PredictDeltaDelegate))]
        private static void PredictDelta(IntPtr snapshotData, IntPtr baseline1Data, IntPtr baseline2Data, ref GhostDeltaPredictor predictor)
        {
            ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData);
            ref var baseline1 = ref GhostComponentSerializer.TypeCast<Snapshot>(baseline1Data);
            ref var baseline2 = ref GhostComponentSerializer.TypeCast<Snapshot>(baseline2Data);
            snapshot.state = predictor.PredictInt(snapshot.state, baseline1.state, baseline2.state);
            snapshot.inhit = (uint)predictor.PredictInt((int)snapshot.inhit, (int)baseline1.inhit, (int)baseline2.inhit);
        }
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.CalculateChangeMaskDelegate))]
        private static void CalculateChangeMask(IntPtr snapshotData, IntPtr baselineData, IntPtr bits, int startOffset)
        {
            ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData);
            ref var baseline = ref GhostComponentSerializer.TypeCast<Snapshot>(baselineData);
            uint changeMask;
            changeMask = (snapshot.distance != baseline.distance) ? 1u : 0;
            changeMask |= (snapshot.state != baseline.state) ? (1u<<1) : 0;
            changeMask |= (snapshot.inhit != baseline.inhit) ? (1u<<2) : 0;
            GhostComponentSerializer.CopyToChangeMask(bits, changeMask, startOffset, 3);
        }
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.SerializeDelegate))]
        private static void Serialize(IntPtr snapshotData, IntPtr baselineData, ref DataStreamWriter writer, ref NetworkCompressionModel compressionModel, IntPtr changeMaskData, int startOffset)
        {
            ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData);
            ref var baseline = ref GhostComponentSerializer.TypeCast<Snapshot>(baselineData);
            uint changeMask = GhostComponentSerializer.CopyFromChangeMask(changeMaskData, startOffset, ChangeMaskBits);
            if ((changeMask & (1 << 0)) != 0)
                writer.WritePackedFloatDelta(snapshot.distance, baseline.distance, compressionModel);
            if ((changeMask & (1 << 1)) != 0)
                writer.WritePackedIntDelta(snapshot.state, baseline.state, compressionModel);
            if ((changeMask & (1 << 2)) != 0)
                writer.WritePackedUIntDelta(snapshot.inhit, baseline.inhit, compressionModel);
        }
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.DeserializeDelegate))]
        private static void Deserialize(IntPtr snapshotData, IntPtr baselineData, ref DataStreamReader reader, ref NetworkCompressionModel compressionModel, IntPtr changeMaskData, int startOffset)
        {
            ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData);
            ref var baseline = ref GhostComponentSerializer.TypeCast<Snapshot>(baselineData);
            uint changeMask = GhostComponentSerializer.CopyFromChangeMask(changeMaskData, startOffset, ChangeMaskBits);
            if ((changeMask & (1 << 0)) != 0)
                snapshot.distance = reader.ReadPackedFloatDelta(baseline.distance, compressionModel);
            else
                snapshot.distance = baseline.distance;
            if ((changeMask & (1 << 1)) != 0)
                snapshot.state = reader.ReadPackedIntDelta(baseline.state, compressionModel);
            else
                snapshot.state = baseline.state;
            if ((changeMask & (1 << 2)) != 0)
                snapshot.inhit = reader.ReadPackedUIntDelta(baseline.inhit, compressionModel);
            else
                snapshot.inhit = baseline.inhit;
        }
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.ReportPredictionErrorsDelegate))]
        private static void ReportPredictionErrors(IntPtr componentData, IntPtr backupData, ref UnsafeList<float> errors)
        {
            ref var component = ref GhostComponentSerializer.TypeCast<FPSdemo.Enemy>(componentData, 0);
            ref var backup = ref GhostComponentSerializer.TypeCast<FPSdemo.Enemy>(backupData, 0);
            int errorIndex = 0;
            errors[errorIndex] = math.max(errors[errorIndex], math.abs(component.distance - backup.distance));
            ++errorIndex;
            errors[errorIndex] = math.max(errors[errorIndex], math.abs(component.state - backup.state));
            ++errorIndex;
            errors[errorIndex] = math.max(errors[errorIndex], (component.inhit != backup.inhit) ? 1 : 0);
            ++errorIndex;
        }
        private static int GetPredictionErrorNames(ref FixedString512 names)
        {
            int nameCount = 0;
            if (nameCount != 0)
                names.Append(new FixedString32(","));
            names.Append(new FixedString64("distance"));
            ++nameCount;
            if (nameCount != 0)
                names.Append(new FixedString32(","));
            names.Append(new FixedString64("state"));
            ++nameCount;
            if (nameCount != 0)
                names.Append(new FixedString32(","));
            names.Append(new FixedString64("inhit"));
            ++nameCount;
            return nameCount;
        }
        #endif
    }
}
