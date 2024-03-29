using FPSdemo;
using System;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.GraphicsIntegration;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using static CharacterControllerUtilities;
using static Unity.Physics.PhysicsStep;
using Math = Unity.Physics.Math;



[Serializable]
public struct CharacterControllerComponentData : IComponentData
{
    [GhostField]
    public float3 Gravity;
    [GhostField]
    public float MovementSpeed;

    [GhostField]
    public float MaxMovementSpeed;
    [GhostField]
    public float RotationSpeed;
    [GhostField]
    public float JumpUpwardsSpeed;
    [GhostField]
    public float MaxSlope; // radians
    [GhostField]
    public int MaxIterations;
    [GhostField]
    public float CharacterMass;
    [GhostField]
    public float SkinWidth;
    [GhostField]
    public float ContactTolerance;
    [GhostField]
    public byte AffectsPhysicsBodies;
    [GhostField]
    public byte RaiseCollisionEvents;
    [GhostField]
    public byte RaiseTriggerEvents;
}



[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct CharacterControllerInput : IComponentData
{
    public bool hasinput;
    public FPSdemo.UserCommand Commond;
    //public float2 Movement;
    //public float2 Looking;
    //public int Jumped;
}


[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
[WriteGroup(typeof(PhysicsGraphicalInterpolationBuffer))]
[WriteGroup(typeof(PhysicsGraphicalSmoothing))]
public struct CharacterControllerInternalData : IComponentData
{
    [GhostField]
    public float CurrentRotationAngle;
    [GhostField]
    public CharacterSupportState SupportedState;
    [GhostField]
    public float3 UnsupportedVelocity;
    [GhostField]
    public PhysicsVelocity Velocity;
    [GhostField]
    public Entity Entity;
    [GhostField]
    public bool IsJumping;

    [GhostField]
    public float3 addVelocity;


    public double starttime;

    public CharacterControllerInput Input;
}

[Serializable]
public class CharacterControllerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    // Gravity force applied to the character controller body
    public float3 Gravity = Default.Gravity;

    // Speed of movement initiated by user input
    public float MovementSpeed = 2.5f;

    // Maximum speed of movement at any given time
    public float MaxMovementSpeed = 10.0f;

    // Speed of rotation initiated by user input
    public float RotationSpeed = 2.5f;

    // Speed of upwards jump initiated by user input
    public float JumpUpwardsSpeed = 5.0f;

    // Maximum slope angle character can overcome (in degrees)
    public float MaxSlope = 60.0f;

    // Maximum number of character controller solver iterations
    public int MaxIterations = 10;

    // Mass of the character (used for affecting other rigid bodies)
    public float CharacterMass = 1.0f;

    // Keep the character at this distance to planes (used for numerical stability)
    public float SkinWidth = 0.02f;

    // Anything in this distance to the character will be considered a potential contact
    // when checking support
    public float ContactTolerance = 0.1f;

    // Whether to affect other rigid bodies
    public bool AffectsPhysicsBodies = true;

    // Whether to raise collision events
    // Note: collision events raised by character controller will always have details calculated
    public bool RaiseCollisionEvents = false;

    // Whether to raise trigger events
    public bool RaiseTriggerEvents = false;

    void OnEnable() {}

    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (enabled)
        {
            var componentData = new CharacterControllerComponentData
            {
                Gravity = Gravity,
                MovementSpeed = MovementSpeed,
                MaxMovementSpeed = MaxMovementSpeed,
                RotationSpeed = RotationSpeed,
                JumpUpwardsSpeed = JumpUpwardsSpeed,
                MaxSlope = math.radians(MaxSlope),
                MaxIterations = MaxIterations,
                CharacterMass = CharacterMass,
                SkinWidth = SkinWidth,
                ContactTolerance = ContactTolerance,
                AffectsPhysicsBodies = (byte)(AffectsPhysicsBodies ? 1 : 0),
                RaiseCollisionEvents = (byte)(RaiseCollisionEvents ? 1 : 0),
                RaiseTriggerEvents = (byte)(RaiseTriggerEvents ? 1 : 0)
            };
            var internalData = new CharacterControllerInternalData
            {
                Entity = entity,
                Input = new CharacterControllerInput(),
            };

            dstManager.AddComponentData(entity, componentData);
            dstManager.AddComponentData(entity, internalData);
            dstManager.AddComponentData(entity, new Point { point =0});
            if (RaiseCollisionEvents)
            {
                dstManager.AddBuffer<StatefulCollisionEvent>(entity);
            }
            if (RaiseTriggerEvents)
            {
                dstManager.AddBuffer<StatefulTriggerEvent>(entity);
                dstManager.AddComponentData(entity, new ExcludeFromTriggerEventConversion {});
            }
        }
    }
}


