using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        World w =new World("myworld");
        var myCube = w.EntityManager.CreateEntity(
       ComponentType.ReadOnly<LocalToWorld>(),
       ComponentType.ReadOnly<RenderMesh>(),
         ComponentType.ReadOnly<RenderBounds>()
         );

        w.EntityManager.SetComponentData(myCube, new LocalToWorld
        {
            Value = new float4x4(rotation: quaternion.identity, translation: new float3(4, 4, 4))
        });
        var ah = Resources.Load<GameObject>("Prefabs\\123").GetComponent<AssetHolder2>();

        w.EntityManager.SetSharedComponentData(myCube, new RenderMesh
        {
            mesh = ah.myMesh,
            material = ah.myMaterial
        });

        w.EntityManager.SetComponentData(myCube, new RenderBounds());
        w.EntityManager.SetName(myCube, "cube123");
        //World.DefaultGameObjectInjectionWorld = w;
        Debug.Log(123);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
