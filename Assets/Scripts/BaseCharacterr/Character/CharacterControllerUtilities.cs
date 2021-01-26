using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.Assertions;

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
    public struct StatefulTriggerEvent : IBufferElementData, System.IComparable<StatefulTriggerEvent>
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

// Stores the impulse to be applied by the character controller body
public struct DeferredCharacterControllerImpulse
{
    public Entity Entity;
    public float3 Impulse;
    public float3 Point;
}

public static class CharacterControllerUtilities
{
    const float k_SimplexSolverEpsilon = 0.0001f;
    const float k_SimplexSolverEpsilonSq = k_SimplexSolverEpsilon * k_SimplexSolverEpsilon;

    const int k_DefaultQueryHitsCapacity = 8;
    const int k_DefaultConstraintsCapacity = 2 * k_DefaultQueryHitsCapacity;

    public enum CharacterSupportState : byte
    {
        Unsupported = 0,
        Sliding,
        Supported
    }

    public struct CharacterControllerStepInput
    {
        public PhysicsWorld World;
        public float DeltaTime;
        public float3 Gravity;
        public float3 Up;
        public int MaxIterations;
        public float Tau;
        public float Damping;
        public float SkinWidth;
        public float ContactTolerance;
        public float MaxSlope;
        public int RigidBodyIndex;
        public float3 CurrentVelocity;
        public float MaxMovementSpeed;
    }

    public struct CharacterControllerAllHitsCollector<T> : ICollector<T> where T : struct, IQueryResult
    {
        private int m_selfRBIndex;

        public bool EarlyOutOnFirstHit => false;
        public float MaxFraction { get; }
        public int NumHits => AllHits.Length;

        public float MinHitFraction;
        public NativeList<T> AllHits;
        public NativeList<T> TriggerHits;

        private PhysicsWorld m_world;
             
        public CharacterControllerAllHitsCollector(int rbIndex, float maxFraction, ref NativeList<T> allHits, PhysicsWorld world,
                                                   NativeList<T> triggerHits = default)
        {
            MaxFraction = maxFraction;
            AllHits = allHits;
            m_selfRBIndex = rbIndex;
            m_world = world;
            TriggerHits = triggerHits;
            MinHitFraction = float.MaxValue;
        }

        #region ICollector

        public bool AddHit(T hit)
        {
            Assert.IsTrue(hit.Fraction < MaxFraction);

            if (hit.RigidBodyIndex == m_selfRBIndex)
            {
                return false;
            }

            if (IsTrigger(m_world.Bodies, hit.RigidBodyIndex, hit.ColliderKey))
            {
                if (TriggerHits.IsCreated)
                {
                    TriggerHits.Add(hit);
                }
                return false;
            }

            MinHitFraction = math.min(MinHitFraction, hit.Fraction);
            AllHits.Add(hit);
            return true;
        }

        #endregion
    }

    // A collector which stores only the closest hit different from itself, the triggers, and predefined list of values it hit.
    public struct CharacterControllerClosestHitCollector<T> : ICollector<T> where T : struct, IQueryResult
    {
        public bool EarlyOutOnFirstHit => false;
        public float MaxFraction { get; private set; }
        public int NumHits { get; private set; }

        private T m_ClosestHit;
        public T ClosestHit => m_ClosestHit;

        private int m_selfRBIndex;
        private PhysicsWorld m_world;

        private NativeList<SurfaceConstraintInfo> m_PredefinedConstraints;

        public CharacterControllerClosestHitCollector(NativeList<SurfaceConstraintInfo> predefinedConstraints, PhysicsWorld world, int rbIndex, float maxFraction)
        {
            MaxFraction = maxFraction;
            m_ClosestHit = default;
            NumHits = 0;
            m_selfRBIndex = rbIndex;
            m_world = world;
            m_PredefinedConstraints = predefinedConstraints;
        }

        #region ICollector