[DisableAutoCreation]
// override the behavior of BufferInterpolatedRigidBodiesMotion
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(BuildPhysicsWorld)), UpdateBefore(typeof(ExportPhysicsWorld))]
[UpdateAfter(typeof(BufferInterpolatedRigidBodiesMotion))]
public class BufferInterpolatedCharacterControllerMotion : SystemBase
{
    CharacterControllerSystem m_CharacterControllerSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_CharacterControllerSystem = World.GetOrCreateSystem<CharacterControllerSystem>();
    }

    protected override void OnUpdate()
    {
        Dependency = Entities
            .WithName("UpdateCCInterpolationBuffers")
            .WithNone<PhysicsExclude>()
            .WithBurst()
            .ForEach((ref PhysicsGraphicalInterpolationBuffer interpolationBuffer, in CharacterControllerInternalData ccInternalData, in Translation position, in Rotation orientation) =>
            {
                interpolationBuffer = new PhysicsGraphicalInterpolationBuffer
                {
                    PreviousTransform = new RigidTransform(orientation.Value, position.Value),
                    PreviousVelocity = ccInternalData.Velocity,
                };
            }).ScheduleParallel(Dependency);

        m_CharacterControllerSystem.AddInputDependency(Dependency);
    }
}

[DisableAutoCreation]
[UpdateInGroup(typeof(GhostPredictionSystemGroup))]
[UpdateAfter(typeof(UpdateCharacterControllerInternalDataSystem))]
public class CharacterControllerSystem : SystemBase
{
    const float k_DefaultTau = 0.4f;
    const float k_DefaultDamping = 0.9f;

    JobHandle m_InputDependency;
    public JobHandle OutDependency => Dependency;

    public void AddInputDependency(JobHandle inputDep) =>
        m_InputDependency = JobHandle.CombineDependencies(m_InputDependency, inputDep);

    [BurstCompile]
    struct CharacterControllerJob : IJobChunk
    {
        public float DeltaTime;

        [ReadOnly]
        public PhysicsWorld PhysicsWorld;

        public ComponentTypeHandle<CharacterControllerInternalData> CharacterControllerInternalType;
        public ComponentTypeHandle<Translation> TranslationType;
        public ComponentTypeHandle<Rotation> RotationType;
        public BufferTypeHandle<StatefulCollisionEvent> CollisionEventBufferType;
        public BufferTypeHandle<StatefulTriggerEvent> TriggerEventBufferType;
        [ReadOnly] public ComponentTypeHandle<CharacterControllerComponentData> CharacterControllerComponentType;
        [ReadOnly] public ComponentTypeHandle<PhysicsCollider> PhysicsColliderType;

        // Stores impulses we wish to apply to dynamic bodies the character is interacting with.
        // This is needed to avoid race conditions when 2 characters are interacting with the
        // same body at the same time.
        [NativeDisableParallelForRestriction] public NativeStream.Writer DeferredImpulseWriter;

