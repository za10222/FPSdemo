using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


namespace FPSdemo
{


public struct Enemy : IComponentData
{
   public float distance;
   public float timegap;
}

[DisallowMultipleComponent]
public class EnemyAuthoring : MonoBehaviour, IConvertGameObjectToEntity
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

        public float distance = 100;
             
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {

          dstManager.AddComponentData(entity, new Enemy { distance= distance });
    }
}
}