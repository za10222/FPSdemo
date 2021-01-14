using System;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
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

namespace Unity.Physics.Stateful
{
    // Describes the overlap state.
    // OverlapState in StatefulTriggerEvent is set to:
    //    1) EventOverlapState.Enter, when 2 bodies are overlapping in the current frame,
    //    but they did not overlap in the previous frame
    //    2) EventOverlapState.Stay, when 2 bodies are overlapping in the current frame,
    //    and they did overlap in the previous frame
    //    3) EventOverlapState.Exit, when 2 bodies are NOT overlapping in the current frame,
    //    but they did overlap in the previous frame
    public enum EventOverlapState : byte
    {
        Enter,
        Stay,
        Exit
    }

    // Trigger Event that is stored inside a DynamicBuffer
    public struct StatefulTriggerEvent : IBufferElementData, IComparable<StatefulTriggerEvent>
    {
        internal EntityPair Entities;
        internal BodyIndexPair BodyIndices;
        internal ColliderKeyPair ColliderKeys;

        public EventOverlapState State;
        public Entity EntityA => Entities.EntityA;
        public Entity EntityB => Entities.EntityB;
        public int BodyIndexA => BodyIndices.BodyIndexA;
        public int BodyIndexB => BodyIndices.BodyIndexB;
        public ColliderKey ColliderKeyA => ColliderKeys.ColliderKeyA;
        public ColliderKey ColliderKeyB => ColliderKeys.ColliderKeyB;

        public StatefulTriggerEvent(Entity entityA, Entity entityB, int bodyIndexA, int bodyIndexB,
                                    ColliderKey colliderKeyA, ColliderKey colliderKeyB)
        {
            Entities = new EntityPair
            {
                EntityA = entityA,
                EntityB = entityB
            };
            BodyIndices = new BodyIndexPair
            {
                BodyIndexA = bodyIndexA,
                BodyIndexB = bodyIndexB
            };
            ColliderKeys = new ColliderKeyPair
            {
                ColliderKeyA = colliderKeyA,
                ColliderKeyB = colliderKeyB
            };
            State = default;
        }

        // Returns other entity in EntityPair, if provided with one
        public Entity GetOtherEntity(Entity entity)
        {
            Assert.IsTrue((entity == EntityA) || (entity == EntityB));
            int2 indexAndVersion = math.select(new int2(EntityB.Index, EntityB.Version),
                new int2(EntityA.Index, EntityA.Version), entity == EntityB);
            return new Entity
            {
                Index = indexAndVersion[0],
                Version = indexAndVersion[1]
            };
        }

        public int CompareTo(StatefulTriggerEvent other)
        {
            var cmpResult = EntityA.CompareTo(other.EntityA);
            if (cmpResult != 0)
            {
                return cmpResult;
            }

            cmpResult = EntityB.CompareTo(other.EntityB);
            if (cmpResult != 0)
            {
                return cmpResult;
            }

            if (ColliderKeyA.Value != other.ColliderKeyA.Value)
            {
                return ColliderKeyA.Value < other.ColliderKeyA.Value ? -1 : 1;
            }

            if (ColliderKeyB.Value != other.ColliderKeyB.Value)
            {
                return ColliderKeyB.Value < other.ColliderKeyB.Value ? -1 : 1;
            }

            return 0;
        }
    }

    // If this component is added to an entity, trigger events won't be added to dynamic buffer
    // of that entity by TriggerEventConversionSystem. This component is by default added to
    // CharacterController entity, so that CharacterControllerSystem can add trigger events to
    // CharacterController on its own, without TriggerEventConversionSystem interference.
    public struct ExcludeFromTriggerEventConversion : IComponentData { }

