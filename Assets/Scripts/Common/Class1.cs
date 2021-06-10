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
        static public Entity Raycast(float3 RayFrom, float3 RayTo, in BuildPhysicsWorld physicsWorldSystem)
        {
            var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;
            RaycastInput input = new RaycastInput()
            {
                Start = RayFrom,
                End = RayTo,
                Filter = new CollisionFilter()
                {
                    BelongsTo = ~0u,
                    CollidesWith = ~0u, // all 1s, so all layers, collide with everything
                    GroupIndex = 0
                }
            };

            RaycastHit hit = new RaycastHit();
            
            bool haveHit = collisionWorld.CastRay(input, out hit);

            if (haveHit)
            {
                // see hit.Position
                // see hit.SurfaceNormal
                Entity e = physicsWorldSystem.PhysicsWorld.Bodies[hit.RigidBodyIndex].Entity;
                return e;
            }

            return Entity.Null;
        }

        //public static bool Newtick
    }
}
