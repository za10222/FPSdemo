using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FPSdemo { 
    public class PrefabAssetManager
    {

        public static void Shutdown()
        {
            m_EntityPrefabs.Clear();
        }

        public static Entity GetOrCreateEntityPrefab(World world, GameObject prefab)
        {
            Entity entityPrefab;
            var tuple = new Tuple<GameObject, World>(prefab, world);
            if (!m_EntityPrefabs.TryGetValue(tuple, out entityPrefab))
            {
                using (BlobAssetStore _BlobAssetStore = new BlobAssetStore())
                {
                    var settings = GameObjectConversionSettings.FromWorld(world, _BlobAssetStore);
                    entityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, settings);
                    m_EntityPrefabs.Add(tuple, entityPrefab);
                }
            }

            return entityPrefab;
        }
        
        static Dictionary<Tuple<GameObject, World>, Entity> m_EntityPrefabs = new Dictionary<Tuple<GameObject, World>, Entity>(64);
    }
}