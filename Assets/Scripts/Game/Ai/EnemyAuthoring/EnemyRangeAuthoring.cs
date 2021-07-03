using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace FPSdemo
{
    //[GenerateAuthoringComponent]
public struct EnemyRange : IComponentData
    {
    
       public double hitDuration;
       public double recoverDuration;
       public Entity bulletspawn;
       
       public float attackRange;

    }
    public struct EnemyRangeInternalData : IComponentData
    {
       public double lastattacktime;
       public double lasthittime;
       public double dietime;
       public bool missileCreated;
    }

    [DisallowMultipleComponent]
public class EnemyRangeAuthoring : MonoBehaviour, IConvertGameObjectToEntity
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
        //public float scale;
        //public float hitDuration;
        public GameObject bulletspawn;
        public float attackRange=10;
        public void Start()
    {
            
    }
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {

            dstManager.AddComponentData(entity, new EnemyRange
            {
                hitDuration = 0.4,
                recoverDuration = 0.1,
                bulletspawn = conversionSystem.GetPrimaryEntity(bulletspawn),
                attackRange=attackRange
            });;
        dstManager.AddComponentData(entity, new EnemyRangeInternalData { });


        }
    }
}