    // This system converts stream of TriggerEvents to StatefulTriggerEvents that are stored in a Dynamic Buffer.
    // In order for TriggerEvents to be transformed to StatefulTriggerEvents and stored in a Dynamic Buffer, it is required to:
    //    1) Tick IsTrigger on PhysicsShapeAuthoring on the entity that should raise trigger events
    //    2) Add a DynamicBufferTriggerEventAuthoring component to that entity
    //    3) If this is desired on a Character Controller, tick RaiseTriggerEvents on CharacterControllerAuthoring (skip 1) and 2)),
    //    note that Character Controller will not become a trigger, it will raise events when overlapping with one
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(StepPhysicsWorld))]
    [UpdateBefore(typeof(EndFramePhysicsSystem))]
    public class TriggerEventConversionSystem : SystemBase
    {
        public JobHandle OutDependency => Dependency;

        private StepPhysicsWorld m_StepPhysicsWorld = default;
        private BuildPhysicsWorld m_BuildPhysicsWorld = default;
        private EndFramePhysicsSystem m_EndFramePhysicsSystem = default;
        private EntityQuery m_Query = default;

        private NativeList<StatefulTriggerEvent> m_PreviousFrameTriggerEvents;
        private NativeList<StatefulTriggerEvent> m_CurrentFrameTriggerEvents;

        protected override void OnCreate()
        {
            m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            m_EndFramePhysicsSystem = World.GetOrCreateSystem<EndFramePhysicsSystem>();
            m_Query = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(StatefulTriggerEvent)
                },
                None = new ComponentType[]
                {
                    typeof(ExcludeFromTriggerEventConversion)
                }
            });

            m_PreviousFrameTriggerEvents = new NativeList<StatefulTriggerEvent>(Allocator.Persistent);
            m_CurrentFrameTriggerEvents = new NativeList<StatefulTriggerEvent>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            m_PreviousFrameTriggerEvents.Dispose();
            m_CurrentFrameTriggerEvents.Dispose();
        }

        protected void SwapTriggerEventStates()
        {
            var tmp = m_PreviousFrameTriggerEvents;
            m_PreviousFrameTriggerEvents = m_CurrentFrameTriggerEvents;
            m_CurrentFrameTriggerEvents = tmp;
            m_CurrentFrameTriggerEvents.Clear();
        }

        protected static void AddTriggerEventsToDynamicBuffers(NativeList<StatefulTriggerEvent> triggerEventList,
            ref BufferFromEntity<StatefulTriggerEvent> bufferFromEntity, NativeHashMap<Entity, byte> entitiesWithTriggerBuffers)
        {
            for (int i = 0; i < triggerEventList.Length; i++)
            {
                var triggerEvent = triggerEventList[i];
                if (entitiesWithTriggerBuffers.ContainsKey(triggerEvent.EntityA))
                {
                    bufferFromEntity[triggerEvent.EntityA].Add(triggerEvent);
                }
                if (entitiesWithTriggerBuffers.ContainsKey(triggerEvent.EntityB))
                {
                    bufferFromEntity[triggerEvent.EntityB].Add(triggerEvent);
                }
            }
        }

        public static void UpdateTriggerEventState(NativeList<StatefulTriggerEvent> previousFrameTriggerEvents, NativeList<StatefulTriggerEvent> currentFrameTriggerEvents,
            NativeList<StatefulTriggerEvent> resultList)
        {
            int i = 0;
            int j = 0;

            while (i < currentFrameTriggerEvents.Length && j < previousFrameTriggerEvents.Length)
            {
                var currentFrameTriggerEvent = currentFrameTriggerEvents[i];
                var previousFrameTriggerEvent = previousFrameTriggerEvents[j];

                int cmpResult = currentFrameTriggerEvent.CompareTo(previousFrameTriggerEvent);

                // Appears in previous, and current frame, mark it as Stay
                if (cmpResult == 0)
                {
                    currentFrameTriggerEvent.State = EventOverlapState.Stay;
                    resultList.Add(currentFrameTriggerEvent);
                    i++;
                    j++;
                }
                else if (cmpResult < 0)
                {
                    // Appears in current, but not in previous, mark it as Enter
                    currentFrameTriggerEvent.State = EventOverlapState.Enter;
                    resultList.Add(currentFrameTriggerEvent);
                    i++;
                }
                else
                {
                    // Appears in previous, but not in current, mark it as Exit
                    previousFrameTriggerEvent.State = EventOverlapState.Exit;
                    resultList.Add(previousFrameTriggerEvent);
                    j++;
                }
            }

            if (i == currentFrameTriggerEvents.Length)
            {
                while (j < previousFrameTriggerEvents.Length)
                {
                    var triggerEvent = previousFrameTriggerEvents[j++];
                    triggerEvent.State = EventOverlapState.Exit;
                    resultList.Add(triggerEvent);
                }
            }
            else if (j == previousFrameTriggerEvents.Length)
            {
                while (i < currentFrameTriggerEvents.Length)
                {
                    var triggerEvent = currentFrameTriggerEvents[i++];
                    triggerEvent.State = EventOverlapState.Enter;
                    resultList.Add(triggerEvent);
                }
            }
        }

        protected override void OnUpdate()
        {
            if (m_Query.CalculateEntityCount() == 0)
            {
                return;
            }

            Dependency = JobHandle.CombineDependencies(m_StepPhysicsWorld.FinalSimulationJobHandle, Dependency);

            Entities
                .WithName("ClearTriggerEventDynamicBuffersJobParallel")
                .WithBurst()
                .WithNone<ExcludeFromTriggerEventConversion>()
                .ForEach((ref DynamicBuffer<StatefulTriggerEvent> buffer) =>
                {
                    buffer.Clear();
                }).ScheduleParallel();

            SwapTriggerEventStates();

            var currentFrameTriggerEvents = m_CurrentFrameTriggerEvents;
            var previousFrameTriggerEvents = m_PreviousFrameTriggerEvents;

            var triggerEventBufferFromEntity = GetBufferFromEntity<StatefulTriggerEvent>();
            var physicsWorld = m_BuildPhysicsWorld.PhysicsWorld;

            var collectTriggerEventsJob = new CollectTriggerEventsJob
            {
                TriggerEvents = currentFrameTriggerEvents
            };

            var collectJobHandle = collectTriggerEventsJob.Schedule(m_StepPhysicsWorld.Simulation, ref physicsWorld, Dependency);

            // Using HashMap since HashSet doesn't exist
            // Setting value type to byte to minimize memory waste
            NativeHashMap<Entity, byte> entitiesWithBuffersMap = new NativeHashMap<Entity, byte>(0, Allocator.TempJob);

            var collectTriggerBuffersHandle = Entities
                .WithName("CollectTriggerBufferJob")
                .WithBurst()
                .WithNone<ExcludeFromTriggerEventConversion>()
                .ForEach((Entity e, ref DynamicBuffer<StatefulTriggerEvent> buffer) =>
                {
                    entitiesWithBuffersMap.Add(e, 0);
                }).Schedule(Dependency);

            Dependency = JobHandle.CombineDependencies(collectJobHandle, collectTriggerBuffersHandle);

            Job
                .WithName("ConvertTriggerEventStreamToDynamicBufferJob")
                .WithBurst()
                .WithCode(() =>
                {
                    currentFrameTriggerEvents.Sort();

                    var triggerEventsWithStates = new NativeList<StatefulTriggerEvent>(currentFrameTriggerEvents.Length, Allocator.Temp);

                    UpdateTriggerEventState(previousFrameTriggerEvents, currentFrameTriggerEvents, triggerEventsWithStates);
                    AddTriggerEventsToDynamicBuffers(triggerEventsWithStates, ref triggerEventBufferFromEntity, entitiesWithBuffersMap);
                }).Schedule();

            m_EndFramePhysicsSystem.AddInputDependency(Dependency);
            entitiesWithBuffersMap.Dispose(Dependency);
        }

        [BurstCompile]
        public struct CollectTriggerEventsJob : ITriggerEventsJob
        {
            public NativeList<StatefulTriggerEvent> TriggerEvents;

            public void Execute(TriggerEvent triggerEvent)
            {
                TriggerEvents.Add(new StatefulTriggerEvent(
                    triggerEvent.EntityA, triggerEvent.EntityB, triggerEvent.BodyIndexA, triggerEvent.BodyIndexB,
                    triggerEvent.ColliderKeyA, triggerEvent.ColliderKeyB));
            }
        }
    }

    public class DynamicBufferTriggerEventAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddBuffer<StatefulTriggerEvent>(entity);
        }
    }
}

