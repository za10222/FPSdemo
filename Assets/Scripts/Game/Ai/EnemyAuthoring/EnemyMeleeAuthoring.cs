using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace FPSdemo
{
    //[GenerateAuthoringComponent]
public struct EnemyMelee : IComponentData
    {
    
       public double hitDuration;
       public double recoverDuration;

    }
    public struct EnemyMeleeInternalData : IComponentData
    {
       public bool hasFind;
       public Entity hitEntity;
       public double lastattacktime;
       public double lasthittime;
       public double dietime;


    }

    [DisallowMultipleComponent]
public class EnemyMeleeAuthoring : MonoBehaviour, IConvertGameObjectToEntity
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
    public void Start()
    {
            
    }
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {

        dstManager.AddComponentData(entity, new EnemyMelee {
            hitDuration =0.4,
            recoverDuration=0.1
        });
        dstManager.AddComponentData(entity, new EnemyMeleeInternalData {hitEntity=Entity.Null});


        }
    }
}