        public bool AddHit(T hit)
        {
            Assert.IsTrue(hit.Fraction <= MaxFraction);

            // Check self hits and trigger hits
            if ((hit.RigidBodyIndex == m_selfRBIndex) || IsTrigger(m_world.Bodies, hit.RigidBodyIndex, hit.ColliderKey))
            {
                return false;
            }

            // Check predefined hits
            for (int i = 0; i < m_PredefinedConstraints.Length; i++)
            {
                SurfaceConstraintInfo constraint = m_PredefinedConstraints[i];
                if (constraint.RigidBodyIndex == hit.RigidBodyIndex &&
                    constraint.ColliderKey.Equals(hit.ColliderKey))
                {
                    // Hit was already defined, skip it
                    return false;
                }
            }

            // Finally, accept the hit
            MaxFraction = hit.Fraction;
            m_ClosestHit = hit;
            NumHits = 1;
            return true;
        }

        #endregion
    }

    public static unsafe void CheckSupport(
        ref PhysicsWorld world, ref PhysicsCollider collider, CharacterControllerStepInput stepInput, RigidTransform transform,
        out CharacterSupportState characterState, out float3 surfaceNormal, out float3 surfaceVelocity,
        NativeList<StatefulCollisionEvent> collisionEvents = default)
    {
        surfaceNormal = float3.zero;
        surfaceVelocity = float3.zero;

        // Up direction must be normalized
        Assert.IsTrue(Unity.Physics.Math.IsNormalized(stepInput.Up));

        // Query the world
        NativeList<ColliderCastHit> castHits = new NativeList<ColliderCastHit>(k_DefaultQueryHitsCapacity, Allocator.Temp);
        CharacterControllerAllHitsCollector<ColliderCastHit> castHitsCollector = new CharacterControllerAllHitsCollector<ColliderCastHit>(
            stepInput.RigidBodyIndex, 1.0f, ref castHits, world);
        var maxDisplacement = -stepInput.ContactTolerance * stepInput.Up;
        {
            ColliderCastInput input = new ColliderCastInput()
            {
                Collider = collider.ColliderPtr,
                Orientation = transform.rot,
                Start = transform.pos,
                End = transform.pos + maxDisplacement
            };

            world.CastCollider(input, ref castHitsCollector);
        }

        // If no hits, proclaim unsupported state
        if (castHitsCollector.NumHits == 0)
        {
            characterState = CharacterSupportState.Unsupported;
            return;
        }

        float maxSlopeCos = math.cos(stepInput.MaxSlope);

        // Iterate over distance hits and create constraints from them
        NativeList<SurfaceConstraintInfo> constraints = new NativeList<SurfaceConstraintInfo>(k_DefaultConstraintsCapacity, Allocator.Temp);
        float maxDisplacementLength = math.length(maxDisplacement);
        for (int i = 0; i < castHitsCollector.NumHits; i++)
        {
            ColliderCastHit hit = castHitsCollector.AllHits[i];
            CreateConstraint(stepInput.World, stepInput.Up,
                hit.RigidBodyIndex, hit.ColliderKey, hit.Position, hit.SurfaceNormal, hit.Fraction * maxDisplacementLength,
                stepInput.SkinWidth, maxSlopeCos, ref constraints);
        }

        // Velocity for support checking
        float3 initialVelocity = maxDisplacement / stepInput.DeltaTime;

        // Solve downwards (don't use min delta time, try to solve full step)
        float3 outVelocity = initialVelocity;
        float3 outPosition = transform.pos;
        SimplexSolver.Solve(stepInput.DeltaTime, stepInput.DeltaTime, stepInput.Up, stepInput.MaxMovementSpeed,
            constraints, ref outPosition, ref outVelocity, out float integratedTime, false);

        // Get info on surface
        int numSupportingPlanes = 0;
        {
            for (int j = 0; j < constraints.Length; j++)
            {
                var constraint = constraints[j];
                if (constraint.Touched && !constraint.IsTooSteep && !constraint.IsMaxSlope)
                {
                    numSupportingPlanes++;
                    surfaceNormal += constraint.Plane.Normal;
                    surfaceVelocity += constraint.Velocity;

                    // Add supporting planes to collision events
                    if (collisionEvents.IsCreated)
                    {
                        var collisionEvent = new StatefulCollisionEvent(stepInput.World.Bodies[stepInput.RigidBodyIndex].Entity,
                            stepInput.World.Bodies[constraint.RigidBodyIndex].Entity, stepInput.RigidBodyIndex, constraint.RigidBodyIndex,
                            ColliderKey.Empty, constraint.ColliderKey, constraint.Plane.Normal);
                        collisionEvent.CollisionDetails = new StatefulCollisionEvent.Details(1, 0, constraint.HitPosition);
                        collisionEvents.Add(collisionEvent);
                    }
                }
            }

            if (numSupportingPlanes > 0)
            {
                float invNumSupportingPlanes = 1.0f / numSupportingPlanes;
                surfaceNormal *= invNumSupportingPlanes;
                surfaceVelocity *= invNumSupportingPlanes;

                surfaceNormal = math.normalize(surfaceNormal);
            }
        }

        // Check support state
        {
            if (math.lengthsq(initialVelocity - outVelocity) < k_SimplexSolverEpsilonSq)
            {
                // If velocity hasn't changed significantly, declare unsupported state
                characterState = CharacterSupportState.Unsupported;
            }
            else if (math.lengthsq(outVelocity) < k_SimplexSolverEpsilonSq && numSupportingPlanes > 0)
            {
                // If velocity is very small, declare supported state
                characterState = CharacterSupportState.Supported;
            }
            else
            {
                // Check if sliding
                outVelocity = math.normalize(outVelocity);
                float slopeAngleSin = math.max(0.0f, math.dot(outVelocity, -stepInput.Up));
                float slopeAngleCosSq = 1 - slopeAngleSin * slopeAngleSin;
                if (slopeAngleCosSq <= maxSlopeCos * maxSlopeCos)
                {
                    characterState = CharacterSupportState.Sliding;
                }
                else if (numSupportingPlanes > 0)
                {
                    characterState = CharacterSupportState.Supported;
                }
                else
                {
                    // If numSupportingPlanes is 0, surface normal is invalid, so state is unsupported
                    characterState = CharacterSupportState.Unsupported;
                }
            }
        }
    }

