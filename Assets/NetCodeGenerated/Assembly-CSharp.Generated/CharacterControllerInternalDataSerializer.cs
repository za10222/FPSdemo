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
    public struct CharacterControllerInternalDataGhostComponentSerializer
    {
        static GhostComponentSerializer.State GetState()
        {
            // This needs to be lazy initialized because otherwise there is a depenency on the static initialization order which breaks il2cpp builds due to TYpeManager not being initialized yet
            if (!s_StateInitialized)
            {
                s_State = new GhostComponentSerializer.State
                {
                    GhostFieldsHash = 12793143981069296801,
                    ExcludeFromComponentCollectionHash = 0,
                    ComponentType = ComponentType.ReadWrite<CharacterControllerInternalData>(),
                    ComponentSize = UnsafeUtility.SizeOf<CharacterControllerInternalData>(),
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
            public float CurrentRotationAngle;
            public int SupportedState;
            public float UnsupportedVelocity_x;
            public float UnsupportedVelocity_y;
            public float UnsupportedVelocity_z;
            public float Velocity_Linear_x;
            public float Velocity_Linear_y;
            public float Velocity_Linear_z;
            public float Velocity_Angular_x;
            public float Velocity_Angular_y;
            public float Velocity_Angular_z;
            public int Entity;
            public uint EntitySpawnTick;
            public uint IsJumping;
        }
        public const int ChangeMaskBits = 7;
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.CopyToFromSnapshotDelegate))]
        private static void CopyToSnapshot(IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, IntPtr componentData, int componentStride, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData, snapshotOffset + snapshotStride*i);
                ref var component = ref GhostComponentSerializer.TypeCast<CharacterControllerInternalData>(componentData, componentStride*i);
                ref var serializerState = ref GhostComponentSerializer.TypeCast<GhostSerializerState>(stateData, 0);
                snapshot.CurrentRotationAngle = component.CurrentRotationAngle;
                snapshot.SupportedState = (int) component.SupportedState;
                snapshot.UnsupportedVelocity_x = component.UnsupportedVelocity.x;
                snapshot.UnsupportedVelocity_y = component.UnsupportedVelocity.y;
                snapshot.UnsupportedVelocity_z = component.UnsupportedVelocity.z;
                snapshot.Velocity_Linear_x = component.Velocity.Linear.x;
                snapshot.Velocity_Linear_y = component.Velocity.Linear.y;
                snapshot.Velocity_Linear_z = component.Velocity.Linear.z;
                snapshot.Velocity_Angular_x = component.Velocity.Angular.x;
                snapshot.Velocity_Angular_y = component.Velocity.Angular.y;
                snapshot.Velocity_Angular_z = component.Velocity.Angular.z;
                snapshot.Entity = 0;
                snapshot.EntitySpawnTick = 0;
                if (serializerState.GhostFromEntity.HasComponent(component.Entity))
                {
                    var ghostComponent = serializerState.GhostFromEntity[component.Entity];
                    snapshot.Entity = ghostComponent.ghostId;
                    snapshot.EntitySpawnTick = ghostComponent.spawnTick;
                }
                snapshot.IsJumping = component.IsJumping?1u:0;
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
                ref var component = ref GhostComponentSerializer.TypeCast<CharacterControllerInternalData>(componentData, componentStride*i);
                component.CurrentRotationAngle = snapshotBefore.CurrentRotationAngle;
                component.SupportedState = (CharacterControllerUtilities.CharacterSupportState) snapshotBefore.SupportedState;
                component.UnsupportedVelocity = new float3(snapshotBefore.UnsupportedVelocity_x, snapshotBefore.UnsupportedVelocity_y, snapshotBefore.UnsupportedVelocity_z);
                component.Velocity.Linear = new float3(snapshotBefore.Velocity_Linear_x, snapshotBefore.Velocity_Linear_y, snapshotBefore.Velocity_Linear_z);
                component.Velocity.Angular = new float3(snapshotBefore.Velocity_Angular_x, snapshotBefore.Velocity_Angular_y, snapshotBefore.Velocity_Angular_z);
                component.Entity = Entity.Null;
                if (snapshotBefore.Entity != 0)
                {
                    if (deserializerState.GhostMap.TryGetValue(new SpawnedGhost{ghostId = snapshotBefore.Entity, spawnTick = snapshotBefore.EntitySpawnTick}, out var ghostEnt))
                        component.Entity = ghostEnt;
                }
                component.IsJumping = snapshotBefore.IsJumping != 0;

            }
        }


        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.RestoreFromBackupDelegate))]
        private static void RestoreFromBackup(IntPtr componentData, IntPtr backupData)
        {
            ref var component = ref GhostComponentSerializer.TypeCast<CharacterControllerInternalData>(componentData, 0);
            ref var backup = ref GhostComponentSerializer.TypeCast<CharacterControllerInternalData>(backupData, 0);
            component.CurrentRotationAngle = backup.CurrentRotationAngle;
            component.SupportedState = backup.SupportedState;
            component.UnsupportedVelocity.x = backup.UnsupportedVelocity.x;
            component.UnsupportedVelocity.y = backup.UnsupportedVelocity.y;
            component.UnsupportedVelocity.z = backup.UnsupportedVelocity.z;
            component.Velocity.Linear.x = backup.Velocity.Linear.x;
            component.Velocity.Linear.y = backup.Velocity.Linear.y;
            component.Velocity.Linear.z = backup.Velocity.Linear.z;
            component.Velocity.Angular.x = backup.Velocity.Angular.x;
            component.Velocity.Angular.y = backup.Velocity.Angular.y;
            component.Velocity.Angular.z = backup.Velocity.Angular.z;
            component.Entity = backup.Entity;
            component.IsJumping = backup.IsJumping;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.PredictDeltaDelegate))]
        private static void PredictDelta(IntPtr snapshotData, IntPtr baseline1Data, IntPtr baseline2Data, ref GhostDeltaPredictor predictor)
        {
            ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData);
            ref var baseline1 = ref GhostComponentSerializer.TypeCast<Snapshot>(baseline1Data);
            ref var baseline2 = ref GhostComponentSerializer.TypeCast<Snapshot>(baseline2Data);
            snapshot.SupportedState = predictor.PredictInt(snapshot.SupportedState, baseline1.SupportedState, baseline2.SupportedState);
            snapshot.Entity = predictor.PredictInt(snapshot.Entity, baseline1.Entity, baseline2.Entity);
            snapshot.EntitySpawnTick = (uint)predictor.PredictInt((int)snapshot.EntitySpawnTick, (int)baseline1.EntitySpawnTick, (int)baseline2.Entity);
            snapshot.IsJumping = (uint)predictor.PredictInt((int)snapshot.IsJumping, (int)baseline1.IsJumping, (int)baseline2.IsJumping);
        }
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.CalculateChangeMaskDelegate))]
        private static void CalculateChangeMask(IntPtr snapshotData, IntPtr baselineData, IntPtr bits, int startOffset)
        {
            ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData);
            ref var baseline = ref GhostComponentSerializer.TypeCast<Snapshot>(baselineData);
            uint changeMask;
            changeMask = (snapshot.CurrentRotationAngle != baseline.CurrentRotationAngle) ? 1u : 0;
            changeMask |= (snapshot.SupportedState != baseline.SupportedState) ? (1u<<1) : 0;
            changeMask |= (snapshot.UnsupportedVelocity_x != baseline.UnsupportedVelocity_x) ? (1u<<2) : 0;
            changeMask |= (snapshot.UnsupportedVelocity_y != baseline.UnsupportedVelocity_y) ? (1u<<2) : 0;
            changeMask |= (snapshot.UnsupportedVelocity_z != baseline.UnsupportedVelocity_z) ? (1u<<2) : 0;
            changeMask |= (snapshot.Velocity_Linear_x != baseline.Velocity_Linear_x) ? (1u<<3) : 0;
            changeMask |= (snapshot.Velocity_Linear_y != baseline.Velocity_Linear_y) ? (1u<<3) : 0;
            changeMask |= (snapshot.Velocity_Linear_z != baseline.Velocity_Linear_z) ? (1u<<3) : 0;
            changeMask |= (snapshot.Velocity_Angular_x != baseline.Velocity_Angular_x) ? (1u<<4) : 0;
            changeMask |= (snapshot.Velocity_Angular_y != baseline.Velocity_Angular_y) ? (1u<<4) : 0;
            changeMask |= (snapshot.Velocity_Angular_z != baseline.Velocity_Angular_z) ? (1u<<4) : 0;
            changeMask |= (snapshot.Entity != baseline.Entity || snapshot.EntitySpawnTick != baseline.EntitySpawnTick) ? (1u<<5) : 0;
            changeMask |= (snapshot.IsJumping != baseline.IsJumping) ? (1u<<6) : 0;
            GhostComponentSerializer.CopyToChangeMask(bits, changeMask, startOffset, 7);
        }
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.SerializeDelegate))]
        private static void Serialize(IntPtr snapshotData, IntPtr baselineData, ref DataStreamWriter writer, ref NetworkCompressionModel compressionModel, IntPtr changeMaskData, int startOffset)
        {
            ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData);
            ref var baseline = ref GhostComponentSerializer.TypeCast<Snapshot>(baselineData);
            uint changeMask = GhostComponentSerializer.CopyFromChangeMask(changeMaskData, startOffset, ChangeMaskBits);
            if ((changeMask & (1 << 0)) != 0)
                writer.WritePackedFloatDelta(snapshot.CurrentRotationAngle, baseline.CurrentRotationAngle, compressionModel);
            if ((changeMask & (1 << 1)) != 0)
                writer.WritePackedIntDelta(snapshot.SupportedState, baseline.SupportedState, compressionModel);
            if ((changeMask & (1 << 2)) != 0)
                writer.WritePackedFloatDelta(snapshot.UnsupportedVelocity_x, baseline.UnsupportedVelocity_x, compressionModel);
            if ((changeMask & (1 << 2)) != 0)
                writer.WritePackedFloatDelta(snapshot.UnsupportedVelocity_y, baseline.UnsupportedVelocity_y, compressionModel);
            if ((changeMask & (1 << 2)) != 0)
                writer.WritePackedFloatDelta(snapshot.UnsupportedVelocity_z, baseline.UnsupportedVelocity_z, compressionModel);
            if ((changeMask & (1 << 3)) != 0)
                writer.WritePackedFloatDelta(snapshot.Velocity_Linear_x, baseline.Velocity_Linear_x, compressionModel);
            if ((changeMask & (1 << 3)) != 0)
                writer.WritePackedFloatDelta(snapshot.Velocity_Linear_y, baseline.Velocity_Linear_y, compressionModel);
            if ((changeMask & (1 << 3)) != 0)
                writer.WritePackedFloatDelta(snapshot.Velocity_Linear_z, baseline.Velocity_Linear_z, compressionModel);
            if ((changeMask & (1 << 4)) != 0)
                writer.WritePackedFloatDelta(snapshot.Velocity_Angular_x, baseline.Velocity_Angular_x, compressionModel);
            if ((changeMask & (1 << 4)) != 0)
                writer.WritePackedFloatDelta(snapshot.Velocity_Angular_y, baseline.Velocity_Angular_y, compressionModel);
            if ((changeMask & (1 << 4)) != 0)
                writer.WritePackedFloatDelta(snapshot.Velocity_Angular_z, baseline.Velocity_Angular_z, compressionModel);
            if ((changeMask & (1 << 5)) != 0)
            {
                writer.WritePackedIntDelta(snapshot.Entity, baseline.Entity, compressionModel);
                writer.WritePackedUIntDelta(snapshot.EntitySpawnTick, baseline.EntitySpawnTick, compressionModel);
            }
            if ((changeMask & (1 << 6)) != 0)
                writer.WritePackedUIntDelta(snapshot.IsJumping, baseline.IsJumping, compressionModel);
        }
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.DeserializeDelegate))]
        private static void Deserialize(IntPtr snapshotData, IntPtr baselineData, ref DataStreamReader reader, ref NetworkCompressionModel compressionModel, IntPtr changeMaskData, int startOffset)
        {
            ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData);
            ref var baseline = ref GhostComponentSerializer.TypeCast<Snapshot>(baselineData);
            uint changeMask = GhostComponentSerializer.CopyFromChangeMask(changeMaskData, startOffset, ChangeMaskBits);
            if ((changeMask & (1 << 0)) != 0)
                snapshot.CurrentRotationAngle = reader.ReadPackedFloatDelta(baseline.CurrentRotationAngle, compressionModel);
            else
                snapshot.CurrentRotationAngle = baseline.CurrentRotationAngle;
            if ((changeMask & (1 << 1)) != 0)
                snapshot.SupportedState = reader.ReadPackedIntDelta(baseline.SupportedState, compressionModel);
            else
                snapshot.SupportedState = baseline.SupportedState;
            if ((changeMask & (1 << 2)) != 0)
                snapshot.UnsupportedVelocity_x = reader.ReadPackedFloatDelta(baseline.UnsupportedVelocity_x, compressionModel);
            else
                snapshot.UnsupportedVelocity_x = baseline.UnsupportedVelocity_x;
            if ((changeMask & (1 << 2)) != 0)
                snapshot.UnsupportedVelocity_y = reader.ReadPackedFloatDelta(baseline.UnsupportedVelocity_y, compressionModel);
            else
                snapshot.UnsupportedVelocity_y = baseline.UnsupportedVelocity_y;
            if ((changeMask & (1 << 2)) != 0)
                snapshot.UnsupportedVelocity_z = reader.ReadPackedFloatDelta(baseline.UnsupportedVelocity_z, compressionModel);
            else
                snapshot.UnsupportedVelocity_z = baseline.UnsupportedVelocity_z;
            if ((changeMask & (1 << 3)) != 0)
                snapshot.Velocity_Linear_x = reader.ReadPackedFloatDelta(baseline.Velocity_Linear_x, compressionModel);
            else
                snapshot.Velocity_Linear_x = baseline.Velocity_Linear_x;
            if ((changeMask & (1 << 3)) != 0)
                snapshot.Velocity_Linear_y = reader.ReadPackedFloatDelta(baseline.Velocity_Linear_y, compressionModel);
            else
                snapshot.Velocity_Linear_y = baseline.Velocity_Linear_y;
            if ((changeMask & (1 << 3)) != 0)
                snapshot.Velocity_Linear_z = reader.ReadPackedFloatDelta(baseline.Velocity_Linear_z, compressionModel);
            else
                snapshot.Velocity_Linear_z = baseline.Velocity_Linear_z;
            if ((changeMask & (1 << 4)) != 0)
                snapshot.Velocity_Angular_x = reader.ReadPackedFloatDelta(baseline.Velocity_Angular_x, compressionModel);
            else
                snapshot.Velocity_Angular_x = baseline.Velocity_Angular_x;
            if ((changeMask & (1 << 4)) != 0)
                snapshot.Velocity_Angular_y = reader.ReadPackedFloatDelta(baseline.Velocity_Angular_y, compressionModel);
            else
                snapshot.Velocity_Angular_y = baseline.Velocity_Angular_y;
            if ((changeMask & (1 << 4)) != 0)
                snapshot.Velocity_Angular_z = reader.ReadPackedFloatDelta(baseline.Velocity_Angular_z, compressionModel);
            else
                snapshot.Velocity_Angular_z = baseline.Velocity_Angular_z;
            if ((changeMask & (1 << 5)) != 0)
            {
                snapshot.Entity = reader.ReadPackedIntDelta(baseline.Entity, compressionModel);
                snapshot.EntitySpawnTick = reader.ReadPackedUIntDelta(baseline.EntitySpawnTick, compressionModel);
            }
            else
            {
                snapshot.Entity = baseline.Entity;
                snapshot.EntitySpawnTick = baseline.EntitySpawnTick;
            }
            if ((changeMask & (1 << 6)) != 0)
                snapshot.IsJumping = reader.ReadPackedUIntDelta(baseline.IsJumping, compressionModel);
            else
                snapshot.IsJumping = baseline.IsJumping;
        }
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.ReportPredictionErrorsDelegate))]
        private static void ReportPredictionErrors(IntPtr componentData, IntPtr backupData, ref UnsafeList<float> errors)
        {
            ref var component = ref GhostComponentSerializer.TypeCast<CharacterControllerInternalData>(componentData, 0);
            ref var backup = ref GhostComponentSerializer.TypeCast<CharacterControllerInternalData>(backupData, 0);
            int errorIndex = 0;
            errors[errorIndex] = math.max(errors[errorIndex], math.abs(component.CurrentRotationAngle - backup.CurrentRotationAngle));
            ++errorIndex;
            errors[errorIndex] = math.max(errors[errorIndex], math.abs(component.SupportedState - backup.SupportedState));
            ++errorIndex;
            errors[errorIndex] = math.max(errors[errorIndex], math.distance(component.UnsupportedVelocity, backup.UnsupportedVelocity));
            ++errorIndex;
            errors[errorIndex] = math.max(errors[errorIndex], math.distance(component.Velocity.Linear, backup.Velocity.Linear));
            ++errorIndex;
            errors[errorIndex] = math.max(errors[errorIndex], math.distance(component.Velocity.Angular, backup.Velocity.Angular));
            ++errorIndex;
            errors[errorIndex] = math.max(errors[errorIndex], (component.IsJumping != backup.IsJumping) ? 1 : 0);
            ++errorIndex;
        }
        private static int GetPredictionErrorNames(ref FixedString512 names)
        {
            int nameCount = 0;
            if (nameCount != 0)
                names.Append(new FixedString32(","));
            names.Append(new FixedString64("CurrentRotationAngle"));
            ++nameCount;
            if (nameCount != 0)
                names.Append(new FixedString32(","));
            names.Append(new FixedString64("SupportedState"));
            ++nameCount;
            if (nameCount != 0)
                names.Append(new FixedString32(","));
            names.Append(new FixedString64("UnsupportedVelocity"));
            ++nameCount;
            if (nameCount != 0)
                names.Append(new FixedString32(","));
            names.Append(new FixedString64("Velocity.Linear"));
            ++nameCount;
            if (nameCount != 0)
                names.Append(new FixedString32(","));
            names.Append(new FixedString64("Velocity.Angular"));
            ++nameCount;
            if (nameCount != 0)
                names.Append(new FixedString32(","));
            names.Append(new FixedString64("IsJumping"));
            ++nameCount;
            return nameCount;
        }
        #endif
    }
}
