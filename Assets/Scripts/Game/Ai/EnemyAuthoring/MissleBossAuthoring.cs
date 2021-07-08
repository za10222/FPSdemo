using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace FPSdemo
{
  
   
    public struct MissleBoss:IComponentData
    {
        [GhostField]
        public bool inhit;
        public float lifetime;
        public Entity find;
        public bool hitFind;
        public Entity hitEntity;

        public Entity bodyEntity;
        public double lasthittime;
        public float navupdatedftime;
        public float hitDuration;
        public float recoverDuration;
    }


    public class MissleBossAuthoring : MonoBehaviour, IConvertGameObjectToEntity
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
        public bool inhit;

        public GameObject bodyEntity;

        public float hitDuration;
        public float recoverDuration;


        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            
            dstManager.AddComponentData(entity, new MissleBoss { recoverDuration= recoverDuration, hitDuration= hitDuration, inhit = inhit, bodyEntity = conversionSystem .GetPrimaryEntity(bodyEntity) });


        }
    }
}