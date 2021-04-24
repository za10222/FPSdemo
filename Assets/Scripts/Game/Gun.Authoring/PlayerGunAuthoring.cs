using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;



namespace FPSdemo
{

    [DisallowMultipleComponent]
    public class PlayerGunAuthoring : MonoBehaviour, IConvertGameObjectToEntity
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
         public double  changegungap=1;
        

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<GunManager.PlayerGunData>(entity);
            dstManager.SetComponentData<GunManager.PlayerGunData>(entity, new GunManager.PlayerGunData
            {
                gunTypeIndex = 0,
                changeGun = false,
                changeGunGap= changegungap,
            });
            dstManager.AddComponent<GunManager.PlayerGunSpawn>(entity);
            dstManager.AddComponent<GunManager.PlayerGunInternalData>(entity);
        }
    }

}