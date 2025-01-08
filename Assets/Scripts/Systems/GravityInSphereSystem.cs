using Aspects;
using Components;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
    [UpdateAfter(typeof(MoveMouseSphereSystem))]
    public partial struct GravityInSphereSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MultiSphereComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var data = SystemAPI.GetSingleton<MultiSphereComponent>();

            state.Dependency = new GravityInSphereJob
            {
                speed = data.speedGravityInSphere
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        private partial struct GravityInSphereJob : IJobEntity
        {
            public float speed;

            private void Execute(ElementAspect element)
            {
                element.LinearVelocity = element.ToTargetGravity * speed;
                element.EnableTargetGravity = false;
            }
        }
    }
}