namespace Unity.Physics.Stateful
{
    // Describes the colliding state.
    // CollidingState in StatefulCollisionEvent is set to:
    //    1) EventCollidingState.Enter, when 2 bodies are colliding in the current frame,
    //    but they did not collide in the previous frame
    //    2) EventCollidingState.Stay, when 2 bodies are colliding in the current frame,
    //    and they did collide in the previous frame
    //    3) EventCollidingState.Exit, when 2 bodies are NOT colliding in the current frame,
    //    but they did collide in the previous frame
    public enum EventCollidingState : byte
    {
        Enter,
        Stay,
        Exit
    }

    // Collision Event that is stored inside a DynamicBuffer
    public struct StatefulCollisionEvent : IBufferElementData, IComparable<StatefulCollisionEvent>
    {
        internal BodyIndexPair BodyIndices;
        internal EntityPair Entities;
        internal ColliderKeyPair ColliderKeys;

        // Only if CalculateDetails is checked on PhysicsCollisionEventBuffer of selected entity,
        // this field will have valid value, otherwise it will be zero initialized
        internal Details CollisionDetails;

        public EventCollidingState CollidingState;

        // Normal is pointing from EntityB to EntityA
        public float3 Normal;

        public StatefulCollisionEvent(Entity entityA, Entity entityB, int bodyIndexA, int bodyIndexB,
                                      ColliderKey colliderKeyA, ColliderKey colliderKeyB, float3 normal)
        {
            Entities = new EntityPair
            {
                EntityA = entityA,
                EntityB = entityB
            };
            BodyIndices = new BodyIndexPair
            {
                BodyIndexA = bodyIndexA,
                BodyIndexB = bodyIndexB
            };
            ColliderKeys = new ColliderKeyPair
            {
                ColliderKeyA = colliderKeyA,
                ColliderKeyB = colliderKeyB
            };
            Normal = normal;
            CollidingState = default;
            CollisionDetails = default;
        }