        public unsafe void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {

            var chunkCCData = chunk.GetNativeArray(CharacterControllerComponentType);
            var chunkCCInternalData = chunk.GetNativeArray(CharacterControllerInternalType);
            var chunkPhysicsColliderData = chunk.GetNativeArray(PhysicsColliderType);
            var chunkTranslationData = chunk.GetNativeArray(TranslationType);
            var chunkRotationData = chunk.GetNativeArray(RotationType);

            var hasChunkCollisionEventBufferType = chunk.Has(CollisionEventBufferType);
            var hasChunkTriggerEventBufferType = chunk.Has(TriggerEventBufferType);

            BufferAccessor<StatefulCollisionEvent> collisionEventBuffers = default;
            BufferAccessor<StatefulTriggerEvent> triggerEventBuffers = default;

            if (hasChunkCollisionEventBufferType)
            {
                collisionEventBuffers = chunk.GetBufferAccessor(CollisionEventBufferType);
            }
            if (hasChunkTriggerEventBufferType)
            {
                triggerEventBuffers = chunk.GetBufferAccessor(TriggerEventBufferType);
            }

            DeferredImpulseWriter.BeginForEachIndex(chunkIndex);

            for (int i = 0; i < chunk.Count; i++)
            {
                var ccComponentData = chunkCCData[i];
                var ccInternalData = chunkCCInternalData[i];
                var collider = chunkPhysicsColliderData[i];
                var position = chunkTranslationData[i];
                var rotation = chunkRotationData[i];
                DynamicBuffer<StatefulCollisionEvent> collisionEventBuffer = default;
                DynamicBuffer<StatefulTriggerEvent> triggerEventBuffer = default;

                if (hasChunkCollisionEventBufferType)
                {
                    collisionEventBuffer = collisionEventBuffers[i];
                }

                if (hasChunkTriggerEventBufferType)
                {
                    triggerEventBuffer = triggerEventBuffers[i];
                }

                if (ccInternalData.Input.hasinput == false)
                    return;
                // Collision filter must be valid
                if (!collider.IsValid || collider.Value.Value.Filter.IsEmpty)
                    continue;

                var up = math.select(math.up(), -math.normalize(ccComponentData.Gravity),
                    math.lengthsq(ccComponentData.Gravity) > 0f);

                // Character step input
                CharacterControllerStepInput stepInput = new CharacterControllerStepInput
                {
                    World = PhysicsWorld,
                    DeltaTime = DeltaTime,
                    Up = up,
                    Gravity = ccComponentData.Gravity,
                    MaxIterations = ccComponentData.MaxIterations,
                    Tau = k_DefaultTau,
                    Damping = k_DefaultDamping,
                    SkinWidth = ccComponentData.SkinWidth,
                    ContactTolerance = ccComponentData.ContactTolerance,
                    MaxSlope = ccComponentData.MaxSlope,
                    RigidBodyIndex = PhysicsWorld.GetRigidBodyIndex(ccInternalData.Entity),
                    CurrentVelocity = ccInternalData.Velocity.Linear,
                    MaxMovementSpeed = ccComponentData.MaxMovementSpeed
                };

                // Character transform
                RigidTransform transform = new RigidTransform
                {
                    pos = position.Value,
                    rot = rotation.Value
                };

                NativeList<StatefulCollisionEvent> currentFrameCollisionEvents = default;
                NativeList<StatefulTriggerEvent> currentFrameTriggerEvents = default;

                if (ccComponentData.RaiseCollisionEvents != 0)
                {
                    currentFrameCollisionEvents = new NativeList<StatefulCollisionEvent>(Allocator.Temp);
                }

                if (ccComponentData.RaiseTriggerEvents != 0)
                {
                    currentFrameTriggerEvents = new NativeList<StatefulTriggerEvent>(Allocator.Temp);
                }
                // Check support
                CheckSupport(ref PhysicsWorld, ref collider, stepInput, transform,
                    out ccInternalData.SupportedState, out float3 surfaceNormal, out float3 surfaceVelocity,
                    currentFrameCollisionEvents);


                // User input
                float3 desiredVelocity = ccInternalData.Velocity.Linear;
                HandleUserInput(ccComponentData, stepInput.Up, surfaceVelocity, ref ccInternalData, ref desiredVelocity);
                // Calculate actual velocity with respect to surface
                if (ccInternalData.SupportedState == CharacterSupportState.Supported)
                {
                    CalculateMovement(ccInternalData.CurrentRotationAngle, stepInput.Up, ccInternalData.IsJumping,
                        ccInternalData.Velocity.Linear, desiredVelocity, surfaceNormal, surfaceVelocity, out ccInternalData.Velocity.Linear);
                }
                else
                {
                    ccInternalData.Velocity.Linear = desiredVelocity;
                }

                // World collision + integrate
                CollideAndIntegrate(stepInput, ccComponentData.CharacterMass, ccComponentData.AffectsPhysicsBodies != 0,
                    collider.ColliderPtr, ref transform, ref ccInternalData.Velocity.Linear, ref DeferredImpulseWriter,
                    currentFrameCollisionEvents, currentFrameTriggerEvents);

                // Update collision event status
                if (currentFrameCollisionEvents.IsCreated)
                {
                    UpdateCollisionEvents(currentFrameCollisionEvents, collisionEventBuffer);
                }

                if (currentFrameTriggerEvents.IsCreated)
                {
                    UpdateTriggerEvents(currentFrameTriggerEvents, triggerEventBuffer);
                }

                // Write back and orientation integration
                position.Value = transform.pos;
                rotation.Value = quaternion.AxisAngle(up, ccInternalData.CurrentRotationAngle);

                // Write back to chunk data
                {
                    chunkCCInternalData[i] = ccInternalData;
                    chunkTranslationData[i] = position;
                    chunkRotationData[i] = rotation;
                }
            }

            DeferredImpulseWriter.EndForEachIndex();
        }

