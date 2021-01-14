using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;


namespace FPSdemo
{



    [DisallowMultipleComponent]
    public class CameraPrafebsAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public Camera cam;
        public AudioListener listener;
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            //dstManager.AddComponentObject(entity, cam);
            //dstManager.AddComponentObject(entity, listener);

            conversionSystem.AddHybridComponent(cam);
            conversionSystem.AddHybridComponent(listener);
        }
        void Start()
        {

        }
    }
  
 
}