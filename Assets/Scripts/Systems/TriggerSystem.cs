using Components;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
    [UpdateBefore(typeof(GravityInSphereSystem))]
    public partial struct TriggerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
            state.Dependency = new TriggerEvent
            {
                ecb = ecb,
                sphereComponent = SystemAPI.GetComponentLookup<TagSphere>(true),
                targetGravityComponent = SystemAPI.GetComponentLookup<TargetGravityComponent>(),
                localToWorld = SystemAPI.GetComponentLookup<LocalToWorld>(true)
            }.Schedule(simulationSingleton, state.Dependency);
        }

        [BurstCompile]
        struct TriggerEvent : ITriggerEventsJob
        {
            internal EntityCommandBuffer ecb;
            [ReadOnly] public ComponentLookup<TagSphere> sphereComponent;
            public ComponentLookup<TargetGravityComponent> targetGravityComponent;
            [ReadOnly] public ComponentLookup<LocalToWorld> localToWorld;

            public void Execute(Unity.Physics.TriggerEvent triggerEvent)
            {
                Entity sphere = Entity.Null;
                Entity element = Entity.Null;

                if (sphereComponent.HasComponent(triggerEvent.EntityA))
                    sphere = triggerEvent.EntityA;
                if (sphereComponent.HasComponent(triggerEvent.EntityB))
                    sphere = triggerEvent.EntityB;
                if (targetGravityComponent.HasComponent(triggerEvent.EntityA))
                    element = triggerEvent.EntityA;
                if (targetGravityComponent.HasComponent(triggerEvent.EntityB))
                    element = triggerEvent.EntityB;

                if (Entity.Null.Equals(sphere) || Entity.Null.Equals(element))
                    return;

                var newTarget = new TargetGravityComponent
                    { target = sphere, position = localToWorld[sphere].Position };
                targetGravityComponent[element] = newTarget;
                ecb.SetComponentEnabled<TargetGravityComponent>(element, true);
            }
        }
    }
}