    public static unsafe void CollideAndIntegrate(
        CharacterControllerStepInput stepInput, float characterMass, bool affectBodies, Unity.Physics.Collider* collider,
        ref RigidTransform transform, ref float3 linearVelocity, ref NativeStream.Writer deferredImpulseWriter,
        NativeList<StatefulCollisionEvent> collisionEvents = default, NativeList<StatefulTriggerEvent> triggerEvents = default)
    {
        // Copy parameters
        float deltaTime = stepInput.DeltaTime;
        float3 up = stepInput.Up;
        PhysicsWorld world = stepInput.World;

        float remainingTime = deltaTime;

        float3 newPosition = transform.pos;
        quaternion orientation = transform.rot;
        float3 newVelocity = linearVelocity;

        float maxSlopeCos = math.cos(stepInput.MaxSlope);

        const float timeEpsilon = 0.000001f;
        for (int i = 0; i < stepInput.MaxIterations && remainingTime > timeEpsilon; i++)
        {
            NativeList<SurfaceConstraintInfo> constraints = new NativeList<SurfaceConstraintInfo>(k_DefaultConstraintsCapacity, Allocator.Temp);

            // Do a collider cast
            {
                float3 displacement = newVelocity * remainingTime;
                NativeList<ColliderCastHit> triggerHits = default;
                if (triggerEvents.IsCreated)
                {
                    triggerHits = new NativeList<ColliderCastHit>(k_DefaultQueryHitsCapacity / 4, Allocator.Temp);
                }
                NativeList<ColliderCastHit> castHits = new NativeList<ColliderCastHit>(k_DefaultQueryHitsCapacity, Allocator.Temp);
                CharacterControllerAllHitsCollector<ColliderCastHit> collector = new CharacterControllerAllHitsCollector<ColliderCastHit>(
                    stepInput.RigidBodyIndex, 1.0f, ref castHits, world, triggerHits);
                ColliderCastInput input = new ColliderCastInput()
                {
                    Collider = collider,
                    Orientation = orientation,
                    Start = newPosition,
                    End = newPosition + displacement
                };
                world.CastCollider(input, ref collector);

                // Iterate over hits and create constraints from them
                for (int hitIndex = 0; hitIndex < collector.NumHits; hitIndex++)
                {
                    ColliderCastHit hit = collector.AllHits[hitIndex];
                    CreateConstraint(stepInput.World, stepInput.Up,
                        hit.RigidBodyIndex, hit.ColliderKey, hit.Position, hit.SurfaceNormal, math.dot(-hit.SurfaceNormal, hit.Fraction * displacement),
                        stepInput.SkinWidth, maxSlopeCos, ref constraints);
                }

                // Update trigger events
                if (triggerEvents.IsCreated)
                {
                    UpdateTriggersSeen(stepInput, triggerHits, triggerEvents, collector.MinHitFraction);
                }
            }

            // Then do a collider distance for penetration recovery,
            // but only fix up penetrating hits
            {
                // Collider distance query
                NativeList<DistanceHit> distanceHits = new NativeList<DistanceHit>(k_DefaultQueryHitsCapacity, Allocator.Temp);
                CharacterControllerAllHitsCollector<DistanceHit> distanceHitsCollector = new CharacterControllerAllHitsCollector<DistanceHit>(
                    stepInput.RigidBodyIndex, stepInput.ContactTolerance, ref distanceHits, world);
                {
                    ColliderDistanceInput input = new ColliderDistanceInput()
                    {
                        MaxDistance = stepInput.ContactTolerance,
                        Transform = transform,
                        Collider = collider
                    };
                    world.CalculateDistance(input, ref distanceHitsCollector);
                }

                // Iterate over penetrating hits and fix up distance and normal
                int numConstraints = constraints.Length;
                for (int hitIndex = 0; hitIndex < distanceHitsCollector.NumHits; hitIndex++)
                {
                    DistanceHit hit = distanceHitsCollector.AllHits[hitIndex];
                    if (hit.Distance < stepInput.SkinWidth)
                    {
                        bool found = false;

                        // Iterate backwards to locate the original constraint before the max slope constraint
                        for (int constraintIndex = numConstraints - 1; constraintIndex >= 0; constraintIndex--)
                        {
                            SurfaceConstraintInfo constraint = constraints[constraintIndex];
                            if (constraint.RigidBodyIndex == hit.RigidBodyIndex &&
                                constraint.ColliderKey.Equals(hit.ColliderKey))
                            {
                                // Fix up the constraint (normal, distance)
                                {
                                    // Create new constraint
                                    CreateConstraintFromHit(world, hit.RigidBodyIndex, hit.ColliderKey,
                                        hit.Position, hit.SurfaceNormal, hit.Distance,
                                        stepInput.SkinWidth, out SurfaceConstraintInfo newConstraint);

                                    // Resolve its penetration
                                    ResolveConstraintPenetration(ref newConstraint);

                                    // Write back
                                    constraints[constraintIndex] = newConstraint;
                                }

                                found = true;
                                break;
                            }
                        }

                        // Add penetrating hit not caught by collider cast
                        if (!found)
                        {
                            CreateConstraint(stepInput.World, stepInput.Up,
                                hit.RigidBodyIndex, hit.ColliderKey, hit.Position, hit.SurfaceNormal, hit.Distance,
                                stepInput.SkinWidth, maxSlopeCos, ref constraints);
                        }
                    }
                }
            }

            // Min delta time for solver to break
            float minDeltaTime = 0.0f;
            if (math.lengthsq(newVelocity) > k_SimplexSolverEpsilonSq)
            {
                // Min delta time to travel at least 1cm
                minDeltaTime = 0.01f / math.length(newVelocity);
            }

            // Solve
            float3 prevVelocity = newVelocity;
            float3 prevPosition = newPosition;
            SimplexSolver.Solve(remainingTime, minDeltaTime, up, stepInput.MaxMovementSpeed, constraints, ref newPosition, ref newVelocity, out float integratedTime);

            // Apply impulses to hit bodies and store collision events
            if (affectBodies || collisionEvents.IsCreated)
            {
                CalculateAndStoreDeferredImpulsesAndCollisionEvents(stepInput, affectBodies, characterMass,
                    prevVelocity, constraints, ref deferredImpulseWriter, collisionEvents);
            }

            // Calculate new displacement
            float3 newDisplacement = newPosition - prevPosition;

            // If simplex solver moved the character we need to re-cast to make sure it can move to new position
            if (math.lengthsq(newDisplacement) > k_SimplexSolverEpsilon)
            {
                // Check if we can walk to the position simplex solver has suggested
                var newCollector = new CharacterControllerClosestHitCollector<ColliderCastHit>(constraints, world, stepInput.RigidBodyIndex, 1.0f);

                ColliderCastInput input = new ColliderCastInput()
                {
                    Collider = collider,
                    Orientation = orientation,
                    Start = prevPosition,
                    End = prevPosition + newDisplacement
                };

                world.CastCollider(input, ref newCollector);

                if (newCollector.NumHits > 0)
                {
                    ColliderCastHit hit = newCollector.ClosestHit;

                    // Move character along the newDisplacement direction until it reaches this new contact
                    {
                        Assert.IsTrue(hit.Fraction >= 0.0f && hit.Fraction <= 1.0f);

                        integratedTime *= hit.Fraction;
                        newPosition = prevPosition + newDisplacement * hit.Fraction;
                    }
                }
            }

            // Reduce remaining time
            remainingTime -= integratedTime;

            // Write back position so that the distance query will update results
            transform.pos = newPosition;
        }

        // Write back final velocity
        linearVelocity = newVelocity;
    }

