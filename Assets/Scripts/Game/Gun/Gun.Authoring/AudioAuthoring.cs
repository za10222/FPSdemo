using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace FPSdemo
{
    [DisallowMultipleComponent]
    public class AudioAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public AudioSource m_audio;
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            conversionSystem.AddHybridComponent(m_audio);
        }

    }
}
    