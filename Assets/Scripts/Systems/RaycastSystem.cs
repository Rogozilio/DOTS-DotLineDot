using Components;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

namespace Systems
{
    public partial struct RaycastSystem : ISystem
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

            RaycastHit hit;
            
            if (!input.isLeftMouseDown && !input.isRightMouseDown) return;
            
            if (Raycast(input, collisionFilter.collisionFilterFloor, physicsWorld, out hit))
            {
                var entity = SystemAPI.GetSingletonEntity<RaycastHitComponent>();
                var hitWithoutY = hit.Position;
                hitWithoutY.y = 0;

                ecb.SetComponent(entity, new RaycastHitComponent()
                {
                    position = hitWithoutY
                });
            }
            
            if (Raycast(input, collisionFilter.collisionFilterSphere, physicsWorld, out hit))
            {
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

        private bool Raycast(InputDataComponent input, CollisionFilter collisionFilter,
            PhysicsWorldSingleton physicsWorld, out RaycastHit hit)
        {
            var raycastInput = new RaycastInput
            {
                Start = input.ray.origin,
                End = (float3)input.ray.origin + math.normalize(input.ray.direction) * 100f,
                Filter = new CollisionFilter
                {
                    BelongsTo = collisionFilter.BelongsTo,
                    CollidesWith = collisionFilter.CollidesWith,
                    GroupIndex = 0
                }
            };

            return physicsWorld.CastRay(raycastInput, out hit);
        }
    }
}