    private static void CreateConstraintFromHit(PhysicsWorld world, int rigidBodyIndex, ColliderKey colliderKey,
        float3 hitPosition, float3 normal, float distance, float skinWidth, out SurfaceConstraintInfo constraint)
    {
        bool bodyIsDynamic = 0 <= rigidBodyIndex && rigidBodyIndex < world.NumDynamicBodies;
        constraint = new SurfaceConstraintInfo()
        {
            Plane = new Unity.Physics.Plane
            {
                Normal = normal,
                Distance = distance - skinWidth,
            },
            RigidBodyIndex = rigidBodyIndex,
            ColliderKey = colliderKey,
            HitPosition = hitPosition,
            Velocity = bodyIsDynamic ?
                world.GetLinearVelocity(rigidBodyIndex, hitPosition) :
                float3.zero,
            Priority = bodyIsDynamic ? 1 : 0
        };
    }

    private static void CreateMaxSlopeConstraint(float3 up, ref SurfaceConstraintInfo constraint, out SurfaceConstraintInfo maxSlopeConstraint)
    {
        float verticalComponent = math.dot(constraint.Plane.Normal, up);

        SurfaceConstraintInfo newConstraint = constraint;
        newConstraint.Plane.Normal = math.normalize(newConstraint.Plane.Normal - verticalComponent * up);
        newConstraint.IsMaxSlope = true;

        float distance = newConstraint.Plane.Distance;

        // Calculate distance to the original plane along the new normal.
        // Clamp the new distance to 2x the old distance to avoid penetration recovery explosions.
        newConstraint.Plane.Distance = distance / math.max(math.dot(newConstraint.Plane.Normal, constraint.Plane.Normal), 0.5f);

        if (newConstraint.Plane.Distance < 0.0f)
        {
            // Disable penetration recovery for the original plane
            constraint.Plane.Distance = 0.0f;

            // Prepare velocity to resolve penetration
            ResolveConstraintPenetration(ref newConstraint);
        }

        // Output max slope constraint
        maxSlopeConstraint = newConstraint;
    }

