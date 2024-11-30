using Components;
using Tags;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    public partial struct MoveMouseSphereSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InputDataComponent>();
            state.RequireForUpdate<RaycastHitComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var raycastHit = SystemAPI.GetSingleton<RaycastHitComponent>();
            
            state.Dependency = new SphereFollowMouseJob
            {
                hitPoint = raycastHit.position
            }.Schedule(state.Dependency);
        }
        
        private partial struct SphereFollowMouseJob : IJobEntity
        {
            public float3 hitPoint;
            private void Execute(Entity entity, ref PhysicsVelocity velocity, in LocalToWorld world, in IsMouseMove tag)
            {
                velocity.Linear = math.normalize(hitPoint - world.Position) * 10f;
                
                if(math.distance(hitPoint, world.Position) < 0.15f)
                    velocity.Linear = float3.zero;
            }
        }
    }
}