using Components;
using Tags;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
    public partial struct MoveMouseSphereSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LevelSettingComponent>();
            state.RequireForUpdate<InputDataComponent>();
            state.RequireForUpdate<RaycastHitComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var raycastHit = SystemAPI.GetSingleton<RaycastHitComponent>();
            var data = SystemAPI.GetSingleton<LevelSettingComponent>();
            
            state.Dependency = new SphereFollowMouseJob
            {
                hitPoint = raycastHit.position,
                speed = data.speedMoveSphere,
                fixedDeltaTime = SystemAPI.Time.fixedDeltaTime
            }.Schedule(state.Dependency);
        }
        
        private partial struct SphereFollowMouseJob : IJobEntity
        {
            public float3 hitPoint;
            public float speed;
            public float fixedDeltaTime;
            private void Execute(ref PhysicsVelocity velocity, in LocalToWorld world, in IsMouseMove tag)
            {
                velocity.Linear = math.normalize(hitPoint - world.Position) * speed * fixedDeltaTime;
                
                if(math.distance(hitPoint, world.Position) < 0.15f)
                    velocity.Linear = float3.zero;
            }
        }
    }
}