    private static void ResolveConstraintPenetration(ref SurfaceConstraintInfo constraint)
    {
        // Fix up the velocity to enable penetration recovery
        if (constraint.Plane.Distance < 0.0f)
        {
            float3 newVel = constraint.Velocity - constraint.Plane.Normal * constraint.Plane.Distance;
            constraint.Velocity = newVel;
            constraint.Plane.Distance = 0.0f;
        }
    }

    private static void CreateConstraint(PhysicsWorld world, float3 up,
        int hitRigidBodyIndex, ColliderKey hitColliderKey, float3 hitPosition, float3 hitSurfaceNormal, float hitDistance,
        float skinWidth, float maxSlopeCos, ref NativeList<SurfaceConstraintInfo> constraints)
    {
        CreateConstraintFromHit(world, hitRigidBodyIndex, hitColliderKey, hitPosition,
            hitSurfaceNormal, hitDistance, skinWidth, out SurfaceConstraintInfo constraint);

        // Check if max slope plane is required
        float verticalComponent = math.dot(constraint.Plane.Normal, up);
        bool shouldAddPlane = verticalComponent > k_SimplexSolverEpsilon && verticalComponent < maxSlopeCos;
        if (shouldAddPlane)
        {
            constraint.IsTooSteep = true;
            CreateMaxSlopeConstraint(up, ref constraint, out SurfaceConstraintInfo maxSlopeConstraint);
            constraints.Add(maxSlopeConstraint);
        }

        // Prepare velocity to resolve penetration
        ResolveConstraintPenetration(ref constraint);

        // Add original constraint to the list
        constraints.Add(constraint);
    }

