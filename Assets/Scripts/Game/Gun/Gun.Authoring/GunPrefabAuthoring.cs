using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace FPSdemo
{
    [DisallowMultipleComponent]
    public class GunPrefabAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
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
        public float shootgap=0;

        public int guntypeindex=0;

        public float  damage = 10;
        public float ballisticVelocity=1;

        public GameObject gunmodel;

        public GameObject muzzleprefab;

        public GameObject projectile;

        public GameObject VFX;

        public void Start()
        {
            
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<GunManager.GunBaseData>(entity);
            dstManager.SetComponentData<GunManager.GunBaseData>(entity, new GunManager.GunBaseData
            {
                shootgap = shootgap,
                gunTypeIndex = guntypeindex,
                ballisticVelocity = ballisticVelocity,
                damage=damage
            });
            GunManager.GunRenderData gunRenderData = default(GunManager.GunRenderData);
            gunRenderData.GunModelEntity = conversionSystem.GetPrimaryEntity(gunmodel);
            gunRenderData.MuzzleEntity = conversionSystem.GetPrimaryEntity(muzzleprefab);
            if(projectile != null)
            {
                gunRenderData.ProjectileEntity = conversionSystem.GetPrimaryEntity(projectile);
            }
            if (VFX != null)
            {
                gunRenderData.VFXEntity = conversionSystem.GetPrimaryEntity(VFX);
            }

            dstManager.AddComponentData<GunManager.GunRenderData>(entity, gunRenderData);

        }
        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(gunmodel);
            referencedPrefabs.Add(muzzleprefab);
            if(projectile!=null)
                referencedPrefabs.Add(projectile);
            if (VFX != null)
                referencedPrefabs.Add(VFX);
        }

    }
}
