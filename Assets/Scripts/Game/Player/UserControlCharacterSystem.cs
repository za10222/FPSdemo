using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace FPSdemo{

[DisableAutoCreation]
[UpdateInGroup(typeof(GhostPredictionSystemGroup))]
//[UpdateAfter(typeof(UserInput.UserInputUpdateSystem))]
    public class UpdateCharacterControllerInternalDataSystem : SystemBase
{
        private GhostPredictionSystemGroup m_GhostPredictionSystemGroup;
        private EntityQuery m_CommandTargetComponentQuery;

        protected override void OnCreate()
        {
            m_GhostPredictionSystemGroup = World.GetExistingSystem<GhostPredictionSystemGroup>();
            m_CommandTargetComponentQuery = GetEntityQuery(typeof(CommandTargetComponent));
            RequireForUpdate(m_CommandTargetComponentQuery);
        }
        protected override void OnUpdate()
    {
            var inputFromEntity = GetBufferFromEntity<UserCommand>(true);
            var pretick = m_GhostPredictionSystemGroup.PredictingTick;
            Entities
            .WithName("UpdateCharacterControllerInternalDataSystemJob")
            .WithReadOnly(inputFromEntity)
            .WithoutBurst()
            .WithAll<CharacterControllerInternalData>()
            .ForEach((Entity ent,ref CharacterControllerInternalData ccData, in PredictedGhostComponent prediction) =>
            {
                if (!GhostPredictionSystemGroup.ShouldPredict(pretick, prediction))
                {
                    ccData.Input.hasinput = false;
                    return;
                }
                var input = inputFromEntity[ent];
                UserCommand inputData=default;
                input.GetDataAtTick(pretick, out inputData);
                ccData.Input.Commond = inputData;
                ccData.Input.hasinput = true;
            }
            ).Run();
    }
}
}