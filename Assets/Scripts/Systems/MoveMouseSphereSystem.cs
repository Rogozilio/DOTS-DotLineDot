﻿using Components;
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
        }
        
        [BurstCompile]
        [WithAll(typeof(IsMouseMove))]
        [WithOptions(EntityQueryOptions.FilterWriteGroup)]
        private partial struct SphereFollowMouseJob : IJobEntity
        {
            public float3 hitPoint;
            public float speed;
            private void Execute(in LocalTransform transform, ref PhysicsVelocity velocity)
            {
                speed *= 1 / transform.Scale;
                
                float3 direction = hitPoint - transform.Position;
                float distance = math.length(direction);
                
                velocity.Linear = math.normalizesafe(direction) * math.min(speed, speed * distance);
                velocity.Angular = float3.zero;
            }
        }
    }
}