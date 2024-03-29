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

namespace Assembly_CSharp.Generated
{
    [BurstCompile]
    public struct FPSdemoGunManager_PlayerGunInternalDataGhostComponentSerializer
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
                    ComponentType = ComponentType.ReadWrite<FPSdemo.GunManager.PlayerGunInternalData>(),
                    ComponentSize = UnsafeUtility.SizeOf<FPSdemo.GunManager.PlayerGunInternalData>(),
                    SnapshotSize = UnsafeUtility.SizeOf<Snapshot>(),
                    ChangeMaskBits = ChangeMaskBits,
                    SendMask = GhostComponentSerializer.SendMask.Predicted,
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
            public uint changeGun;
            public uint shoot;
            public uint lastChangeTick;
        }
        public const int ChangeMaskBits = 3;
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.CopyToFromSnapshotDelegate))]
        private static void CopyToSnapshot(IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, IntPtr componentData, int componentStride, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData, snapshotOffset + snapshotStride*i);
                ref var component = ref GhostComponentSerializer.TypeCast<FPSdemo.GunManager.PlayerGunInternalData>(componentData, componentStride*i);
                ref var serializerState = ref GhostComponentSerializer.TypeCast<GhostSerializerState>(stateData, 0);
                snapshot.changeGun = component.changeGun?1u:0;
                snapshot.shoot = component.shoot?1u:0;
                snapshot.lastChangeTick = (uint)component.lastChangeTick;
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
                ref var component = ref GhostComponentSerializer.TypeCast<FPSdemo.GunManager.PlayerGunInternalData>(componentData, componentStride*i);
                component.changeGun = snapshotBefore.changeGun != 0;
                component.shoot = snapshotBefore.shoot != 0;
                component.lastChangeTick = (uint) snapshotBefore.lastChangeTick;

            }
        }


        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.RestoreFromBackupDelegate))]
        private static void RestoreFromBackup(IntPtr componentData, IntPtr backupData)
        {
            ref var component = ref GhostComponentSerializer.TypeCast<FPSdemo.GunManager.PlayerGunInternalData>(componentData, 0);
            ref var backup = ref GhostComponentSerializer.TypeCast<FPSdemo.GunManager.PlayerGunInternalData>(backupData, 0);
            component.changeGun = backup.changeGun;
            component.shoot = backup.shoot;
            component.lastChangeTick = backup.lastChangeTick;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.PredictDeltaDelegate))]
        private static void PredictDelta(IntPtr snapshotData, IntPtr baseline1Data, IntPtr baseline2Data, ref GhostDeltaPredictor predictor)
        {
            ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData);
            ref var baseline1 = ref GhostComponentSerializer.TypeCast<Snapshot>(baseline1Data);
            ref var baseline2 = ref GhostComponentSerializer.TypeCast<Snapshot>(baseline2Data);
            snapshot.changeGun = (uint)predictor.PredictInt((int)snapshot.changeGun, (int)baseline1.changeGun, (int)baseline2.changeGun);
            snapshot.shoot = (uint)predictor.PredictInt((int)snapshot.shoot, (int)baseline1.shoot, (int)baseline2.shoot);
            snapshot.lastChangeTick = (uint)predictor.PredictInt((int)snapshot.lastChangeTick, (int)baseline1.lastChangeTick, (int)baseline2.lastChangeTick);
        }
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.CalculateChangeMaskDelegate))]
        private static void CalculateChangeMask(IntPtr snapshotData, IntPtr baselineData, IntPtr bits, int startOffset)
        {
            ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData);
            ref var baseline = ref GhostComponentSerializer.TypeCast<Snapshot>(baselineData);
            uint changeMask;
            changeMask = (snapshot.changeGun != baseline.changeGun) ? 1u : 0;
            changeMask |= (snapshot.shoot != baseline.shoot) ? (1u<<1) : 0;
            changeMask |= (snapshot.lastChangeTick != baseline.lastChangeTick) ? (1u<<2) : 0;
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
                writer.WritePackedUIntDelta(snapshot.changeGun, baseline.changeGun, compressionModel);
            if ((changeMask & (1 << 1)) != 0)
                writer.WritePackedUIntDelta(snapshot.shoot, baseline.shoot, compressionModel);
            if ((changeMask & (1 << 2)) != 0)
                writer.WritePackedUIntDelta(snapshot.lastChangeTick, baseline.lastChangeTick, compressionModel);
        }
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.DeserializeDelegate))]
        private static void Deserialize(IntPtr snapshotData, IntPtr baselineData, ref DataStreamReader reader, ref NetworkCompressionModel compressionModel, IntPtr changeMaskData, int startOffset)
        {
            ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData);
            ref var baseline = ref GhostComponentSerializer.TypeCast<Snapshot>(baselineData);
            uint changeMask = GhostComponentSerializer.CopyFromChangeMask(changeMaskData, startOffset, ChangeMaskBits);
            if ((changeMask & (1 << 0)) != 0)
                snapshot.changeGun = reader.ReadPackedUIntDelta(baseline.changeGun, compressionModel);
            else
                snapshot.changeGun = baseline.changeGun;
            if ((changeMask & (1 << 1)) != 0)
                snapshot.shoot = reader.ReadPackedUIntDelta(baseline.shoot, compressionModel);
            else
                snapshot.shoot = baseline.shoot;
            if ((changeMask & (1 << 2)) != 0)
                snapshot.lastChangeTick = reader.ReadPackedUIntDelta(baseline.lastChangeTick, compressionModel);
            else
                snapshot.lastChangeTick = baseline.lastChangeTick;
        }
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.ReportPredictionErrorsDelegate))]
        private static void ReportPredictionErrors(IntPtr componentData, IntPtr backupData, ref UnsafeList<float> errors)
        {
            ref var component = ref GhostComponentSerializer.TypeCast<FPSdemo.GunManager.PlayerGunInternalData>(componentData, 0);
            ref var backup = ref GhostComponentSerializer.TypeCast<FPSdemo.GunManager.PlayerGunInternalData>(backupData, 0);
            int errorIndex = 0;
            errors[errorIndex] = math.max(errors[errorIndex], (component.changeGun != backup.changeGun) ? 1 : 0);
            ++errorIndex;
            errors[errorIndex] = math.max(errors[errorIndex], (component.shoot != backup.shoot) ? 1 : 0);
            ++errorIndex;
            errors[errorIndex] = math.max(errors[errorIndex],
                (component.lastChangeTick > backup.lastChangeTick) ?
                (component.lastChangeTick - backup.lastChangeTick) :
                (backup.lastChangeTick - component.lastChangeTick));
            ++errorIndex;
        }
        private static int GetPredictionErrorNames(ref FixedString512 names)
        {
            int nameCount = 0;
            if (nameCount != 0)
                names.Append(new FixedString32(","));
            names.Append(new FixedString64("changeGun"));
            ++nameCount;
            if (nameCount != 0)
                names.Append(new FixedString32(","));
            names.Append(new FixedString64("shoot"));
            ++nameCount;
            if (nameCount != 0)
                names.Append(new FixedString32(","));
            names.Append(new FixedString64("lastChangeTick"));
            ++nameCount;
            return nameCount;
        }
        #endif
    }
}
