using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace FPSdemo
{
    //[DisableAutoCreation]
    [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
    [UpdateAfter(typeof(UpdateCharacterControllerInternalDataSystem))]
    [UpdateBefore(typeof(CharacterControllerSystem))]
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
                .ForEach((ref GunManager.PlayerGunData playerGunData, ref GunManager.PlayerGunInternalData  internalData, in Parent pa) =>
                {
                    var input = GetComponent<CharacterControllerInternalData>(pa.Value).Input;
                    internalData.hasinput = input.hasinput;
                    if (input.hasinput == false)
                    {
                        return;
                    }
                    internalData.changeGun = input.Commond.buttons.IsSet(UserCommand.Button.ChangeGun);
                    internalData.shoot = input.Commond.buttons.IsSet(UserCommand.Button.Shooting);
                    //internalData.shoot = input.Commond.Looking;


                }).Run();
        }
    }


}