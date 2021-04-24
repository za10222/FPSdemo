using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.DebugDisplay;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine.Rendering.Universal;

namespace FPSdemo
{
    public static class PlayerCameraControl
    {

        [GhostComponent(PrefabType = GhostPrefabType.PredictedClient)]
        public struct State : IComponentData
        {
            public int isEnabled;
            public Vector3 position;
            public Quaternion rotation;
            public float fieldOfView;
            public float VerticalRotationSpeed;
        }
        public struct CameraEntity : ISystemStateComponentData
        {
            public Entity Value;
        }


        [DisableAutoCreation]
        [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
        public class HandlePlayerCameraControlSpawn : SystemBase
        {

            protected override void OnCreate()
            {
                
                //_BlobAssetStore = new BlobAssetStore();
                m_cameraPrefab = (GameObject)Resources.Load("Prefabs/PlayerCamera");
                m_cameraPrefabEntity=PrefabAssetManager.GetOrCreateEntityPrefab(World, m_cameraPrefab);
                var camera = EntityManager.GetComponentObject<Camera>(m_cameraPrefabEntity);
                camera.enabled = false;
                var audioListener = EntityManager.GetComponentObject<AudioListener>(m_cameraPrefabEntity);
                audioListener.enabled = false;
            }
            protected override void OnDestroy()
            {
                //_BlobAssetStore.Dispose();
                base.OnDestroy();
            }

            protected override void OnUpdate()
            {
                Entities
                  .WithStructuralChanges()
                  .WithoutBurst().WithAll<State>()
                  .WithNone<CameraEntity,Prefab>()
                  .ForEach((Entity entity) =>
                  {
                      var w=World.EntityManager.Instantiate(m_cameraPrefabEntity);
                      var camera = EntityManager.GetComponentObject<Camera>(w);
                      camera.enabled = false;
                      var audioListener = EntityManager.GetComponentObject<AudioListener>(w);
                      audioListener.enabled = false;

                      var childs = EntityManager.GetBuffer<LinkedEntityGroup>(w);
  

                      var guncam = EntityManager.GetComponentObject<Camera>(childs[1].Value);
                      var cameraData = camera.GetUniversalAdditionalCameraData();
                      var guncameraData = guncam.GetUniversalAdditionalCameraData();

                      guncameraData.renderType = CameraRenderType.Overlay;
                      cameraData.cameraStack.Add(guncam);


                      World.EntityManager.AddComponentData(entity, new CameraEntity
                      {
                          Value = w,
                      });
                  }).Run();
            }
            //BlobAssetStore _BlobAssetStore;
            GameObject m_cameraPrefab;
            Entity m_cameraPrefabEntity;
        }


        [DisableAutoCreation]
        [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
        [UpdateBefore(typeof(UpdatePlayerCameras))]
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
                            var audioListener = EntityManager.GetComponentObject<AudioListener>(cameraEntity.Value);
                            audioListener.enabled = true;
                            
                        }

                        camera.fieldOfView = state.fieldOfView;
                        //camera.transform.position = state.position;
                        //camera.transform.rotation = state.rotation;
                        //Debug.Log(lw.Position);
                        //Debug.Log(camera.transform.position);

                        EntityManager.SetComponentData<Translation>(cameraEntity.Value,new Translation {Value= lw.Position });

                        //Ϊ�˲���Ԥ��ʧ�� ��תӰ������ͷ̫������
                        //EntityManager.SetComponentData<Rotation>(cameraEntity.Value, new Rotation { Value = lw.Rotation });
                        EntityManager.SetComponentData<Rotation>(cameraEntity.Value, new Rotation { Value = state.rotation });



                        //elc.Position = lw.Position;
                        //camera.transform.rotation = lw.Rotation;

                    }).Run();
            }
        }

        [DisableAutoCreation]
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

        [DisableAutoCreation]
        [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
        [UpdateAfter(typeof(CharacterControllerSystem))]
        [UpdateBefore(typeof(UpdatePlayerCameras))]
        public class PlayCameraUserInputUpdateSystem : SystemBase
        {
            private GhostPredictionSystemGroup m_GhostPredictionSystemGroup;
            private EntityQuery m_CommandTargetComponentQuery;
            protected override void OnCreate()
            {
                m_CommandTargetComponentQuery = GetEntityQuery(typeof(CommandTargetComponent));
                RequireForUpdate(m_CommandTargetComponentQuery);
            }
            protected override void OnUpdate()
            {
                Entities
                    .WithName("PlayCameraUserInputUpdateJob")
                    .WithoutBurst()
                    .ForEach((ref State camerastate, ref CameraEntity cameraEntity, ref CharacterHead head,ref Parent pa) =>
                    {
                        var input = GetComponent<CharacterControllerInternalData>(pa.Value).Input;
                        camerastate.rotation= input.Commond.LookRotation;
                        //Debug.Log(input.Commond.Looking);
                    }).Run();
            }
        }

    }

    
}