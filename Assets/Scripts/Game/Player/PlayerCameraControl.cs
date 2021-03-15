using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.DebugDisplay;
using Unity.Mathematics;

namespace FPSdemo
{
    public static class PlayerCameraControl
    {
        public struct State : IComponentData
        {
            public int isEnabled;
            public Vector3 position;
            public Quaternion rotation;
            public float fieldOfView;
        }
        public struct CameraEntity : ISystemStateComponentData
        {
            public Entity Value;
        }


        //[DisableAutoCreation]
        public class HandlePlayerCameraControlSpawn : SystemBase
        {
            public HandlePlayerCameraControlSpawn()
            {
                 _BlobAssetStore = new BlobAssetStore();
                m_cameraPrefab = (GameObject)Resources.Load("Prefabs/PlayerCamera");
                //Debug.Log(m_cameraPrefab);
            }

            protected override void OnStopRunning()
            {
                _BlobAssetStore.Dispose();
            }

            protected override void OnCreate()
            {
                var settings = GameObjectConversionSettings.FromWorld(World, _BlobAssetStore);
                m_cameraPrefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(m_cameraPrefab, settings);
                var camera = EntityManager.GetComponentObject<Camera>(m_cameraPrefabEntity);
                camera.enabled = false;
                var audioListener = EntityManager.GetComponentObject<AudioListener>(m_cameraPrefabEntity);
                audioListener.enabled = false;
            }

            protected override void OnUpdate()
            {
                Entities
                  .WithStructuralChanges()
                  .WithoutBurst().WithAll<State>()
                  .WithNone<CameraEntity>()
                  .ForEach((Entity entity) =>
                  {
                      var w=World.EntityManager.Instantiate(m_cameraPrefabEntity);
                      var camera = EntityManager.GetComponentObject<Camera>(w);
                      camera.enabled = false;
                      var audioListener = EntityManager.GetComponentObject<AudioListener>(w);
                      audioListener.enabled = false;
                      World.EntityManager.AddComponentData(entity, new CameraEntity
                      {
                          Value = w,
                      });
                  }).Run();
            }
            BlobAssetStore _BlobAssetStore;
            GameObject m_cameraPrefab;
            Entity m_cameraPrefabEntity;
        }

        //[DisableAutoCreation]
        public class UpdatePlayerCameras : SystemBase
        {

            protected override void OnUpdate()
            {

                Entities.WithName("UpdatePlayerCamerasJob")
                    .WithoutBurst()
                    .ForEach((Entity entity, in PlayerCameraControl.State state, in CameraEntity cameraEntity,in LocalToWorld lw) =>
                    {
                        // We get Camera here as it might be disabled and therefore does not appear in query
                  
                        var camera = EntityManager.GetComponentObject<Camera>(cameraEntity.Value);
                        var enabled = state.isEnabled;
                        var isEnabled = camera.enabled;
                        if (enabled == 0)
                        {
                            if (isEnabled)
                            {
                                camera.enabled = false;
                                var audioListener = EntityManager.GetComponentObject<AudioListener>(cameraEntity.Value);
                                audioListener.enabled = false;

                            }
                            return;
                        }

                        if (!isEnabled)
                        {
                            camera.enabled = true;

                            var audioListener = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentObject<AudioListener>(cameraEntity.Value);
                            audioListener.enabled = true;

                        }

                        camera.fieldOfView = state.fieldOfView;
                        //camera.transform.position = state.position;
                        //camera.transform.rotation = state.rotation;
                        //Debug.Log(lw.Position);
                        //Debug.Log(camera.transform.position);
                        //camera.transform.position = lw.Position;
                        //camera.transform.rotation = lw.Rotation;
                        EntityManager.SetComponentData<Translation>(cameraEntity.Value, new Translation { Value = lw.Position });
                        EntityManager.SetComponentData<Rotation>(cameraEntity.Value, new Rotation { Value = lw.Rotation });

                        //elc.Position = lw.Position;
                        //camera.transform.rotation = lw.Rotation;

                    }).Run();
            }
        }

        //[DisableAutoCreation]
        public class CleanPlayerCameras : SystemBase
        {
            protected override void OnCreate()
            {

                ecbSource = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
                EntityQueryDesc query = new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        typeof(CameraEntity)
                    },
                    None = new ComponentType[]
                    {
                        typeof(State)
                    }

                };
                m_CamerasEntityQuery = GetEntityQuery(query);
            }

            protected override void OnUpdate()
            {
                EntityCommandBuffer.ParallelWriter parallelWriterECB = ecbSource.CreateCommandBuffer().AsParallelWriter();
                if (m_CamerasEntityQuery.CalculateEntityCount() == 0)
                    return;

                Entities.WithName("CleanPlayerCamerasJob")
                    .WithStoreEntityQueryInField(ref m_CamerasEntityQuery)
                    .ForEach((Entity entity, int entityInQueryIndex, in CameraEntity cameraEntity) =>
                    {
                        parallelWriterECB.DestroyEntity(entityInQueryIndex, cameraEntity.Value);

                        parallelWriterECB.RemoveComponent<CameraEntity>(entityInQueryIndex, entity);
                    }).ScheduleParallel();
                ecbSource.AddJobHandleForProducer(this.Dependency);
            }
            private EntityCommandBufferSystem ecbSource;

            private EntityQuery m_CamerasEntityQuery;

        }
       

    }
}