        private void HandleUserInput(CharacterControllerComponentData ccComponentData, float3 up, float3 surfaceVelocity,
            ref CharacterControllerInternalData ccInternalData, ref float3 linearVelocity)
        {

            // Reset jumping state and unsupported velocity
            if (ccInternalData.SupportedState == CharacterSupportState.Supported)
            {
                ccInternalData.IsJumping = false;
                ccInternalData.UnsupportedVelocity = float3.zero;
            }

            // Movement and jumping
            bool shouldJump = false;
            float3 requestedMovementDirection = float3.zero;
            {
                float3 forward = math.forward(quaternion.identity);
                float3 right = math.cross(up, forward);

                float horizontal = ccInternalData.Input.Commond.Movement.x;
                float vertical = ccInternalData.Input.Commond.Movement.y;
                bool jumpRequested = ccInternalData.Input.Commond.buttons.IsSet(FPSdemo.UserCommand.Button.Jump);
                ccInternalData.Input.Commond.buttons.Set(FPSdemo.UserCommand.Button.Jump, false); // "consume" the event
                bool haveInput = (math.abs(horizontal) > float.Epsilon) || (math.abs(vertical) > float.Epsilon);
                if (haveInput && ccInternalData.Input.hasinput)
                {
                    float3 localSpaceMovement = forward * vertical + right * horizontal;
                    float3 worldSpaceMovement = math.rotate(quaternion.AxisAngle(up, ccInternalData.CurrentRotationAngle), localSpaceMovement);
                    requestedMovementDirection = math.normalize(worldSpaceMovement);
                }
                shouldJump = jumpRequested && ccInternalData.SupportedState == CharacterSupportState.Supported;
            }

            // Turning
            {
                float horizontal = ccInternalData.Input.Commond.Looking.x;
                bool haveInput = (math.abs(horizontal) > float.Epsilon);
                if (haveInput && ccInternalData.Input.hasinput)
                {
                    //ccInternalData.Velocity.Angular = -userRotationSpeed * up;
                    ccInternalData.Velocity.Angular = 0f;
                    ccInternalData.CurrentRotationAngle = math.radians(ccInternalData.Input.Commond.Looking.x);
                    //ccInternalData.CurrentRotationAngle %= (math.PI * 2);
                    //Debug.Log("CurrentRotationAngle=" + ccInternalData.CurrentRotationAngle);
                }
                else
                {
                    ccInternalData.Velocity.Angular = 0f;
                }
            }


            // Apply input velocities
            {
                if (shouldJump)
                {
                    // Add jump speed to surface velocity and make character unsupported
                    ccInternalData.IsJumping = true;
                    ccInternalData.SupportedState = CharacterSupportState.Unsupported;
                    ccInternalData.UnsupportedVelocity = surfaceVelocity + ccComponentData.JumpUpwardsSpeed * up;
                }
                else if (ccInternalData.SupportedState != CharacterSupportState.Supported)
                {
                    // Apply gravity
                    ccInternalData.UnsupportedVelocity += ccComponentData.Gravity * DeltaTime;
                }
                // If unsupported then keep jump and surface momentum
                linearVelocity = requestedMovementDirection * ccComponentData.MovementSpeed + ccInternalData.addVelocity +
                    (ccInternalData.SupportedState != CharacterSupportState.Supported ? ccInternalData.UnsupportedVelocity : float3.zero);
            }
            if (ccInternalData.starttime>0)
            {
                ccInternalData.starttime -= DeltaTime;
                if (ccInternalData.starttime <= 0)
                {
                    ccInternalData.addVelocity = 0;
                }
            }
            
            //ccInternalData.addVelocity = 0;
            //ccInternalData.Input.hasinput = false;
        }