    private static unsafe void CalculateAndStoreDeferredImpulsesAndCollisionEvents(
        CharacterControllerStepInput stepInput, bool affectBodies, float characterMass,
        float3 linearVelocity, NativeList<SurfaceConstraintInfo> constraints, ref NativeStream.Writer deferredImpulseWriter,
        NativeList<StatefulCollisionEvent> collisionEvents)
    {
        PhysicsWorld world = stepInput.World;
        for (int i = 0; i < constraints.Length; i++)
        {
            SurfaceConstraintInfo constraint = constraints[i];
            int rigidBodyIndex = constraint.RigidBodyIndex;

            float3 impulse = float3.zero;

            if (rigidBodyIndex < 0)
            {
                continue;
            }

            // Skip static bodies if needed to calculate impulse
            if (affectBodies && (rigidBodyIndex < world.NumDynamicBodies))
            {
                RigidBody body = world.Bodies[rigidBodyIndex];

                float3 pointRelVel = world.GetLinearVelocity(rigidBodyIndex, constraint.HitPosition);
                pointRelVel -= linearVelocity;

                float projectedVelocity = math.dot(pointRelVel, constraint.Plane.Normal);

                // Required velocity change
                float deltaVelocity = -projectedVelocity * stepInput.Damping;

                float distance = constraint.Plane.Distance;
                if (distance < 0.0f)
                {
                    deltaVelocity += (distance / stepInput.DeltaTime) * stepInput.Tau;
                }

                // Calculate impulse
                MotionVelocity mv = world.MotionVelocities[rigidBodyIndex];
                if (deltaVelocity < 0.0f)
                {
                    // Impulse magnitude
                    float impulseMagnitude = 0.0f;
                    {
                        float objectMassInv = GetInvMassAtPoint(constraint.HitPosition, constraint.Plane.Normal, body, mv);
                        impulseMagnitude = deltaVelocity / objectMassInv;
                    }

                    impulse = impulseMagnitude * constraint.Plane.Normal;
                }

                // Add gravity
                {
                    // Effect of gravity on character velocity in the normal direction
                    float3 charVelDown = stepInput.Gravity * stepInput.DeltaTime;
                    float relVelN = math.dot(charVelDown, constraint.Plane.Normal);

                    // Subtract separation velocity if separating contact
                    {
                        bool isSeparatingContact = projectedVelocity < 0.0f;
                        float newRelVelN = relVelN - projectedVelocity;
                        relVelN = math.select(relVelN, newRelVelN, isSeparatingContact);
                    }

                    // If resulting velocity is negative, an impulse is applied to stop the character
                    // from falling into the body
                    {
                        float3 newImpulse = impulse;
                        newImpulse += relVelN * characterMass * constraint.Plane.Normal;
                        impulse = math.select(impulse, newImpulse, relVelN < 0.0f);
                    }
                }

                // Store impulse
                deferredImpulseWriter.Write(
                    new DeferredCharacterControllerImpulse()
                    {
                        Entity = body.Entity,
                        Impulse = impulse,
                        Point = constraint.HitPosition
                    });
            }

            if (collisionEvents.IsCreated && constraint.Touched && !constraint.IsMaxSlope)
            {
                var collisionEvent = new StatefulCollisionEvent(world.Bodies[stepInput.RigidBodyIndex].Entity,
                    world.Bodies[rigidBodyIndex].Entity, stepInput.RigidBodyIndex, rigidBodyIndex, ColliderKey.Empty,
                    constraint.ColliderKey, constraint.Plane.Normal);
                collisionEvent.CollisionDetails = new StatefulCollisionEvent.Details(
                    1, math.dot(impulse, collisionEvent.Normal), constraint.HitPosition);

                // check if collision event exists for the same bodyID and colliderKey
                // although this is a nested for, number of solved constraints shouldn't be high
                // if the same constraint (same entities, rigidbody indices and collider keys)
                // is solved in multiple solver iterations, pick the one from latest iteration
                bool newEvent = true;
                for (int j = 0; j < collisionEvents.Length; j++)
                {
                    if (collisionEvents[j].CompareTo(collisionEvent) == 0)
                    {
                        collisionEvents[j] = collisionEvent;
                        newEvent = false;
                        break;
                    }
                }
                if (newEvent)
                {
                    collisionEvents.Add(collisionEvent);
                }
            }
        }
    }

