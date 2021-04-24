using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace FPSdemo
{

    //[DisableAutoCreation]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(UpdateCharacterControllerInternalDataSystem))]
    public class PlayGunUserInputUpdateSystem : SystemBase
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
            var dt= Time.DeltaTime;
            Entities
                .WithName("PlayCameraUserInputUpdateJob")
                .WithAll<GunManager.PlayerGunData,GunManager.PlayerGunInternalData>()
                .WithoutBurst()
                .ForEach((ref GunManager.PlayerGunData playerGunData, ref GunManager.PlayerGunInternalData  internalData, in Parent pa) =>
                {
                    internalData.lastChangeDeltaTime+=dt;
                    internalData.lastShootDeltaTime+=dt;
                    var input = GetComponent<CharacterControllerInternalData>(pa.Value).Input;
                    if(input.Commond.buttons.IsSet(UserCommand.Button.ChangeGun) && internalData.lastShootDeltaTime > playerGunData.changeGunGap)
                    {
                        Debug.Log("lastShootDeltaTime" + internalData.lastShootDeltaTime);
                        Debug.Log("time " + playerGunData.changeGunGap);
                        playerGunData.changeGun = true;
                        internalData.lastShootDeltaTime  = 0;
                        playerGunData.gunTypeIndex = (playerGunData.gunTypeIndex + 1) % 2;
                        Debug.Log("武器变化"+ playerGunData.gunTypeIndex);
                        Debug.Log("time " + dt);

                    }
                }).Run();
        }
    }
}