        private void CalculateMovement(float currentRotationAngle, float3 up, bool isJumping,
            float3 currentVelocity, float3 desiredVelocity, float3 surfaceNormal, float3 surfaceVelocity, out float3 linearVelocity)
        {
            float3 forward = math.forward(quaternion.AxisAngle(up, currentRotationAngle));

            Rotation surfaceFrame;
            float3 binorm;
            {
                binorm = math.cross(forward, up);
                binorm = math.normalize(binorm);

                float3 tangent = math.cross(binorm, surfaceNormal);
                tangent = math.normalize(tangent);

                binorm = math.cross(tangent, surfaceNormal);
                binorm = math.normalize(binorm);

                surfaceFrame.Value = new quaternion(new float3x3(binorm, tangent, surfaceNormal));
            }

            float3 relative = currentVelocity - surfaceVelocity;
            relative = math.rotate(math.inverse(surfaceFrame.Value), relative);
            float3 diff;
            {
                float3 sideVec = math.cross(forward, up);
                float fwd = math.dot(desiredVelocity, forward);
                float side = math.dot(desiredVelocity, sideVec);
                float len = math.length(desiredVelocity);
                float3 desiredVelocitySF = new float3(-side, -fwd, 0.0f);
                desiredVelocitySF = math.normalizesafe(desiredVelocitySF, float3.zero);
                desiredVelocitySF *= len;
                diff = desiredVelocitySF - relative;
            }
            relative += diff;

            linearVelocity = math.rotate(surfaceFrame.Value, relative) + surfaceVelocity +
                (isJumping ? math.dot(desiredVelocity, up) * up : float3.zero);
        }

        private void UpdateTriggerEvents(NativeList<StatefulTriggerEvent> triggerEvents,
            DynamicBuffer<StatefulTriggerEvent> triggerEventBuffer)
        {
            triggerEvents.Sort();

            var previousFrameTriggerEvents = new NativeList<StatefulTriggerEvent>(triggerEventBuffer.Length, Allocator.Temp);

            for (int i = 0; i < triggerEventBuffer.Length; i++)
            {
                var triggerEvent = triggerEventBuffer[i];
                if (triggerEvent.State != EventOverlapState.Exit)
                {
                    previousFrameTriggerEvents.Add(triggerEvent);
                }
            }

            var eventsWithState = new NativeList<StatefulTriggerEvent>(triggerEvents.Length, Allocator.Temp);

            TriggerEventConversionSystem.UpdateTriggerEventState(previousFrameTriggerEvents, triggerEvents, eventsWithState);

            triggerEventBuffer.Clear();

            for (int i = 0; i < eventsWithState.Length; i++)
            {
                triggerEventBuffer.Add(eventsWithState[i]);
            }
        }