    private static void UpdateTriggersSeen<T>(CharacterControllerStepInput stepInput, NativeList<T> triggerHits,
        NativeList<StatefulTriggerEvent> currentFrameTriggerEvents, float maxFraction) where T : struct, IQueryResult
    {
        var world = stepInput.World;
        for (int i = 0; i < triggerHits.Length; i++)
        {
            var hit = triggerHits[i];

            if (hit.Fraction > maxFraction)
            {
                continue;
            }

            var found = false;
            for (int j = 0; j < currentFrameTriggerEvents.Length; j++)
            {
                var triggerEvent = currentFrameTriggerEvents[j];
                if ((triggerEvent.EntityB == hit.Entity) &&
                    (triggerEvent.ColliderKeyB.Value == hit.ColliderKey.Value))
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                currentFrameTriggerEvents.Add(new StatefulTriggerEvent(world.Bodies[stepInput.RigidBodyIndex].Entity, hit.Entity,
                    stepInput.RigidBodyIndex, hit.RigidBodyIndex, ColliderKey.Empty, hit.ColliderKey));
            }
        }
    }

    private static unsafe bool IsTrigger(NativeArray<RigidBody> bodies, int rigidBodyIndex, ColliderKey colliderKey)
    {
        RigidBody hitBody = bodies[rigidBodyIndex];
        hitBody.Collider.Value.GetLeaf(colliderKey, out ChildCollider leafCollider);
        Unity.Physics.Material material = UnsafeUtility.AsRef<ConvexColliderHeader>(leafCollider.Collider).Material;
        return material.CollisionResponse == CollisionResponsePolicy.RaiseTriggerEvents;
    }

    static float GetInvMassAtPoint(float3 point, float3 normal, RigidBody body, MotionVelocity mv)
    {
        var massCenter =
            math.transform(body.WorldFromBody, body.Collider.Value.MassProperties.MassDistribution.Transform.pos);
        float3 arm = point - massCenter;
        float3 jacAng = math.cross(arm, normal);
        float3 armC = jacAng * mv.InverseInertia;

        float objectMassInv = math.dot(armC, jacAng);
        objectMassInv += mv.InverseMass;

        return objectMassInv;
    }
}
