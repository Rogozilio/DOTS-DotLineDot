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
            var levelSetting = SystemAPI.GetSingleton<LevelSettingComponent>();
            
            state.Dependency = new SphereFollowMouseJob
            {
                hitPoint = raycastHit.position,
                speed = levelSetting.speedMoveSphere,
            }.Schedule(state.Dependency);
            state.Dependency = new ClearVelocitySphereJob().Schedule(state.Dependency);
        }
        
        [BurstCompile]
        [WithAll(typeof(IsMouseMove))]
        [WithOptions(EntityQueryOptions.FilterWriteGroup)]
        private partial struct SphereFollowMouseJob : IJobEntity
        {
            public float3 hitPoint;
            public float speed;
            private void Execute(in LocalTransform transform, ref PhysicsVelocity physicsVelocity)
            {
                speed *= 1 / transform.Scale;
                
                var direction = hitPoint - transform.Position;
                var distance = math.length(direction);

                physicsVelocity.Linear = math.normalizesafe(direction) * math.min(speed, speed * distance);
                physicsVelocity.Angular = float3.zero;
            }
        }

        [BurstCompile]
        [WithDisabled(typeof(IsMouseMove))]
        [WithOptions(EntityQueryOptions.FilterWriteGroup)]
        [WithChangeFilter(typeof(IsMouseMove))]
        public partial struct ClearVelocitySphereJob : IJobEntity
        {
            public void Execute(ref PhysicsVelocity physicsVelocity)
            {
                physicsVelocity.Linear = float3.zero;
                physicsVelocity.Angular = float3.zero;
            }
        }
    }
}