        private void UpdateCollisionEvents(NativeList<StatefulCollisionEvent> collisionEvents,
            DynamicBuffer<StatefulCollisionEvent> collisionEventBuffer)
        {
            collisionEvents.Sort();

            var previousFrameCollisionEvents = new NativeList<StatefulCollisionEvent>(collisionEventBuffer.Length, Allocator.Temp);

            for (int i = 0; i < collisionEventBuffer.Length; i++)
            {
                var collisionEvent = collisionEventBuffer[i];
                if (collisionEvent.CollidingState != EventCollidingState.Exit)
                {
                    previousFrameCollisionEvents.Add(collisionEvent);
                }
            }

            var eventsWithState = new NativeList<StatefulCollisionEvent>(collisionEvents.Length, Allocator.Temp);

            CollisionEventConversionSystem.UpdateCollisionEventState(previousFrameCollisionEvents, collisionEvents, eventsWithState);

            collisionEventBuffer.Clear();

            for (int i = 0; i < eventsWithState.Length; i++)
            {
                collisionEventBuffer.Add(eventsWithState[i]);
            }
        }
    }


    [BurstCompile]
    struct ApplyDefferedPhysicsUpdatesJob : IJob
    {
        // Chunks can be deallocated at this point
        [DeallocateOnJobCompletion] public NativeArray<ArchetypeChunk> Chunks;

        public NativeStream.Reader DeferredImpulseReader;

        public ComponentDataFromEntity<PhysicsVelocity> PhysicsVelocityData;
        public ComponentDataFromEntity<PhysicsMass> PhysicsMassData;
        public ComponentDataFromEntity<Translation> TranslationData;
        public ComponentDataFromEntity<Rotation> RotationData;

        public void Execute()
        {
            int index = 0;
            int maxIndex = DeferredImpulseReader.ForEachCount;
            DeferredImpulseReader.BeginForEachIndex(index++);
            while (DeferredImpulseReader.RemainingItemCount == 0 && index < maxIndex)
            {
                DeferredImpulseReader.BeginForEachIndex(index++);
            }

            while (DeferredImpulseReader.RemainingItemCount > 0)
            {
                // Read the data
                var impulse = DeferredImpulseReader.Read<DeferredCharacterControllerImpulse>();
                while (DeferredImpulseReader.RemainingItemCount == 0 && index < maxIndex)
                {
                    DeferredImpulseReader.BeginForEachIndex(index++);
                }

                PhysicsVelocity pv = PhysicsVelocityData[impulse.Entity];
                PhysicsMass pm = PhysicsMassData[impulse.Entity];
                Translation t = TranslationData[impulse.Entity];
                Rotation r = RotationData[impulse.Entity];

                // Don't apply on kinematic bodies
                if (pm.InverseMass > 0.0f)
                {
                    // Apply impulse
                    pv.ApplyImpulse(pm, t, r, impulse.Impulse, impulse.Point);

                    // Write back
                    PhysicsVelocityData[impulse.Entity] = pv;
                }
            }
        }
    }

    // override the behavior of CopyPhysicsVelocityToSmoothing
    [BurstCompile]
    struct CopyVelocityToGraphicalSmoothingJob : IJobChunk
    {
        [ReadOnly] public ComponentTypeHandle<CharacterControllerInternalData> CharacterControllerInternalType;
        public ComponentTypeHandle<PhysicsGraphicalSmoothing> PhysicsGraphicalSmoothingType;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            NativeArray<CharacterControllerInternalData> ccInternalDatas = chunk.GetNativeArray(CharacterControllerInternalType);
            NativeArray<PhysicsGraphicalSmoothing> physicsGraphicalSmoothings = chunk.GetNativeArray(PhysicsGraphicalSmoothingType);