        public Entity EntityA => Entities.EntityA;
        public Entity EntityB => Entities.EntityB;
        public ColliderKey ColliderKeyA => ColliderKeys.ColliderKeyA;
        public ColliderKey ColliderKeyB => ColliderKeys.ColliderKeyB;
        public int BodyIndexA => BodyIndices.BodyIndexA;
        public int BodyIndexB => BodyIndices.BodyIndexB;

        // This struct describes additional, optional, details about collision of 2 bodies
        public struct Details
        {
            internal int IsValid;

            // If 1, then it is a vertex collision
            // If 2, then it is an edge collision
            // If 3 or more, then it is a face collision
            public int NumberOfContactPoints;

            // Estimated impulse applied
            public float EstimatedImpulse;
            // Average contact point position
            public float3 AverageContactPointPosition;

            public Details(int numberOfContactPoints, float estimatedImpulse, float3 averageContactPosition)
            {
                IsValid = 1;
                NumberOfContactPoints = numberOfContactPoints;
                EstimatedImpulse = estimatedImpulse;
                AverageContactPointPosition = averageContactPosition;
            }
        }

        // Returns the other entity in EntityPair, if provided with other one
        public Entity GetOtherEntity(Entity entity)
        {
            Assert.IsTrue((entity == EntityA) || (entity == EntityB));
            int2 indexAndVersion = math.select(new int2(EntityB.Index, EntityB.Version),
                new int2(EntityA.Index, EntityA.Version), entity == EntityB);
            return new Entity
            {
                Index = indexAndVersion[0],
                Version = indexAndVersion[1]
            };
        }

