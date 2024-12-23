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
        private ComponentLookup<TagSphere> _lookupSphere;
        private ComponentLookup<TargetGravityComponent> _lookupElement;
        private ComponentLookup<LocalToWorld> _lookupLocalToWorld;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SimulationSingleton>();

            _lookupSphere = state.GetComponentLookup<TagSphere>(true);
            _lookupElement = state.GetComponentLookup<TargetGravityComponent>();
            _lookupLocalToWorld = state.GetComponentLookup<LocalToWorld>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            _lookupSphere.Update(ref state);
            _lookupElement.Update(ref state);
            _lookupLocalToWorld.Update(ref state);

            var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
            state.Dependency = new TriggerEvent
            {
                ecb = ecb,
                sphereComponent = _lookupSphere,
                targetGravityComponent = _lookupElement,
                localToWorld = _lookupLocalToWorld
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