            for (int i = 0, count = chunk.Count; i < count; ++i)
            {
                var smoothing = physicsGraphicalSmoothings[i];
                smoothing.CurrentVelocity = ccInternalDatas[i].Velocity;
                physicsGraphicalSmoothings[i] = smoothing;
            }
        }
    }

    BuildPhysicsWorld m_BuildPhysicsWorldSystem;
    ExportPhysicsWorld m_ExportPhysicsWorldSystem;
    EndFramePhysicsSystem m_EndFramePhysicsSystem;

    EntityQuery m_CharacterControllersGroup;
    EntityQuery m_SmoothedCharacterControllersGroup;

    protected override void OnCreate()
    {
        m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_ExportPhysicsWorldSystem = World.GetOrCreateSystem<ExportPhysicsWorld>();
        m_EndFramePhysicsSystem = World.GetOrCreateSystem<EndFramePhysicsSystem>();

        EntityQueryDesc query = new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(CharacterControllerComponentData),
                typeof(CharacterControllerInternalData),
                typeof(PhysicsCollider),
                typeof(Translation),
                typeof(Rotation)
            }
        };
        m_CharacterControllersGroup = GetEntityQuery(query);
        m_SmoothedCharacterControllersGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(CharacterControllerInternalData),
                typeof(PhysicsGraphicalSmoothing)
            }
        });
    }

    protected override void OnUpdate()
    {
        if (m_CharacterControllersGroup.CalculateEntityCount() == 0)
            return;
    
        // Combine implicit input dependency with the user one
        Dependency = JobHandle.CombineDependencies(Dependency, m_InputDependency);

        var chunks = m_CharacterControllersGroup.CreateArchetypeChunkArray(Allocator.TempJob);

        var ccComponentType = GetComponentTypeHandle<CharacterControllerComponentData>();
        var ccInternalType = GetComponentTypeHandle<CharacterControllerInternalData>();
        var physicsColliderType = GetComponentTypeHandle<PhysicsCollider>();
        var translationType = GetComponentTypeHandle<Translation>();
        var rotationType = GetComponentTypeHandle<Rotation>();
        var collisionEventBufferType = GetBufferTypeHandle<StatefulCollisionEvent>();
        var triggerEventBufferType = GetBufferTypeHandle<StatefulTriggerEvent>();

        var deferredImpulses = new NativeStream(chunks.Length, Allocator.TempJob);

        var ccJob = new CharacterControllerJob
        {
            // Archetypes
            CharacterControllerComponentType = ccComponentType,
            CharacterControllerInternalType = ccInternalType,
            PhysicsColliderType = physicsColliderType,
            TranslationType = translationType,
            RotationType = rotationType,
            CollisionEventBufferType = collisionEventBufferType,
            TriggerEventBufferType = triggerEventBufferType,

            // Input
            DeltaTime = Time.DeltaTime,
            PhysicsWorld = m_BuildPhysicsWorldSystem.PhysicsWorld,
            DeferredImpulseWriter = deferredImpulses.AsWriter()
        };

        //Dependency = JobHandle.CombineDependencies(Dependency, m_ExportPhysicsWorldSystem.GetOutputDependency());
        Dependency = ccJob.Schedule(m_CharacterControllersGroup, Dependency);
        Dependency.Complete();
        var copyVelocitiesHandle = new CopyVelocityToGraphicalSmoothingJob
        {
            CharacterControllerInternalType = GetComponentTypeHandle<CharacterControllerInternalData>(true),
            PhysicsGraphicalSmoothingType = GetComponentTypeHandle<PhysicsGraphicalSmoothing>()
        }.ScheduleParallel(m_SmoothedCharacterControllersGroup, Dependency);

        var applyJob = new ApplyDefferedPhysicsUpdatesJob()
        {
            Chunks = chunks,
            DeferredImpulseReader = deferredImpulses.AsReader(),
            PhysicsVelocityData = GetComponentDataFromEntity<PhysicsVelocity>(),
            PhysicsMassData = GetComponentDataFromEntity<PhysicsMass>(),
            TranslationData = GetComponentDataFromEntity<Translation>(),
            RotationData = GetComponentDataFromEntity<Rotation>()
        };

        Dependency = applyJob.Schedule(Dependency);

        Dependency = JobHandle.CombineDependencies(Dependency, copyVelocitiesHandle);

        var disposeHandle = deferredImpulses.Dispose(Dependency);

        // Must finish all jobs before physics step end
        //m_EndFramePhysicsSystem.AddInputDependency(disposeHandle);

        // Invalidate input dependency since it's been used by now
        m_InputDependency = default;
    }
}