        // Returns the normal pointing from passed entity to the other one in pair
        public float3 GetNormalFrom(Entity entity)
        {
            Assert.IsTrue((entity == EntityA) || (entity == EntityB));
            return math.select(-Normal, Normal, entity == EntityB);
        }

        public bool TryGetDetails(out Details details)
        {
            details = CollisionDetails;
            return CollisionDetails.IsValid != 0;
        }

        public int CompareTo(StatefulCollisionEvent other)
        {
            var cmpResult = EntityA.CompareTo(other.EntityA);
            if (cmpResult != 0)
            {
                return cmpResult;
            }

            cmpResult = EntityB.CompareTo(other.EntityB);
            if (cmpResult != 0)
            {
                return cmpResult;
            }

            if (ColliderKeyA.Value != other.ColliderKeyA.Value)
            {
                return ColliderKeyA.Value < other.ColliderKeyA.Value ? -1 : 1;
            }

            if (ColliderKeyB.Value != other.ColliderKeyB.Value)
            {
                return ColliderKeyB.Value < other.ColliderKeyB.Value ? -1 : 1;
            }

            return 0;
        }
    }

    public struct CollisionEventBuffer : IComponentData
    {
        public int CalculateDetails;
    }

    // This system converts stream of CollisionEvents to StatefulCollisionEvents that are stored in a Dynamic Buffer.
    // In order for CollisionEvents to be transformed to StatefulCollisionEvents and stored in a Dynamic Buffer, it is required to:
    //    1) Tick Raises Collision Events on PhysicsShapeAuthoring on the entity that should raise collision events
    //    2) Add a DynamicBufferCollisionEventAuthoring component to that entity (and select if details should be calculated or not)
    //    3) If this is desired on a Character Controller, tick RaiseCollisionEvents flag on CharacterControllerAuthoring (skip 1) and 2)),
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(StepPhysicsWorld))]
    [UpdateBefore(typeof(EndFramePhysicsSystem))]
    public class CollisionEventConversionSystem : SystemBase
    {
        public JobHandle OutDependency => Dependency;

        private StepPhysicsWorld m_StepPhysicsWorld = default;
        private BuildPhysicsWorld m_BuildPhysicsWorld = default;
        private EndFramePhysicsSystem m_EndFramePhysicsSystem = default;
        private EntityQuery m_Query = default;

        private NativeList<StatefulCollisionEvent> m_PreviousFrameCollisionEvents;
        private NativeList<StatefulCollisionEvent> m_CurrentFrameCollisionEvents;

        protected override void OnCreate()
        {
            m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            m_EndFramePhysicsSystem = World.GetOrCreateSystem<EndFramePhysicsSystem>();
            m_Query = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(CollisionEventBuffer)
                }
            });

            m_PreviousFrameCollisionEvents = new NativeList<StatefulCollisionEvent>(Allocator.Persistent);
            m_CurrentFrameCollisionEvents = new NativeList<StatefulCollisionEvent>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            m_PreviousFrameCollisionEvents.Dispose();
            m_CurrentFrameCollisionEvents.Dispose();
        }

        protected void SwapCollisionEventState()
        {
            var tmp = m_PreviousFrameCollisionEvents;
            m_PreviousFrameCollisionEvents = m_CurrentFrameCollisionEvents;
            m_CurrentFrameCollisionEvents = tmp;
            m_CurrentFrameCollisionEvents.Clear();
        }

        public static void UpdateCollisionEventState(NativeList<StatefulCollisionEvent> previousFrameCollisionEvents,
            NativeList<StatefulCollisionEvent> currentFrameCollisionEvents, NativeList<StatefulCollisionEvent> resultList)
        {
            int i = 0;
            int j = 0;

            while (i < currentFrameCollisionEvents.Length && j < previousFrameCollisionEvents.Length)
            {
                var currentFrameCollisionEvent = currentFrameCollisionEvents[i];
                var previousFrameCollisionEvent = previousFrameCollisionEvents[j];

                int cmpResult = currentFrameCollisionEvent.CompareTo(previousFrameCollisionEvent);

                // Appears in previous, and current frame, mark it as Stay
                if (cmpResult == 0)
                {
                    currentFrameCollisionEvent.CollidingState = EventCollidingState.Stay;
                    resultList.Add(currentFrameCollisionEvent);
                    i++;
                    j++;
                }
                else if (cmpResult < 0)
                {
                    // Appears in current, but not in previous, mark it as Enter
                    currentFrameCollisionEvent.CollidingState = EventCollidingState.Enter;
                    resultList.Add(currentFrameCollisionEvent);
                    i++;
                }
                else
                {
                    // Appears in previous, but not in current, mark it as Exit
                    previousFrameCollisionEvent.CollidingState = EventCollidingState.Exit;
                    resultList.Add(previousFrameCollisionEvent);
                    j++;
                }
            }

            if (i == currentFrameCollisionEvents.Length)
            {
                while (j < previousFrameCollisionEvents.Length)
                {
                    var collisionEvent = previousFrameCollisionEvents[j++];
                    collisionEvent.CollidingState = EventCollidingState.Exit;
                    resultList.Add(collisionEvent);
                }
            }
            else if (j == previousFrameCollisionEvents.Length)
            {
                while (i < currentFrameCollisionEvents.Length)
                {
                    var collisionEvent = currentFrameCollisionEvents[i++];
                    collisionEvent.CollidingState = EventCollidingState.Enter;
                    resultList.Add(collisionEvent);
                }
            }
        }

        protected static void AddCollisionEventsToDynamicBuffers(NativeList<StatefulCollisionEvent> collisionEventList,
            ref BufferFromEntity<StatefulCollisionEvent> bufferFromEntity, NativeHashMap<Entity, byte> entitiesWithCollisionEventBuffers)
        {
            for (int i = 0; i < collisionEventList.Length; i++)
            {
                var collisionEvent = collisionEventList[i];
                if (entitiesWithCollisionEventBuffers.ContainsKey(collisionEvent.EntityA))
                {
                    bufferFromEntity[collisionEvent.EntityA].Add(collisionEvent);
                }
                if (entitiesWithCollisionEventBuffers.ContainsKey(collisionEvent.EntityB))
                {
                    bufferFromEntity[collisionEvent.EntityB].Add(collisionEvent);
                }
            }
        }

        protected override void OnUpdate()
        {
            if (m_Query.CalculateEntityCount() == 0)
            {
                return;
            }

            Dependency = JobHandle.CombineDependencies(m_StepPhysicsWorld.FinalSimulationJobHandle, Dependency);

            Entities
                .WithName("ClearCollisionEventDynamicBuffersJobParallel")
                .WithBurst()
                .WithAll<CollisionEventBuffer>()
                .ForEach((ref DynamicBuffer<StatefulCollisionEvent> buffer) =>
                {
                    buffer.Clear();
                }).ScheduleParallel();

            SwapCollisionEventState();

            var previousFrameCollisionEvents = m_PreviousFrameCollisionEvents;
            var currentFrameCollisionEvents = m_CurrentFrameCollisionEvents;

            var collisionEventBufferFromEntity = GetBufferFromEntity<StatefulCollisionEvent>();
            var physicsCollisionEventBufferTags = GetComponentDataFromEntity<CollisionEventBuffer>();

            // Using HashMap since HashSet doesn't exist
            // Setting value type to byte to minimize memory waste
            NativeHashMap<Entity, byte> entitiesWithBuffersMap = new NativeHashMap<Entity, byte>(0, Allocator.TempJob);

            Entities
                .WithName("CollectCollisionBufferJob")
                .WithBurst()
                .WithAll<CollisionEventBuffer>()
                .ForEach((Entity e, ref DynamicBuffer<StatefulCollisionEvent> buffer) =>
                {
                    entitiesWithBuffersMap.Add(e, 0);
                }).Schedule();

            var collectCollisionEventsJob = new CollectCollisionEventsJob
            {
                CollisionEvents = currentFrameCollisionEvents,
                PhysicsCollisionEventBufferTags = physicsCollisionEventBufferTags,
                PhysicsWorld = m_BuildPhysicsWorld.PhysicsWorld,
                EntitiesWithBuffersMap = entitiesWithBuffersMap
            };

            Dependency = collectCollisionEventsJob.Schedule(m_StepPhysicsWorld.Simulation, ref m_BuildPhysicsWorld.PhysicsWorld, Dependency);

            Job
                .WithName("ConvertCollisionEventStreamToDynamicBufferJob")
                .WithBurst()
                .WithCode(() =>
                {
                    currentFrameCollisionEvents.Sort();

                    var collisionEventsWithStates = new NativeList<StatefulCollisionEvent>(currentFrameCollisionEvents.Length, Allocator.Temp);
                    UpdateCollisionEventState(previousFrameCollisionEvents, currentFrameCollisionEvents, collisionEventsWithStates);
                    AddCollisionEventsToDynamicBuffers(collisionEventsWithStates, ref collisionEventBufferFromEntity, entitiesWithBuffersMap);
                }).Schedule();

            m_EndFramePhysicsSystem.AddInputDependency(Dependency);
            entitiesWithBuffersMap.Dispose(Dependency);
        }

        [BurstCompile]
        public struct CollectCollisionEventsJob : ICollisionEventsJob
        {
            public NativeList<StatefulCollisionEvent> CollisionEvents;
            public ComponentDataFromEntity<CollisionEventBuffer> PhysicsCollisionEventBufferTags;

            [ReadOnly] public NativeHashMap<Entity, byte> EntitiesWithBuffersMap;
            [ReadOnly] public PhysicsWorld PhysicsWorld;

            public void Execute(CollisionEvent collisionEvent)
            {
                var collisionEventBufferElement = new StatefulCollisionEvent(collisionEvent.EntityA, collisionEvent.EntityB,
                    collisionEvent.BodyIndexA, collisionEvent.BodyIndexB, collisionEvent.ColliderKeyA,
                    collisionEvent.ColliderKeyB, collisionEvent.Normal);

                var calculateDetails = false;

                if (EntitiesWithBuffersMap.ContainsKey(collisionEvent.EntityA))
                {
                    if (PhysicsCollisionEventBufferTags[collisionEvent.EntityA].CalculateDetails != 0)
                    {
                        calculateDetails = true;
                    }
                }

                if (!calculateDetails && EntitiesWithBuffersMap.ContainsKey(collisionEvent.EntityB))
                {
                    if (PhysicsCollisionEventBufferTags[collisionEvent.EntityB].CalculateDetails != 0)
                    {
                        calculateDetails = true;
                    }
                }

                if (calculateDetails)
                {
                    var details = collisionEvent.CalculateDetails(ref PhysicsWorld);
                    collisionEventBufferElement.CollisionDetails = new StatefulCollisionEvent.Details(
                        details.EstimatedContactPointPositions.Length, details.EstimatedImpulse, details.AverageContactPointPosition);
                }

                CollisionEvents.Add(collisionEventBufferElement);
            }
        }
    }

    public class DynamicBufferCollisionEventAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        [Tooltip("If selected, the details will be calculated in collision event dynamic buffer of this entity")]
        public bool CalculateDetails = false;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var dynamicBufferTag = new CollisionEventBuffer
            {
                CalculateDetails = CalculateDetails ? 1 : 0
            };

            dstManager.AddComponentData(entity, dynamicBufferTag);
            dstManager.AddBuffer<StatefulCollisionEvent>(entity);
        }
    }
}


