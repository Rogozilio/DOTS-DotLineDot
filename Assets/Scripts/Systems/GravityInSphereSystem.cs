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
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();

            var data = SystemAPI.GetSingleton<MultiSphereComponent>();

            state.Dependency = new GravityInSphereJob
            {
                ecb = ecb.CreateCommandBuffer(state.WorldUnmanaged),
                speed = data.speedGravityInSphere
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        private partial struct GravityInSphereJob : IJobEntity
        {
            internal EntityCommandBuffer ecb;
            public float speed;

            private void Execute(Entity entity, ref PhysicsVelocity velocity, 
                in TargetGravityComponent targetGravity, in LocalToWorld localToWorld)
            {
                velocity.Linear = math.normalizesafe(targetGravity.position - localToWorld.Position) * speed;
                ecb.SetComponentEnabled<TargetGravityComponent>(entity, false);
            }
        }
    }
}