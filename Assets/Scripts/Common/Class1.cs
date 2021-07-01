using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

namespace FPSdemo
{
    public static class CommonUtilities
    {
        static public bool Raycast(float3 RayFrom, float3 RayTo, in CollisionWorld collisionWorld, out RaycastHit hit)
        {

            RaycastInput input = new RaycastInput()
            {
                Start = RayFrom,
                End = RayTo,
                Filter = new CollisionFilter()
                {
                    BelongsTo = ~0u,
                    CollidesWith = ~(1u<<22), // all 1s, so all layers, collide with everything
                    GroupIndex = -5
                }
            };

            bool haveHit = collisionWorld.CastRay(input, out hit);

            return haveHit;
        }
        static public bool Raycast(float3 RayFrom, float3 RayTo, in CollisionWorld collisionWorld,ref RaycastInput input, out RaycastHit hit)
        {
            input.Start = RayFrom;
            input.End = RayTo;
            bool haveHit = collisionWorld.CastRay(input, out hit);

            return haveHit;
        }
        //public static bool Newtick
    }
}
