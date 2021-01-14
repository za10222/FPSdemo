using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Entities;

public class Ship : MonoBehaviour, IConvertGameObjectToEntity
{
    public ParticleSystem particleCompanion;
    public ParticleSystemRenderer rendererCompanion;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        conversionSystem.AddHybridComponent(particleCompanion);
        conversionSystem.AddHybridComponent(rendererCompanion);
    }
}