using Components;
using Components.Shared;
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
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct RaycastSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();
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

            var ecbSingleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            if (!input.isLeftMouseDown && !input.isRightMouseDown) return;

            RaycastHit hit;
            
            //Floor
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
            
            //Sphere
            if (Raycast(input, collisionFilter.collisionFilterSphere, physicsWorld, out hit))
            {
                
                if (input.isLeftMouseClicked)
                {
                    ecb.SetComponentEnabled<IsMouseMove>(hit.Entity, true);
                }
                else if (input.isRightMouseClicked)
                {
                    var index = state.EntityManager.GetSharedComponent<IndexSharedComponent>(hit.Entity);
                    ecb.AddComponent(hit.Entity, new SpawnSphereComponent
                    {
                        index = index.value,
                        countElements = SystemAPI.GetComponent<SphereComponent>(hit.Entity).countElements,
                        isAddConnectSphere = true
                    });
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
        private bool Raycast(InputDataComponent input, CollisionFilter collisionFilter,
            PhysicsWorldSingleton physicsWorld, ref NativeList<RaycastHit> hit)
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

            return physicsWorld.CastRay(raycastInput, ref hit);
        }
    }
}