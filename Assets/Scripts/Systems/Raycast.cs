using Components;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace Systems
{
    public partial struct Raycast : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<RaycastHitComponent>();
            state.RequireForUpdate<CollisionFilterComponent>();
            state.RequireForUpdate<InputDataComponent>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var input = SystemAPI.GetSingleton<InputDataComponent>();

            var collisionFilter = SystemAPI.GetSingleton<CollisionFilterComponent>();

            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

            var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            RaycastFloor(input, collisionFilter, physicsWorld, ecb);
            RaycastSphere(input, collisionFilter, physicsWorld, ecb);
        }

        private void RaycastFloor(InputDataComponent input, CollisionFilterComponent collisionFilter,
            PhysicsWorldSingleton physicsWorld, EntityCommandBuffer ecb)
        {
            if (!input.isLeftMouseDown && !input.isRightMouseDown) return;

            var raycastInput = new RaycastInput
            {
                Start = input.ray.origin,
                End = (float3)input.ray.origin + math.normalize(input.ray.direction) * 100f,
                Filter = new CollisionFilter
                {
                    BelongsTo = collisionFilter.collisionFilterFloor.BelongsTo,
                    CollidesWith = collisionFilter.collisionFilterFloor.CollidesWith,
                    GroupIndex = 0
                }
            };

            if (physicsWorld.CastRay(raycastInput, out var hit))
            {
                var entity = SystemAPI.GetSingletonEntity<RaycastHitComponent>();
                var hitWithoutY = hit.Position;
                hitWithoutY.y = 0;

                ecb.SetComponent(entity, new RaycastHitComponent()
                {
                    position = hitWithoutY
                });
            }
        }

        private void RaycastSphere(InputDataComponent input, CollisionFilterComponent collisionFilter,
            PhysicsWorldSingleton physicsWorld, EntityCommandBuffer ecb)
        {
            var raycastInput = new RaycastInput
            {
                Start = input.ray.origin,
                End = (float3)input.ray.origin + math.normalize(input.ray.direction) * 100f,
                Filter = new CollisionFilter
                {
                    BelongsTo = collisionFilter.collisionFilterSphere.BelongsTo,
                    CollidesWith = collisionFilter.collisionFilterSphere.CollidesWith,
                    GroupIndex = 0
                }
            };

            if (!physicsWorld.CastRay(raycastInput, out var hit)) return;

            if (input.isLeftMouseClicked)
            {
                ecb.SetComponentEnabled<IsMouseMove>(hit.Entity, true);
            }
            else if (input.isRightMouseClicked)
            {
                var newSphere = ecb.Instantiate(hit.Entity);
                ecb.SetComponentEnabled<IsMouseMove>(newSphere, true);
                ecb.AddComponent(hit.Entity, new ConnectSphere { target = newSphere });
            }
        }
    }
}