[Serializable]
public struct CharacterControllerComponentData : IComponentData
{
    public float3 Gravity;
    public float MovementSpeed;
    public float MaxMovementSpeed;
    public float RotationSpeed;
    public float JumpUpwardsSpeed;
    public float MaxSlope; // radians
    public int MaxIterations;
    public float CharacterMass;
    public float SkinWidth;
    public float ContactTolerance;
    public byte AffectsPhysicsBodies;
    public byte RaiseCollisionEvents;
    public byte RaiseTriggerEvents;
}

public struct CharacterControllerInput : IComponentData
{
    public float2 Movement;
    public float2 Looking;
    public int Jumped;
}

[WriteGroup(typeof(PhysicsGraphicalInterpolationBuffer))]
[WriteGroup(typeof(PhysicsGraphicalSmoothing))]
public struct CharacterControllerInternalData : IComponentData
{
    public float CurrentRotationAngle;
    public CharacterSupportState SupportedState;
    public float3 UnsupportedVelocity;
    public PhysicsVelocity Velocity;
    public Entity Entity;
    public bool IsJumping;
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

// override the behavior of BufferInterpolatedRigidBodiesMotion
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(BuildPhysicsWorld)), UpdateBefore(typeof(ExportPhysicsWorld))]
[UpdateAfter(typeof(BufferInterpolatedRigidBodiesMotion)), UpdateBefore(typeof(CharacterControllerSystem))]
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

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(ExportPhysicsWorld)), UpdateBefore(typeof(EndFramePhysicsSystem))]
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

                float horizontal = ccInternalData.Input.Movement.x;
                float vertical = ccInternalData.Input.Movement.y;
                bool jumpRequested = ccInternalData.Input.Jumped != 0;
                ccInternalData.Input.Jumped = 0; // "consume" the event
                bool haveInput = (math.abs(horizontal) > float.Epsilon) || (math.abs(vertical) > float.Epsilon);
                if (haveInput)
                {
                    float3 localSpaceMovement = forward * vertical + right * horizontal;
                    float3 worldSpaceMovement = math.rotate(quaternion.AxisAngle(up, ccInternalData.CurrentRotationAngle), localSpaceMovement);
                    requestedMovementDirection = math.normalize(worldSpaceMovement);
                }
                shouldJump = jumpRequested && ccInternalData.SupportedState == CharacterSupportState.Supported;
            }

            // Turning
            {
                float horizontal = ccInternalData.Input.Looking.x;
                bool haveInput = (math.abs(horizontal) > float.Epsilon);
                if (haveInput)
                {
                    var userRotationSpeed = horizontal * ccComponentData.RotationSpeed;
                    ccInternalData.Velocity.Angular = -userRotationSpeed * up;
                    ccInternalData.CurrentRotationAngle += userRotationSpeed * DeltaTime;
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
                linearVelocity = requestedMovementDirection * ccComponentData.MovementSpeed +
                    (ccInternalData.SupportedState != CharacterSupportState.Supported ? ccInternalData.UnsupportedVelocity : float3.zero);
            }
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

        Dependency = JobHandle.CombineDependencies(Dependency, m_ExportPhysicsWorldSystem.GetOutputDependency());
        Dependency = ccJob.Schedule(m_CharacterControllersGroup, Dependency);

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
        m_EndFramePhysicsSystem.AddInputDependency(disposeHandle);

        // Invalidate input dependency since it's been used by now
        m_InputDependency = default;
    }
}
