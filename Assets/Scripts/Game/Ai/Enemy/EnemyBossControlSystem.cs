using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Systems;
using static FPSdemo.GunManager;
using UnityEngine;
using Reese.Nav;
using Unity.Rendering;
using System.Collections.Generic;
using Unity.NetCode;

namespace FPSdemo
{
    //[DisableAutoCreation]
    //[UpdateBefore(typeof(EnemyAnimationUpdateSystem))]
    [UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
    public class EnemyBossControlSystem : SystemBase
    {
        private BuildPhysicsWorld physicsWorldSystem;
        private EntityQuery m_FPCQuery;
        EntityCommandBufferSystem barrier => World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        private Entity m_misslePrefab;

        protected override void OnCreate()
        {
            physicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();
            m_FPCQuery = GetEntityQuery(typeof(GhostOwnerComponent), typeof(CharacterControllerComponentData), typeof(PhysicsCollider));
            RequireSingletonForUpdate<GunDataBufferElement>();
        }

        protected override void OnUpdate()
        {
            var commandBuffer = barrier.CreateCommandBuffer().AsParallelWriter();
          
            var bufferFromEntity= GetBufferFromEntity<HealthEventBufferElement>();


            var time = Time.ElapsedTime;


            var ltwFromEntity = GetComponentDataFromEntity<LocalToWorld>();
            var tranFromEntity = GetComponentDataFromEntity<Translation>();
            var PointFromEntity = GetComponentDataFromEntity<Point>();

            var FPCs = m_FPCQuery.ToEntityArray(Unity.Collections.Allocator.TempJob);

            var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;

            if (m_misslePrefab == Entity.Null)
            {
                var prefabEntity = GetSingletonEntity<GhostPrefabCollectionComponent>();
                var prefabs = EntityManager.GetBuffer<GhostPrefabBuffer>(prefabEntity);
                var foundPrefab = Entity.Null;
                for (int i = 0; i < prefabs.Length; ++i)
                {
                    if (EntityManager.HasComponent<MissleBoss>(prefabs[i].Value))
                        foundPrefab = prefabs[i].Value;
                }
                if (foundPrefab != Entity.Null)
                    m_misslePrefab = foundPrefab;
            }
            var m_misslePrefab2 = m_misslePrefab;

            Entities
                .WithReadOnly(bufferFromEntity)
                .WithReadOnly(ltwFromEntity)
                .WithReadOnly(collisionWorld)
                .WithReadOnly(tranFromEntity)
                .WithReadOnly(FPCs)
                .WithReadOnly(PointFromEntity)
                .ForEach((Entity entity, int entityInQueryIndex, ref EnemyBossInternalData enemyBossInternalData, ref EnemyBoss enemyBoss) =>
            {
                if(HasComponent<HealthData>(entity))
                {
                    var health = GetComponent<HealthData>(entity);
                    if(health.currentHp==0)
                    {
                        if(HasComponent<NavWalking>(entity))
                        {
                            commandBuffer.AddComponent<NavStop>(entityInQueryIndex, entity);
                        }
                        var physicsVelocity = new PhysicsVelocity();
                        commandBuffer.AddComponent(entityInQueryIndex, entity, physicsVelocity);

                        enemyBoss.state = EnemyBoss.EnemyBossState.dieing;
                        enemyBoss.inhit = false;
                        if (enemyBossInternalData.dietime>0.1)
                        {
                            if(time - enemyBossInternalData.dietime > 2)
                            {
                                commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                                bool isfind = false;
                                Entity shooter=Entity.Null;
                                foreach (var i in FPCs)
                                {
                                    if (GetComponent<GhostOwnerComponent>(i).NetworkId == health.lasthit)
                                    {
                                        shooter = i;
                                        isfind = true;
                                        break;
                                    }
                                }
                                if (isfind)
                                {
                                    var point = PointFromEntity[shooter];
                                    point.point += 10;
                                    commandBuffer.SetComponent<Point>(entityInQueryIndex, shooter, point);
                                    Debug.Log(string.Format("加分{0},分数{1}", shooter.Index , point.point));
                                }
                            }
                        }
                        else
                        {
                            enemyBossInternalData.dietime = time;
                        }
                        
                        return;
                    }
                }

                //如果变色期间，无视所有被打
                if (enemyBoss.inhit)
                {
                    if (time > enemyBossInternalData.lasthittime + enemyBoss.hitDuration)
                    {  //还原
                        //commandBuffer.SetComponent(entityInQueryIndex, enemyMelee.entitynode, new URPMaterialPropertyBaseColor
                        //{
                        //    Value = white4
                        //});
                        enemyBoss.inhit = false;
                    }
                 
                }
                else
                {
                    if ((time > enemyBossInternalData.lasthittime + enemyBoss.hitDuration+ enemyBoss.recoverDuration)&&
                    bufferFromEntity.HasComponent(entity) && bufferFromEntity[entity].Length > 0)
                    {
                        enemyBoss.inhit = true;
                        enemyBossInternalData.lasthittime = time;
                    }
                }
                var ltw_left = ltwFromEntity[enemyBoss.LeftRock];
                var ltw_right = ltwFromEntity[enemyBoss.RightRock];
                var ltw = ltwFromEntity[entity];

                var ltw_left_temp =new RigidTransform(ltw_left.Value);
                ltw_left_temp.pos.y = ltw.Position.y;
                var ltw_right_temp = new RigidTransform(ltw_right.Value);
                ltw_right_temp.pos.y = ltw.Position.y;
                if (enemyBoss.state == EnemyBoss.EnemyBossState.shoot)
                {
                    //判断
                    var t = time - enemyBossInternalData.lastattacktime;

                    if (t > 3d && t < 5d)
                    {
                     
                        if (!enemyBossInternalData.shootcreated)
                        {
                            var e_left = commandBuffer.Instantiate(entityInQueryIndex, m_misslePrefab2);

                            var missBossData = GetComponent<MissleBoss>(m_misslePrefab2);
                                missBossData.lifetime = 10;
                                missBossData.find = enemyBossInternalData.find;
                            commandBuffer.SetComponent<Translation>(entityInQueryIndex, e_left, new Translation { Value = ltw_left_temp.pos });
                            commandBuffer.SetComponent<Rotation>(entityInQueryIndex, e_left, new Rotation { Value = ltw.Rotation });
 
                            commandBuffer.SetComponent<MissleBoss>(entityInQueryIndex, e_left, missBossData);

                            var e_right = commandBuffer.Instantiate(entityInQueryIndex, m_misslePrefab2);
                            commandBuffer.SetComponent<Translation>(entityInQueryIndex, e_right, new Translation { Value = ltw_right_temp.pos });
                            commandBuffer.SetComponent<Rotation>(entityInQueryIndex, e_right, new Rotation { Value = ltw.Rotation });
                            commandBuffer.SetComponent<MissleBoss>(entityInQueryIndex, e_right, missBossData);

                            enemyBossInternalData.shootcreated = true;
                        }


                        //进行攻击判断 状态变为idle
                        //Debug.Log(string.Format("find player {0}", hitresult.Entity));
                        //enemy.state = Enemy.EnemyState.idle;
                        //commandBuffer.SetComponent(entityInQueryIndex, enemyMelee.entitynode, new URPMaterialPropertyBaseColor { Value = white4 });
                    }   //等待 啥都不做
                    else if (t > 5d)
                    {
                        enemyBoss.state = EnemyBoss.EnemyBossState.idle;
                        //状态变为idle
                        //Debug.Log(string.Format("find player {0}", hitresult.Entity));
                        //enemy.state = Enemy.EnemyState.idle;
                        //commandBuffer.SetComponent(entityInQueryIndex, enemyMelee.entitynode, new URPMaterialPropertyBaseColor { Value = white4 });

                    }
                    return;
                }

                if (enemyBoss.state == EnemyBoss.EnemyBossState.taunt)
                {
                    //判断
                    var t = time - enemyBossInternalData.lastattacktime;

                    if (t > 0d && t < 3d)
                    {

                        if (!enemyBossInternalData.shootcreated)
                        {
                            var tarpos = tranFromEntity[enemyBossInternalData.find];
                            var physicsMass = GetComponent<PhysicsMass>(entity);
                            var translation = GetComponent<Translation>(entity);
                            var rotation = GetComponent<Rotation>(entity);

                            var normal = math.normalize(math.forward(ltw.Rotation));

                           // tarpos.Value -= normal * 2;
                            var dis = math.distance(tarpos.Value, ltw.Position);
                            var v_abs = dis / 3;
                            var physicsVelocity = new PhysicsVelocity { Linear = normal * v_abs };

                            Debug.Log(string.Format("{0}", physicsVelocity.Linear));
                            commandBuffer.AddComponent(entityInQueryIndex, entity, physicsVelocity);

                            enemyBossInternalData.shootcreated = true;

                        }
                        //进行攻击判断 状态变为idle
                        //Debug.Log(string.Format("find player {0}", hitresult.Entity));
                        //enemy.state = Enemy.EnemyState.idle;
                        //commandBuffer.SetComponent(entityInQueryIndex, enemyMelee.entitynode, new URPMaterialPropertyBaseColor { Value = white4 });
                    }   //等待 啥都不做
                    else if (t > 3d)
                    {

                        enemyBoss.state = EnemyBoss.EnemyBossState.idle;
                        var physicsVelocity = new PhysicsVelocity();
                        commandBuffer.AddComponent(entityInQueryIndex, entity, physicsVelocity);
                        //var mass = GetComponent<PhysicsMass>(entity);
                        //var ve
                        //PhysicsVelocity.CalculateVelocityToTarget();
                        //状态变为idle
                        //Debug.Log(string.Format("find player {0}", hitresult.Entity));
                        //enemy.state = Enemy.EnemyState.idle;
                        //commandBuffer.SetComponent(entityInQueryIndex, enemyMelee.entitynode, new URPMaterialPropertyBaseColor { Value = white4 });

                    }
                    return;
                }
       



                //正常状态
                {
                    var random=new Unity.Mathematics.Random((uint)time*100+1);
                    var nextAttackType = random.NextBool();
                    //;


                    var forward = math.normalize(math.forward(ltw.Rotation));
                    var startpos = ltw.Position;
                    var endpos = ltw.Position + forward * enemyBoss.findDistance;
                    var hitresult = new ColliderCastHit();
                    var ishit = CommonUtilities.SphereCollidercast(ltwFromEntity[entity].Position, endpos, 0.5f, in collisionWorld, out hitresult);

                    if (ishit)
                    {
                        commandBuffer.AddComponent<NavStop>(entityInQueryIndex, entity);
                        enemyBossInternalData.lastattacktime = time;
                        enemyBossInternalData.shootcreated = false;
                        enemyBossInternalData.find = hitresult.Entity;
                        Debug.Log(string.Format("{0}", enemyBossInternalData.find.Index));
                        if (nextAttackType)
                        {
                        enemyBoss.state = EnemyBoss.EnemyBossState.shoot;
                     
                        }else
                        {

                         enemyBoss.state = EnemyBoss.EnemyBossState.taunt;

                        }
                    }
                    else
                    {
                        //

                        enemyBoss.state = EnemyBoss.EnemyBossState.idle;
                    }

                }



            }).ScheduleParallel();
            Dependency.Complete();
            barrier.AddJobHandleForProducer(Dependency);
            var df = Time.DeltaTime;
            Entities.ForEach((Entity entity, int entityInQueryIndex, ref MissleBoss missile) =>
            {
                missile.lifetime -= df;
                if (missile.lifetime <= 0)
                {
                    commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                }
            }).ScheduleParallel();
            barrier.AddJobHandleForProducer(Dependency);
            FPCs.Dispose();
        }
    }
}