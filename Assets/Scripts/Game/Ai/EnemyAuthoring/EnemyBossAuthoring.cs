using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;


namespace FPSdemo
{


public struct EnemyBoss : IComponentData
{
   [GhostField]
   public float findDistance;
   [GhostField]
   public EnemyBossState state;
   public Entity Bodyup;
   public Entity Bodylow;
   [GhostField]
   public bool inhit;


   public float hitDuration;

   public float recoverDuration;

   public double FindUpdateTime;
        public enum EnemyBossState
    {
            idle=0,
            shoot,
            bigshoot,
            dieing,
            taunt
        }
}
public struct EnemyBossInternalData : IComponentData
{
    public bool hasFind;
    public Entity hitEntity;
    public double lastattacktime;
    public double lasthittime;
    public double dietime;
}

    [DisallowMultipleComponent]
public class EnemyBossAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
        // Add fields to your component here. Remember that:
        //
        // * The purpose of this class is to store data for authoring purposes - it is not for use while the game is
        //   running.
        // 
        // * Traditional Unity serialization rules apply: fields must be public or marked with [SerializeField], and
        //   must be one of the supported types.
        //
        // For example,
        //    public float scale;

        public float findDistance = 100;
        public float hitDuration = 0.4f;
        public float recoverDuration = 0.1f;
        public GameObject Bodylow;
        public GameObject Bodyup;
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {

          dstManager.AddComponentData(entity, new EnemyBoss { findDistance = findDistance, Bodyup = conversionSystem.GetPrimaryEntity(Bodyup), Bodylow = conversionSystem.GetPrimaryEntity(Bodylow) 
          ,
              hitDuration= hitDuration,
              recoverDuration= recoverDuration
          });

            dstManager.AddComponentData(entity, new EnemyBossInternalData { });
        }
}
}